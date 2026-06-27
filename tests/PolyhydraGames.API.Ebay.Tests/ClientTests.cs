using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PolyhydraGames.API.Ebay;
using PolyhydraGames.API.Ebay.Models;

namespace PolyhydraGames.API.Ebay.Tests;

[TestFixture]
public class ClientTests
{
    [Test]
    public void Options_ValidateRejectsMissingRequiredValues()
    {
        var options = new EbayOAuthOptions
        {
            ClientId = "client",
            ClientSecret = "secret",
            RedirectUriRuName = "ru-name",
            Scopes = string.Empty
        };

        var ex = Assert.Throws<ArgumentException>(options.Validate);

        Assert.That(ex?.ParamName, Is.EqualTo(nameof(EbayOAuthOptions.Scopes)));
    }

    [Test]
    public void AddEbayOAuth_RejectsMissingConfigurationSection()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<InvalidOperationException>(() => services.AddEbayOAuth(configuration));

        Assert.That(ex?.Message, Does.Contain("Missing Ebay configuration section"));
    }

    [Test]
    public void ConsentUrl_UsesSandboxAuthorizeEndpointAndEncodesQuery()
    {
        var client = new EbayOAuthClient(CreateOptions(EbayEnvironment.Sandbox));

        var consent = client.GetConsentUrl(state: "hello world", promptLogin: true, locale: "de-DE");
        var query = HttpUtility.ParseQueryString(consent.Query);

        Assert.That(consent.AbsoluteUri, Does.StartWith("https://auth.sandbox.ebay.com/oauth2/authorize"));
        Assert.That(query["client_id"], Is.EqualTo("client-id"));
        Assert.That(query["redirect_uri"], Is.EqualTo("ru-name"));
        Assert.That(query["response_type"], Is.EqualTo("code"));
        Assert.That(query["scope"], Is.EqualTo("scope-a scope-b"));
        Assert.That(query["prompt"], Is.EqualTo("login"));
        Assert.That(query["state"], Is.EqualTo("hello world"));
        Assert.That(query["locale"], Is.EqualTo("de-DE"));
    }

    [Test]
    public async Task ExchangeCodeForTokensAsync_SendsAuthorizationCodeGrantRequest()
    {
        var handler = new CapturingHandler(() => JsonContent(new AuthorizationCodeTokenResponse
        {
            AccessToken = "access-token",
            ExpiresInSeconds = 7200,
            TokenType = EbayDefinitions.UserAccessToken,
            RefreshToken = "refresh-token",
            RefreshTokenExpiresInSeconds = 3600
        }));

        var client = new EbayOAuthClient(CreateOptions(EbayEnvironment.Sandbox), new HttpClient(handler));

        var response = await client.ExchangeCodeForTokensAsync("CODE-123");

        Assert.That(response.AccessToken, Is.EqualTo("access-token"));
        Assert.That(response.RefreshToken, Is.EqualTo("refresh-token"));
        Assert.That(handler.Request?.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(handler.Request?.RequestUri?.AbsoluteUri, Is.EqualTo("https://api.sandbox.ebay.com/identity/v1/oauth2/token"));
        Assert.That(handler.Request?.Headers.Authorization?.Scheme, Is.EqualTo("Basic"));
        Assert.That(handler.Request?.Headers.Accept.Single().MediaType, Is.EqualTo("application/json"));

        var parsed = HttpUtility.ParseQueryString(handler.Body);

        Assert.That(parsed["grant_type"], Is.EqualTo("authorization_code"));
        Assert.That(parsed["redirect_uri"], Is.EqualTo("ru-name"));
        Assert.That(parsed["code"], Is.EqualTo("CODE-123"));
        Assert.That(handler.Request.Headers.Authorization?.Parameter, Is.EqualTo(Convert.ToBase64String(Encoding.UTF8.GetBytes("client-id:client-secret"))));
    }

    [Test]
    public async Task RefreshAccessTokenAsync_SendsRefreshTokenGrantRequest()
    {
        var handler = new CapturingHandler(() => JsonContent(new RefreshTokenResponse
        {
            AccessToken = "new-access-token",
            ExpiresInSeconds = 7200,
            TokenType = EbayDefinitions.UserAccessToken
        }));

        var client = new EbayOAuthClient(CreateOptions(EbayEnvironment.Production), new HttpClient(handler));

        var response = await client.RefreshAccessTokenAsync("refresh-token");

        Assert.That(response.AccessToken, Is.EqualTo("new-access-token"));
        Assert.That(handler.Request?.RequestUri?.AbsoluteUri, Is.EqualTo("https://api.ebay.com/identity/v1/oauth2/token"));

        var parsed = HttpUtility.ParseQueryString(handler.Body);

        Assert.That(parsed["grant_type"], Is.EqualTo("refresh_token"));
        Assert.That(parsed["refresh_token"], Is.EqualTo("refresh-token"));
        Assert.That(parsed["scope"], Is.EqualTo("scope-a scope-b"));
    }

    [Test]
    public async Task RefreshAccessTokenAsync_UsesExplicitScopesWhenProvided()
    {
        var handler = new CapturingHandler(() => JsonContent(new RefreshTokenResponse
        {
            AccessToken = "new-access-token",
            ExpiresInSeconds = 7200,
            TokenType = EbayDefinitions.UserAccessToken
        }));

        var client = new EbayOAuthClient(CreateOptions(), new HttpClient(handler));

        await client.RefreshAccessTokenAsync("refresh-token", scopes: "scope-c scope-d");

        var parsed = HttpUtility.ParseQueryString(handler.Body);

        Assert.That(parsed["scope"], Is.EqualTo("scope-c scope-d"));
    }

    private static EbayOAuthOptions CreateOptions(EbayEnvironment environment = EbayEnvironment.Sandbox) => new()
    {
        ClientId = "client-id",
        ClientSecret = "client-secret",
        RedirectUriRuName = "ru-name",
        Scopes = "scope-a scope-b",
        Environment = environment
    };

    private static HttpResponseMessage JsonContent<T>(T payload)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
    }

    private sealed class CapturingHandler(Func<HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null
                ? string.Empty
                : request.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
            return Task.FromResult(responseFactory());
        }
    }
}
