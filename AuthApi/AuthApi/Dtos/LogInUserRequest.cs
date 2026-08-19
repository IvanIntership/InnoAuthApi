namespace AuthApi.API.Dtos;

public sealed record LogInUserRequest
{
    public string Username { get; init; }
    public string Password { get; init; }
}