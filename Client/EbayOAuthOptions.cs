// File: EbayOAuth/EbayEnvironment.cs
using System;

namespace PolyhydraGames.API.Ebay;

public sealed class EbayOAuthOptions
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }

    /// <summary>
    /// eBay calls this the RuName. It must match the environment (Sandbox vs Production) value
    /// you configured in the eBay Developer portal.
    /// </summary>
    public required string RedirectUriRuName { get; init; }

    /// <summary>
    /// Space-separated list of scopes (full URL values) expected by eBay.
    /// Example: "https://api.ebay.com/oauth/api_scope/sell.inventory https://api.ebay.com/oauth/api_scope/sell.account"
    /// </summary>
    public required string Scopes { get; init; }

    public EbayEnvironment Environment { get; init; } = EbayEnvironment.Sandbox;

    /// <summary>
    /// Optional default locale for the consent page, e.g. "de-DE".
    /// </summary>
    public string? DefaultLocale { get; init; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ClientId)) throw new ArgumentException("ClientId is required", nameof(ClientId));
        if (string.IsNullOrWhiteSpace(ClientSecret)) throw new ArgumentException("ClientSecret is required", nameof(ClientSecret));
        if (string.IsNullOrWhiteSpace(RedirectUriRuName)) throw new ArgumentException("RedirectUriRuName (RuName) is required", nameof(RedirectUriRuName));
        if (string.IsNullOrWhiteSpace(Scopes)) throw new ArgumentException("Scopes are required (space-separated, URL values)", nameof(Scopes));
    }
}