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

        var configs = configure.GetSection("Ebay").Get<EbayOAuthOptions>();

        configs.Validate();
        services.AddSingleton(configs);
        services.AddSingleton<EbayAppClient>();
        services.AddSingleton<EbayOAuthClient>();
        services.AddHttpClient<EbayOAuthClient>();
        return services;
    }
}