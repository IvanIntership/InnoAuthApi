using System.Text.Json.Serialization;

namespace AuthApi.API.Dtos;

public sealed record TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; }
    
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; init; }
    
    [JsonPropertyName("expires_in")]
    public int AccessTokenExpiresIn { get; init; }
    
    [JsonPropertyName("refresh_expires_in")]
    public int RefreshTokenExpiresIn { get; init; }
};