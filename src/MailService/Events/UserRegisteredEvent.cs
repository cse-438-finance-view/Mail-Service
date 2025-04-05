namespace MailService.Events;

public class UserRegisteredEvent : IDomainEvent
{
    public string Email { get; set; }
    public string Username { get; set; }
    public string EventType => nameof(UserRegisteredEvent);
    public DateTime OccurredOn { get; private set; }

    public UserRegisteredEvent()
    {
        OccurredOn = DateTime.UtcNow;
    }

    public UserRegisteredEvent(string email, string username) : this()
    {
        Email = email;
        Username = username;
    }
} 