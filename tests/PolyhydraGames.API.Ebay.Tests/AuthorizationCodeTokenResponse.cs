using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PolyhydraGames.API.Ebay.Tests
{
    public sealed class AuthorizationCodeTokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; init; } = string.Empty;
        [JsonPropertyName("expires_in")] public int ExpiresInSeconds { get; init; }
        [JsonPropertyName("token_type")] public string TokenType { get; init; } = string.Empty; // "User Access Token"
        [JsonPropertyName("refresh_token")] public string RefreshToken { get; init; } = string.Empty;
        [JsonPropertyName("refresh_token_expires_in")] public int? RefreshTokenExpiresInSeconds { get; init; }
        [JsonExtensionData] public Dictionary<string, object?> Extra { get; init; } = new();
    }
}