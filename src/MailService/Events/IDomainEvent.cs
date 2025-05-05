namespace MailService.Events;
 
public interface IDomainEvent
{
    string EventType { get; }
    DateTime OccurredOn { get; }
} 