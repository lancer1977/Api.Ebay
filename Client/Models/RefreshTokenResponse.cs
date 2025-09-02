using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PolyhydraGames.API.Ebay.Models;

public sealed class RefreshTokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; init; } = string.Empty;
    [JsonPropertyName("expires_in")] public int ExpiresInSeconds { get; init; }
    [JsonPropertyName("token_type")] public string TokenType { get; init; } = string.Empty; // "User Access Token"
    [JsonExtensionData] public Dictionary<string, object> Extra { get; init; } = new();
}