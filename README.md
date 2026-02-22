# Api.Ebay

A minimal C# OAuth client for eBay Authorization Code flow, including consent URL generation, code exchange, and refresh token support.

## Repository layout

This repository has been organized into a more standard structure:

- `src/PolyhydraGames.API.Ebay/` — main library project
- `tests/PolyhydraGames.API.Ebay.Tests/` — test project
- `docs/` — repository and development documentation
- `scripts/` — helper scripts and local utility snippets

## Quick start

```csharp
var options = new EbayOAuthOptions
{
    ClientId = "YOUR-APP-ID",
    ClientSecret = "YOUR-APP-SECRET",
    RedirectUriRuName = "YOUR-RUNAME",
    Environment = EbayEnvironment.Sandbox,
    Scopes = string.Join(' ', new[]
    {
        "https://api.ebay.com/oauth/api_scope/sell.inventory",
        "https://api.ebay.com/oauth/api_scope/sell.account"
    }),
    DefaultLocale = "en-US"
};

var oauth = new EbayOAuthClient(options);
var state = Guid.NewGuid().ToString("N");
var consentUrl = oauth.GetConsentUrl(state: state, promptLogin: true);
```

## Build and test

```bash
dotnet restore Api.Ebay.sln
dotnet build Api.Ebay.sln
dotnet test Api.Ebay.sln
```

## Notes

- eBay returns authorization `code` URL-encoded; pass as-is to avoid double-encoding.
- `Scopes` should be a single space-separated set of full scope URLs.
- For refresh operations, requested scope must be equal to or a subset of original scope.

See `docs/` for additional repository documentation.
