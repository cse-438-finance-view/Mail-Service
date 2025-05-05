using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MailService.Services;
using Xunit;

namespace MailService.Tests.Unit;

public class EmailServiceTests
{
    private readonly Mock<ILogger<EmailService>> _loggerMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly EmailService _emailService;

    public EmailServiceTests()
    {
        _loggerMock = new Mock<ILogger<EmailService>>();
        _configMock = new Mock<IConfiguration>();

        _configMock.Setup(c => c["MailService:Name"]).Returns("Test Mail Service");
        _configMock.Setup(c => c["MailService:Email"]).Returns("test@example.com");
        _configMock.Setup(c => c["MailService:Host"]).Returns("smtp.example.com");
        _configMock.Setup(c => c["MailService:Port"]).Returns("587");
        _configMock.Setup(c => c["MailService:Key"]).Returns("test-password");

        _emailService = new EmailService(_configMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void SendEmail_NullOrEmptyEmail_ThrowsArgumentNullException()
    {
        string? email = null;
        string subject = "Test Subject";
        string message = "Test Message";

        Assert.Throws<ArgumentNullException>(() => _emailService.SendEmail(email, subject, message));
    }

    [Fact]
    public void SendEmail_EmptyEmail_ThrowsArgumentNullException()
    {
        string email = string.Empty;
        string subject = "Test Subject";
        string message = "Test Message";

        Assert.Throws<ArgumentNullException>(() => _emailService.SendEmail(email, subject, message));
    }

    [Fact]
    public void SendEmail_MissingFromEmail_ThrowsInvalidOperationException()
    {
        _configMock.Setup(c => c["MailService:Email"]).Returns((string)null);
        var emailService = new EmailService(_configMock.Object, _loggerMock.Object);

        var exception = Assert.Throws<InvalidOperationException>(() => 
            emailService.SendEmail("recipient@example.com", "Test Subject", "Test Message"));
        
        Assert.Contains("Sender email address is not configured", exception.Message);
    }

    [Fact]
    public void SendEmail_MissingHost_ThrowsInvalidOperationException()
    {
        _configMock.Setup(c => c["MailService:Host"]).Returns((string)null);
        var emailService = new EmailService(_configMock.Object, _loggerMock.Object);

        var exception = Assert.Throws<InvalidOperationException>(() => 
            emailService.SendEmail("recipient@example.com", "Test Subject", "Test Message"));
        
        Assert.Contains("SMTP host is not configured", exception.Message);
    }

    [Fact]
    public void SendEmail_MissingPassword_ThrowsInvalidOperationException()
    {
        _configMock.Setup(c => c["MailService:Key"]).Returns((string)null);
        var emailService = new EmailService(_configMock.Object, _loggerMock.Object);

        var exception = Assert.Throws<InvalidOperationException>(() => 
            emailService.SendEmail("recipient@example.com", "Test Subject", "Test Message"));
        
        Assert.Contains("SMTP password/key is not configured", exception.Message);
    }

    [Fact]
    public void SendEmail_InvalidPort_UsesDefaultPort()
    {
        _configMock.Setup(c => c["MailService:Port"]).Returns("invalid");
        var emailService = new EmailService(_configMock.Object, _loggerMock.Object);

      
        var exception = Assert.ThrowsAny<Exception>(() => 
            emailService.SendEmail("recipient@example.com", "Test Subject", "Test Message"));
        
        Assert.DoesNotContain("Invalid or missing SMTP port", exception.Message);
    }
} 