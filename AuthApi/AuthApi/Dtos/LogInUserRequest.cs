namespace AuthApi.API.Dtos;

public sealed class LogInUserRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}