namespace InnoClinic.Shared.Events;

public interface IStaffCreatedEvent
{
    Guid AccountId { get; }
    string Email { get; }
    string Password { get; }
    string Firstname { get; }
    string Lastname { get; }
    Roles Role { get; }
}

public enum Roles
{
    Patient = 0,
    Doctor = 1,
    Administrator = 2
}