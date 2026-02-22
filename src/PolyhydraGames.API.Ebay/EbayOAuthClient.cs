using PolyhydraGames.API.Ebay.Models;
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
using static System.Net.WebRequestMethods;

namespace PolyhydraGames.API.Ebay;

public abstract class EbayClient
{
    protected readonly HttpClient _http;
    protected readonly EbayOAuthOptions _options;
    protected string? _authToken;
    public EbayClient(EbayOAuthOptions options, HttpClient? httpClient = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _http = httpClient ?? new HttpClient();
    }
    protected string BuildBasicCredentials()
    {
        var raw = Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}");
        return Convert.ToBase64String(raw);
    }

    protected static async Task EnsureSuccess(HttpResponseMessage resp)
    {
        if (resp.IsSuccessStatusCode) return;
        var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        throw new HttpRequestException($"eBay OAuth request failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");
    }

    protected static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, // .NET 9
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

}
public sealed class EbayOAuthClient : EbayClient
{


    public EbayOAuthClient(EbayOAuthOptions options, HttpClient httpClient = null) : base(options, httpClient)
    {
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
    /// <summary>
    /// Mints an Application access token (aka app token) using the client credentials grant.
    /// The <paramref name="scopes"/> parameter is a space-separated list of full scope URLs. If null, uses <see cref="EbayOAuthOptions.Scopes"/>.
    /// Note: App tokens cannot be refreshed; reuse until expiry and then call again.
    /// </summary>

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



 
}

public class EbayAppClient : EbayClient
{
    private string _prefix;
    public EbayAppClient(HttpClient http, EbayOAuthOptions options) : base(options, http)
    {
        _prefix =  options.Environment == EbayEnvironment.Production 
            ? "https://api.ebay.com" 
            : "https://api.sandbox.ebay.com";
    }
    public async Task<bool> Initialize()
    {

        var result = await GetApplicationTokenAsync();
        _authToken = result.AccessToken;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
        _http.DefaultRequestHeaders.Add("X-EBAY-C-MARKETPLACE-ID", "EBAY_US");
        _http.DefaultRequestHeaders.Add("Accept-Language", "en-US");
        return !string.IsNullOrEmpty(result.AccessToken);
    }
    public   async Task<string> Search(string query, int limit = 5)
    { 

        // Example: search “turbo grafx console”, US marketplace, include auctions, cap 5
        var filter = "buyingOptions:{AUCTION | FIXED_PRICE},price:[50..500]";
        var url = $"{_prefix}/buy/browse/v1/item_summary/search?q={query}&limit={limit}&filter={filter}";

        using var resp = await _http.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadAsStringAsync();
        return result;
    }


    private async Task<ApplicationTokenResponse> GetApplicationTokenAsync(string? scopes = null, CancellationToken ct = default)
    {
        var endpoint = _options.Environment == EbayEnvironment.Sandbox
            ? "https://api.sandbox.ebay.com/identity/v1/oauth2/token"
            : "https://api.ebay.com/identity/v1/oauth2/token";


        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials"
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new FormUrlEncodedContent(form)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", BuildBasicCredentials());
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        await EnsureSuccess(resp);
        var payload = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<ApplicationTokenResponse>(payload, JsonOptions())!;
    }
}

