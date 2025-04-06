using MailService.Events;

namespace MailService.Services;

public interface IRabbitMQService
{
    void StartConsuming();
    void HandleUserRegisteredEvent(UserRegisteredEvent @event);
    void HandleUserCreatedEvent(UserCreatedEvent @event);
    void HandleEmailCommand(EmailCommand command);
} 