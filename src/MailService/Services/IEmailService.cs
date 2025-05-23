namespace MailService.Services;
 
public interface IEmailService
{
    void SendEmail(string email, string subject, string message);
    void SendEmailWithAttachment(string email, string subject, string message, byte[] attachment, string fileName);
} 