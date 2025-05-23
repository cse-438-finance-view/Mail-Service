using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using Microsoft.Extensions.Logging;

namespace MailService.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private const int MaxRetryAttempts = 3;
    private const int DelayBetweenRetriesMs = 2000;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public void SendEmail(string email, string subject, string message)
    {
        var attempt = 0;
        Exception lastException = null;

        while (attempt < MaxRetryAttempts)
        {
            try
            {
                attempt++;
                _logger.LogInformation("Attempting to send email to {Email} (attempt {Attempt}/{MaxAttempts})", 
                    email, attempt, MaxRetryAttempts);

                SendEmailInternal(email, subject, message);
                _logger.LogInformation("Email sent successfully to {Email} on attempt {Attempt}", email, attempt);
                return; // Success - exit retry loop
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Failed to send email to {Email} on attempt {Attempt}/{MaxAttempts}: {ErrorMessage}", 
                    email, attempt, MaxRetryAttempts, ex.Message);

                if (attempt < MaxRetryAttempts)
                {
                    _logger.LogInformation("Retrying in {DelayMs}ms...", DelayBetweenRetriesMs);
                    Thread.Sleep(DelayBetweenRetriesMs);
                }
            }
        }

        // If all retries failed, log the final error but don't throw
        _logger.LogError(lastException, "Failed to send email to {Email} after {MaxAttempts} attempts. Giving up.", 
            email, MaxRetryAttempts);
        
        // Instead of throwing, we could implement a dead letter queue or notification mechanism
        // throw lastException; // ❌ Don't rethrow - handle gracefully
    }

    private void SendEmailInternal(string email, string subject, string message)
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
            // Set timeouts to prevent hanging
            client.Timeout = 30000; // 30 seconds
            
            _logger.LogInformation($"Connecting to SMTP server: {host}:{port}");
            client.Connect(host, port, false);
            client.Authenticate(fromEmail, password);
            client.Send(mimeMessage);
            client.Disconnect(true);
        }
    }

    public void SendEmailWithAttachment(string email, string subject, string message, byte[] attachment, string fileName)
    {
        var attempt = 0;
        Exception lastException = null;

        while (attempt < MaxRetryAttempts)
        {
            try
            {
                attempt++;
                _logger.LogInformation("Attempting to send email with attachment to {Email} (attempt {Attempt}/{MaxAttempts})", 
                    email, attempt, MaxRetryAttempts);

                SendEmailWithAttachmentInternal(email, subject, message, attachment, fileName);
                _logger.LogInformation("Email with attachment sent successfully to {Email} on attempt {Attempt}", email, attempt);
                return; // Success - exit retry loop
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Failed to send email with attachment to {Email} on attempt {Attempt}/{MaxAttempts}: {ErrorMessage}", 
                    email, attempt, MaxRetryAttempts, ex.Message);

                if (attempt < MaxRetryAttempts)
                {
                    _logger.LogInformation("Retrying in {DelayMs}ms...", DelayBetweenRetriesMs);
                    Thread.Sleep(DelayBetweenRetriesMs);
                }
            }
        }

        // If all retries failed, log the final error
        _logger.LogError(lastException, "Failed to send email with attachment to {Email} after {MaxAttempts} attempts. Giving up.", 
            email, MaxRetryAttempts);
    }

    private void SendEmailWithAttachmentInternal(string email, string subject, string message, byte[] attachment, string fileName)
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
            // Set timeouts to prevent hanging
            client.Timeout = 30000; // 30 seconds
            
            _logger.LogInformation($"Connecting to SMTP server: {host}:{port}");
            client.Connect(host, port, false);
            client.Authenticate(fromEmail, passwordKey);
            client.Send(mimeMessage);
            client.Disconnect(true);
        }
    }
} 