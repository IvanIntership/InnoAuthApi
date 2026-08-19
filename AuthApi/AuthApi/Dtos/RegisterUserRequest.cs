using AuthApi.API.Enums;

namespace AuthApi.API.Dtos;

public sealed class RegisterUserRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Email { get; set; }
    public Roles Role { get; set; }
}