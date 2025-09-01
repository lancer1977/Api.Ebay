using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace PolyhydraGames.API.Ebay;

public sealed class EbayOAuthClient
{
    private readonly HttpClient _http;
    private readonly EbayOAuthOptions _options;

    public EbayOAuthClient(EbayOAuthOptions options, HttpClient? httpClient = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _http = httpClient ?? new HttpClient();
    }

    public Uri GetConsentUrl(string? state = null, bool promptLogin = false, string? locale = null)
    {
        var baseUrl = _options.Environment == EbayEnvironment.Sandbox
            ? "https://auth.sandbox.ebay.com/oauth2/authorize"
            : "https://auth.ebay.com/oauth2/authorize";

        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = _options.ClientId;
        if (!string.IsNullOrWhiteSpace(locale ?? _options.DefaultLocale))
            query["locale"] = locale ?? _options.DefaultLocale;
        if (promptLogin) query["prompt"] = "login";
        query["redirect_uri"] = _options.RedirectUriRuName;
        query["response_type"] = "code";
        // eBay expects a URL-encoded, space-delimited string.
        query["scope"] = _options.Scopes;
        if (!string.IsNullOrWhiteSpace(state)) query["state"] = state;

        return new Uri($"{baseUrl}?{query}");
    }

    public async Task<AuthorizationCodeTokenResponse> ExchangeCodeForTokensAsync(string urlEncodedAuthorizationCode, CancellationToken ct = default)
    {
        // Per docs, the `code` you receive is URL-encoded. Most form-body helpers encode again,
        // so pass the value exactly once as it appears in the redirect (no double-encoding).
        var endpoint = _options.Environment == EbayEnvironment.Sandbox
            ? "https://api.sandbox.ebay.com/identity/v1/oauth2/token"
            : "https://api.ebay.com/identity/v1/oauth2/token";

        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = _options.RedirectUriRuName,
                ["code"] = urlEncodedAuthorizationCode
            })
        };

        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", BuildBasicCredentials());
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        await EnsureSuccess(resp);
        var payload = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<AuthorizationCodeTokenResponse>(payload, JsonOptions())!;
    }

    public async Task<RefreshTokenResponse> RefreshAccessTokenAsync(string refreshToken, string? scopes = null, CancellationToken ct = default)
    {
        var endpoint = _options.Environment == EbayEnvironment.Sandbox
            ? "https://api.sandbox.ebay.com/identity/v1/oauth2/token"
            : "https://api.ebay.com/identity/v1/oauth2/token";

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        };
        // Optional; if provided, must be equal to or a subset of original consent scopes.
        if (!string.IsNullOrWhiteSpace(scopes)) form["scope"] = scopes!; else form["scope"] = _options.Scopes;

        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new FormUrlEncodedContent(form)
        };

        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", BuildBasicCredentials());
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        await EnsureSuccess(resp);
        var payload = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<RefreshTokenResponse>(payload, JsonOptions())!;
    }

    private string BuildBasicCredentials()
    {
        var raw = Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}");
        return Convert.ToBase64String(raw);
    }

    private static async Task EnsureSuccess(HttpResponseMessage resp)
    {
        if (resp.IsSuccessStatusCode) return;
        var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        throw new HttpRequestException($"eBay OAuth request failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, // .NET 9
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}