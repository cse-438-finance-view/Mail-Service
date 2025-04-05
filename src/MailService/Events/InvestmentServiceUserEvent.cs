using System.Text.Json.Serialization;

namespace MailService.Events;

// Bu sınıf Investment Management Service'den gelen event formatına uygundur
public class InvestmentServiceUserEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("email")]
    public string Email { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("surname")]
    public string Surname { get; set; }

    // Diğer Investment Service özellikler (opsiyonel)
    [JsonPropertyName("userName")]
    public string UserName { get; set; }
    
    [JsonPropertyName("bornDate")]
    public DateTime? BornDate { get; set; }
    
    [JsonPropertyName("createDate")]
    public DateTime? CreateDate { get; set; }
    
    // UserCreatedEvent formatına dönüştürme
    public UserCreatedEvent ToUserCreatedEvent()
    {
        return new UserCreatedEvent(
            Id ?? string.Empty,
            Email ?? string.Empty,
            Name ?? string.Empty,
            Surname ?? string.Empty
        );
    }
} 