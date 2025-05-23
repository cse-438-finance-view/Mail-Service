using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using Microsoft.Extensions.Logging;

namespace MailService.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public void SendEmail(string email, string subject, string message)
    {
        try
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentNullException(nameof(email), "Email address cannot be null or empty");
            }
            
            var fromName = _configuration["MailService:Name"] ?? "Mail Service";
            var fromEmail = _configuration["MailService:Email"];
            
            if (string.IsNullOrEmpty(fromEmail))
            {
                throw new InvalidOperationException("Sender email address is not configured. Check MailService:Email in configuration.");
            }
            
            MimeMessage mimeMessage = new MimeMessage();
            MailboxAddress from = new MailboxAddress(fromName, fromEmail);
            MailboxAddress to = new MailboxAddress(email, email);

            mimeMessage.From.Add(from);
            mimeMessage.To.Add(to);

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = message;
            mimeMessage.Body = bodyBuilder.ToMessageBody();
            mimeMessage.Subject = subject ?? "No Subject";

            int port;
            int.TryParse(_configuration["MailService:Port"], out port);
            
            if (port <= 0)
            {
                port = 587; 
                _logger.LogWarning("Invalid or missing SMTP port in configuration. Using default port 587.");
            }
            
            var host = _configuration["MailService:Host"];
            if (string.IsNullOrEmpty(host))
            {
                throw new InvalidOperationException("SMTP host is not configured. Check MailService:Host in configuration.");
            }
            
            var password = _configuration["MailService:Key"];
            if (string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("SMTP password/key is not configured. Check MailService:Key in configuration.");
            }

            using (SmtpClient client = new SmtpClient())
            {
                _logger.LogInformation($"Connecting to SMTP server: {host}:{port}");
                client.Connect(host, port, false);
                client.Authenticate(fromEmail, password);
                client.Send(mimeMessage);
                client.Disconnect(true);
                _logger.LogInformation($"Email sent successfully to {email}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email}: {ErrorMessage}", email, ex.Message);
            throw;
        }
    }

    public void SendEmailWithAttachment(string email, string subject, string message, byte[] attachment, string fileName)
    {
        if (string.IsNullOrEmpty(email))
        {
            throw new ArgumentNullException(nameof(email), "Email address cannot be null or empty");
        }
        var fromName = _configuration["MailService:Name"] ?? "Mail Service";
        var fromEmail = _configuration["MailService:Email"];
        if (string.IsNullOrEmpty(fromEmail))
        {
            throw new InvalidOperationException("Sender email address is not configured. Check MailService:Email in configuration.");
        }
        MimeMessage mimeMessage = new MimeMessage();
        MailboxAddress from = new MailboxAddress(fromName, fromEmail);
        MailboxAddress to = new MailboxAddress(email, email);
        mimeMessage.From.Add(from);
        mimeMessage.To.Add(to);
        var bodyBuilder = new BodyBuilder();
        bodyBuilder.HtmlBody = message;
        // Add PDF attachment if present
        if (attachment != null && !string.IsNullOrEmpty(fileName))
        {
            bodyBuilder.Attachments.Add(fileName, attachment);
        }
        mimeMessage.Body = bodyBuilder.ToMessageBody();
        mimeMessage.Subject = subject ?? "No Subject";
        int port;
        int.TryParse(_configuration["MailService:Port"], out port);
        if (port <= 0)
        {
            port = 587;
            _logger.LogWarning("Invalid or missing SMTP port in configuration. Using default port 587.");
        }
        var host = _configuration["MailService:Host"];
        if (string.IsNullOrEmpty(host))
        {
            throw new InvalidOperationException("SMTP host is not configured. Check MailService:Host in configuration.");
        }
        var passwordKey = _configuration["MailService:Key"];
        if (string.IsNullOrEmpty(passwordKey))
        {
            throw new InvalidOperationException("SMTP password/key is not configured. Check MailService:Key in configuration.");
        }
        using (SmtpClient client = new SmtpClient())
        {
            _logger.LogInformation($"Connecting to SMTP server: {host}:{port}");
            client.Connect(host, port, false);
            client.Authenticate(fromEmail, passwordKey);
            client.Send(mimeMessage);
            client.Disconnect(true);
            _logger.LogInformation($"Email with attachment sent successfully to {email}");
        }
    }
} 