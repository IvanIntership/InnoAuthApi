using AuthApi.API.Enums;

namespace AuthApi.API.Dtos;

public sealed record RegisterUserRequest
{
    public string Username { get; init; }
    public string Password { get; init; }
    public string Email { get; init; }
    public Roles Role { get; init; }
}