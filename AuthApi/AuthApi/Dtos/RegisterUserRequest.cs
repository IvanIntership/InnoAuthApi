using InnoClinic.Shared.Events;

namespace AuthApi.API.Dtos;

public sealed record RegisterUserRequest
{
    public string Email { get; init; }
    public string Password { get; init; }
    public string Firstname { get; init; }
    public string Lastname { get; init; }
    public string PhoneNumber { get; init; }
    public DateTime Birthday { get; init; }
    public Roles Role { get; init; }
}