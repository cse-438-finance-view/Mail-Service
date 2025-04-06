using System.Text;
using System.Text.Json;
using MailService.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MailService.Services;

public class RabbitMQService : IRabbitMQService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMQService> _logger;
    private readonly IEmailService _emailService;
    private IConnection _connection;
    private IModel _channel;

    public RabbitMQService(
        IConfiguration configuration,
        ILogger<RabbitMQService> logger,
        IEmailService emailService)
    {
        _configuration = configuration;
        _logger = logger;
        _emailService = emailService;
        SetupRabbitMQ();
    }

    private void SetupRabbitMQ()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"],
                UserName = _configuration["RabbitMQ:UserName"],
                Password = _configuration["RabbitMQ:Password"],
                VirtualHost = _configuration["RabbitMQ:VirtualHost"]
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(
                exchange: "domain_events",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _channel.QueueDeclare(
                queue: "user_registered_queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.QueueBind(
                queue: "user_registered_queue",
                exchange: "domain_events",
                routingKey: "user.registered");

            _channel.ExchangeDeclare(
                exchange: "investment_exchange",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _channel.QueueDeclare(
                queue: "user_created_queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.QueueBind(
                queue: "user_created_queue",
                exchange: "investment_exchange",
                routingKey: "UserCreatedEvent");

            _channel.QueueBind(
                queue: "user_created_queue",
                exchange: "investment_exchange",
                routingKey: "User.Created");
            
            _channel.QueueBind(
                queue: "user_created_queue",
                exchange: "investment_exchange",
                routingKey: "user.created");

            var sagaExchange = _configuration["RabbitMQ:SagaExchange"];
            var sagaQueue = _configuration["RabbitMQ:SagaQueue"];
            var sagaRoutingKey = _configuration["RabbitMQ:SagaRoutingKey"];

            _channel.ExchangeDeclare(
                exchange: sagaExchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _channel.QueueDeclare(
                queue: sagaQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.QueueBind(
                queue: sagaQueue,
                exchange: sagaExchange,
                routingKey: sagaRoutingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error establishing RabbitMQ connection");
            throw;
        }
    }

    public void StartConsuming()
    {
        var userRegisteredConsumer = new EventingBasicConsumer(_channel);
        
        userRegisteredConsumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var routingKey = ea.RoutingKey;

                _logger.LogInformation($"Message received. Routing key: {routingKey}");

                if (routingKey == "user.registered")
                {
                    var @event = JsonSerializer.Deserialize<UserRegisteredEvent>(message);
                    if (@event != null)
                    {
                        HandleUserRegisteredEvent(@event);
                    }
                }

                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(
            queue: "user_registered_queue",
            autoAck: false,
            consumer: userRegisteredConsumer);

        var userCreatedConsumer = new EventingBasicConsumer(_channel);
        
        userCreatedConsumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var routingKey = ea.RoutingKey;

                _logger.LogInformation($"Message received from investment_exchange. Routing key: {routingKey}");
                _logger.LogInformation($"Raw message content: {message}");

                if (routingKey == "UserCreatedEvent" || routingKey == "User.Created" || routingKey == "user.created")
                {
                    try 
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            AllowTrailingCommas = true,
                            ReadCommentHandling = JsonCommentHandling.Skip
                        };
                        
                        var investmentEvent = JsonSerializer.Deserialize<InvestmentServiceUserEvent>(message, options);
                        
                        _logger.LogInformation($"Raw investment event: Id={investmentEvent?.Id}, Email={investmentEvent?.Email}, " +
                                              $"Name={investmentEvent?.Name}, Surname={investmentEvent?.Surname}, " + 
                                              $"UserName={investmentEvent?.UserName}");
                        
                        if (investmentEvent != null)
                        {
                            var @event = investmentEvent.ToUserCreatedEvent();
                            _logger.LogInformation($"Converted event: Id={@event.Id}, Email={@event.Email}, Name={@event.Name}, Surname={@event.Surname}");
                            
                            HandleUserCreatedEvent(@event);
                        }
                        else
                        {
                            _logger.LogWarning("Could not deserialize as InvestmentServiceUserEvent, trying UserCreatedEvent format");
                            var @event = JsonSerializer.Deserialize<UserCreatedEvent>(message, options);
                            
                            if (@event != null)
                            {
                                _logger.LogInformation($"Deserialized as UserCreatedEvent: Id={@event.Id}, Email={@event.Email}");
                                HandleUserCreatedEvent(@event);
                            }
                            else
                            {
                                _logger.LogError("Failed to deserialize message in any known format");
                            }
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "JSON deserialization error: {ErrorMessage}. Raw message: {Message}", jsonEx.Message, message);
                        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false); 
                    }
                }

                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from investment_exchange");
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(
            queue: "user_created_queue",
            autoAck: false,
            consumer: userCreatedConsumer);

        var emailCommandConsumer = new EventingBasicConsumer(_channel);
        var sagaQueue = _configuration["RabbitMQ:SagaQueue"];
        
        emailCommandConsumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var routingKey = ea.RoutingKey;

                _logger.LogInformation($"Message received from saga exchange. Routing key: {routingKey}");
                _logger.LogInformation($"Saga message content: {message}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };
                
                var command = JsonSerializer.Deserialize<EmailCommand>(message, options);
                
                if (command != null)
                {
                    _logger.LogInformation($"Processing email command: Email={command.Email}, MailType={command.MailType}");
                    HandleEmailCommand(command);
                }
                else
                {
                    _logger.LogError("Failed to deserialize email command");
                }

                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from saga exchange");
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(
            queue: sagaQueue,
            autoAck: false,
            consumer: emailCommandConsumer);
            
        _logger.LogInformation("Started consuming messages from all queues");
    }

    public void HandleUserRegisteredEvent(UserRegisteredEvent @event)
    {
        _logger.LogInformation($"Processing user registration event: {@event.Email}");
        
        var subject = "Welcome - Your Account Has Been Created Successfully";
        var message = $@"
            <html>
            <body>
                <h2>Hello {@event.Username},</h2>
                <p>Your account has been successfully created. You can now use our system.</p>
                <p>Thank you.</p>
            </body>
            </html>";

        _emailService.SendEmail(@event.Email, subject, message);
    }

    public void HandleUserCreatedEvent(UserCreatedEvent @event)
    {
        _logger.LogInformation($"Processing user created event from investment service: {@event.Email}");
        
        if (string.IsNullOrEmpty(@event.Email))
        {
            _logger.LogError("Cannot send email: Email address is null or empty in the received event");
            return;
        }
        
        var subject = "Welcome to Investment Management Service";
        var fullName = $"{@event.Name} {@event.Surname}";
        
        if (string.IsNullOrEmpty(@event.Name) && string.IsNullOrEmpty(@event.Surname))
        {
            fullName = "Valued Customer";
        }
        else if (string.IsNullOrEmpty(@event.Name))
        {
            fullName = @event.Surname;
        }
        else if (string.IsNullOrEmpty(@event.Surname))
        {
            fullName = @event.Name;
        }
        
        var message = $@"
            <html>
            <body>
                <h2>Hello {fullName},</h2>
                <p>Your investment management account has been successfully created.</p>
                <p>You can now access all our investment services and manage your portfolio.</p>
                <p>If you have any questions, please contact our support team.</p>
                <p>Thank you for choosing our platform!</p>
            </body>
            </html>";

        _emailService.SendEmail(@event.Email, subject, message);
    }

    public void HandleEmailCommand(EmailCommand command)
    {
        _logger.LogInformation($"Processing email command: {command.Email}, Type: {command.MailType}");
        
        if (string.IsNullOrEmpty(command.Email))
        {
            _logger.LogError("Cannot send email: Email address is null or empty in the received command");
            return;
        }
        
        var fullName = GetFullName(command.Name, command.Surname);
        
        if (command.MailType == "Welcome")
        {
            var subject = "Welcome - Your Account Has Been Created Successfully";
            var message = $@"
                <html>
                <body>
                    <h2>Hello {fullName},</h2>
                    <p>Your account has been successfully created. You can now use our system.</p>
                    <p>Thank you for joining our platform!</p>
                </body>
                </html>";

            _emailService.SendEmail(command.Email, subject, message);
        }
        else if (command.MailType == "Failure")
        {
            var subject = "Account Creation Failed";
            var message = $@"
                <html>
                <body>
                    <h2>Hello {fullName},</h2>
                    <p>We're sorry, but we couldn't create your account.</p>
                    <p>Reason: {command.FailureReason ?? "Unknown error"}</p>
                    <p>Please try again or contact our support team for assistance.</p>
                </body>
                </html>";

            _emailService.SendEmail(command.Email, subject, message);
        }
        else
        {
            _logger.LogWarning($"Unknown mail type received: {command.MailType}");
        }
    }

    private string GetFullName(string? firstName, string? lastName)
    {
        if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName))
        {
            return "Valued Customer";
        }
        else if (string.IsNullOrEmpty(firstName))
        {
            return lastName!;
        }
        else if (string.IsNullOrEmpty(lastName))
        {
            return firstName;
        }
        
        return $"{firstName} {lastName}";
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
} 