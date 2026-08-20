namespace AuthApi.API.Dtos;

public sealed record TokenResponse
{
    public string AccessToken { get; init; }
    public string RefreshToken { get; init; }
    public int AccessTokenExpiresIn { get; init; }
    public int RefreshTokenExpiresIn { get; init; }
};