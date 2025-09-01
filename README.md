

// File: README.md
# EbayOAuth – minimal C# client for eBay Authorization Code Flow

Implements the consent URL builder, authorization-code exchange, and refresh-token flow for eBay REST APIs.

## Quick start
```csharp
var options = new EbayOAuthOptions
{
    ClientId = "YOUR-APP-ID",
    ClientSecret = "YOUR-APP-SECRET",
    RedirectUriRuName = "YOUR-RUNAME", // e.g., "YourCompany-YourApp-SBX-1234567890"
    Environment = EbayEnvironment.Sandbox, // or Production
    Scopes = string.Join(' ', new []
    {
        "https://api.ebay.com/oauth/api_scope/sell.inventory",
        "https://api.ebay.com/oauth/api_scope/sell.account"
    }),
    DefaultLocale = "en-US"
};
var oauth = new EbayOAuthClient(options);

// 1) Build consent URL and redirect user
var state = Guid.NewGuid().ToString("N"); // store and verify on return
var consentUrl = oauth.GetConsentUrl(state: state, promptLogin: true);
// Redirect the user-agent to consentUrl

// 2) Handle redirect back at your Accept URL (RuName mapping)
// Extract `code` (already URL-encoded by eBay) and your `state` and verify `state`.
var code = receivedQuery["code"]; // preserve exact value
var tokens = await oauth.ExchangeCodeForTokensAsync(code);

// Persist tokens.RefreshToken securely for the user; AccessToken is short-lived.

// 3) Later, refresh the access token when needed
var refreshed = await oauth.RefreshAccessTokenAsync(tokens.RefreshToken);
```

## Notes
-eBay returns the authorization code URL-encoded and expects it passed as-is in the form payload (avoid double-encoding).
- The `scope` string must be a single space-separated list of full scope URLs.
- When refreshing, if you specify `scope`, it must be equal to or a subset of the original scopes.

## Target frameworks
- .NET 8+ (uses HttpClient)
- If on .NET 9, `JsonNamingPolicy.SnakeCaseLower` is used; for older frameworks, swap to default and annotate properties.

## Packaging
Create a new class library and drop the `EbayOAuth` folder in, or package as a NuGet with this as the root namespace.
