namespace PolyhydraGames.API.Ebay;

public record OAuthCreds(string ClientID, string ClientSecret, string DevID, string RedirectURI)
{
    public string Scopes { get; set; }
    public string RedirectUriRuName { get; set; }
}
 