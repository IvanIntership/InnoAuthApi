using InnoClinic.Shared.Events;

namespace AuthApi.API.Entities;

public sealed class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public Roles Role { get; set; }
}