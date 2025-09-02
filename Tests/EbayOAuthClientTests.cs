using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Castle.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using PolyhydraGames.API.Ebay.Models;

namespace PolyhydraGames.API.Ebay.Tests;

public class EbayAppClientTests : TestBase<EbayAppClientTests>
{
    private EbayAppClient _client;
    [Test]
    public async Task ExchangeCodeForTokensAsync_SendsCorrectRequest_ParsesResponse()
    {

        var result = await _client.Initialize();
        Assert.That(result);
    }

    [Test]
    public async Task SearchForTurbo()
    {
        var result = await _client.Initialize();
        var results = await _client.Search("Turbo Grafx Express");
        
        Assert.That(result);
    }
    [SetUp]
    public void Setup()
    {
        BuildServiceProvider(x => { });
        _client = base.ServiceProvider.GetService<EbayAppClient>();
    }
}

public class EbayOAuthClientTests : TestBase<EbayOAuthClientTests>
{
    [SetUp]
    public void Setup()
    {
        BuildServiceProvider(x =>
        {

        });
    }

    [Test]
    public void GetConsentUrl_BuildsExpectedQuery()
    {
        var client = base.ServiceProvider.GetService<EbayOAuthClient>();

        var state = "abc123";
        var uri = client.GetConsentUrl(state: state, promptLogin: true, locale: "de-DE");

        Assert.That(uri.Host, Is.EqualTo("auth.sandbox.ebay.com"));
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        Assert.Multiple(() =>
        {
            Assert.That(query["client_id"], Is.EqualTo("cid"));
            Assert.That(query["redirect_uri"], Is.EqualTo("YourCompany-YourApp-SBX-1234567890"));
            Assert.That(query["response_type"], Is.EqualTo("code"));
            Assert.That(query["scope"], Is.EqualTo("https://api.ebay.com/oauth/api_scope/sell.inventory https://api.ebay.com/oauth/api_scope/sell.account"));
            Assert.That(query["state"], Is.EqualTo(state));
            Assert.That(query["prompt"], Is.EqualTo("login"));
            Assert.That(query["locale"], Is.EqualTo("de-DE"));
        });
    }



    [Test]
    public async Task RefreshAccessTokenAsync_SendsCorrectRequest_ParsesResponse()
    {
        var handler = new TestHttpMessageHandler(req =>
        {
            Assert.That(req.RequestUri!.ToString(), Is.EqualTo("https://api.sandbox.ebay.com/identity/v1/oauth2/token"));
            var body = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            var parsed = System.Web.HttpUtility.ParseQueryString(body);
            Assert.That(parsed["grant_type"], Is.EqualTo("refresh_token"));
            Assert.That(parsed["refresh_token"], Is.EqualTo("rt"));
            Assert.That(parsed["scope"], Is.EqualTo("https://api.ebay.com/oauth/api_scope/sell.inventory https://api.ebay.com/oauth/api_scope/sell.account"));

            var payload = JsonSerializer.Serialize(new RefreshTokenResponse
            {
                AccessToken = "new-at",
                ExpiresInSeconds = 7200,
                TokenType = "User Access Token"
            });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        });
        var client = base.ServiceProvider.GetService<EbayOAuthClient>();

        var res = await client.RefreshAccessTokenAsync("rt");
        Assert.Multiple(() =>
        {
            Assert.That(res.AccessToken, Is.EqualTo("new-at"));
            Assert.That(res.TokenType, Is.EqualTo("User Access Token"));
            Assert.That(res.ExpiresInSeconds, Is.EqualTo(7200));
        });
    }

    [Test]
    public void ExchangeCodeForTokensAsync_OnError_ThrowsWithBody()
    {

        var client = base.ServiceProvider.GetService<EbayOAuthClient>();

        var ex = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await client.ExchangeCodeForTokensAsync("bad"));
        StringAssert.Contains("eBay OAuth request failed: 400", ex!.Message);
        StringAssert.Contains("invalid_grant", ex!.Message);
    }

    [Test]
    public void AddEbayOAuth_RegistersClientAndOptions()
    {
        var services = new ServiceCollection();
        services.AddEbayOAuth(base._configuration);
        var sp = services.BuildServiceProvider();

        var opts = sp.GetRequiredService<EbayOAuthOptions>();
        Assert.That(opts.ClientId, Is.EqualTo("cid"));

        var client = sp.GetRequiredService<Ebay.EbayOAuthClient>();
        Assert.That(client, Is.Not.Null);
    }
}
