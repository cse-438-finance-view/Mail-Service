using System.Text.Json.Serialization;

namespace MailService.Events;

public class EmailCommand
{
    [JsonPropertyName("email")]
    public string Email { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("surname")]
    public string? Surname { get; set; }
    
    [JsonPropertyName("mailType")]
    public string MailType { get; set; }
    
    [JsonPropertyName("failureReason")]
    public string? FailureReason { get; set; }
    
    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }
    
    [JsonPropertyName("pdfBase64")]
    public string? PdfBase64 { get; set; }
} 