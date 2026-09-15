using MassTransit;
using InnoClinic.Shared.Events;
using AuthApi.API.Interfaces;
using AuthApi.API.Dtos;

namespace AuthApi.API.Consumers;

public sealed class StaffCreatedConsumer : IConsumer<IStaffCreatedEvent>
{
    private readonly IAuthService _authService;

    public StaffCreatedConsumer(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task Consume(ConsumeContext<IStaffCreatedEvent> context)
    {
        var msg = context.Message;

        var request = new RegisterUserRequest
        {
            Email = msg.Email,
            Password = msg.Password,
            Firstname = msg.Firstname,
            Lastname = msg.Lastname,
            Role = Enum.Parse<Roles>(msg.Role.ToString())
        };
        
        await _authService.RegisterUserAsync(request, msg.AccountId, context.CancellationToken);
    }
}