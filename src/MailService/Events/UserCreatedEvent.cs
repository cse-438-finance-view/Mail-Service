using System.Text.Json.Serialization;

namespace MailService.Events;

public class UserCreatedEvent : IDomainEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("email")]
    public string Email { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("surname")]
    public string Surname { get; set; }
    
    public string EventType => nameof(UserCreatedEvent);
    public DateTime OccurredOn { get; private set; }

    public UserCreatedEvent()
    {
        OccurredOn = DateTime.UtcNow;
    }

    public UserCreatedEvent(string id, string email, string name, string surname) : this()
    {
        Id = id;
        Email = email;
        Name = name;
        Surname = surname;
    }
} 