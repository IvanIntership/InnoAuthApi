namespace InnoClinic.Shared.Events;

public interface IPatientRegisteredEvent
{
    Guid AccountId { get; }
    string Firstname { get; }
    string Lastname { get; }
    string Email { get; }
    string PhoneNumber { get; }
    string Password { get; }
    DateTime Birthday { get; }
}