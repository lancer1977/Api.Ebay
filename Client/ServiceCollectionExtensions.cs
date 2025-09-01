using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PolyhydraGames.API.Ebay;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers EbayOAuthClient with DI. Configure <see cref="EbayOAuthOptions"/> via the configure action.
    /// </summary>
    public static IServiceCollection AddEbayOAuth(this IServiceCollection services, IConfiguration configure)
    {

        var configs = configure.GetSection("Ebay").Get<OAuthCreds>();
        var opts = new EbayOAuthOptions
        {
            ClientId = configs.ClientID,
            ClientSecret = configs.ClientSecret,
            RedirectUriRuName = configs.RedirectUriRuName,
            Scopes =  string.Join(' ', new[]
            {
                "https://api.ebay.com/oauth/api_scope/sell.inventory",
                "https://api.ebay.com/oauth/api_scope/sell.account"
            }),
        }; 
        opts.Validate();
        services.AddSingleton(opts);
        services.AddHttpClient<EbayOAuthClient>();
        return services;
    }
}