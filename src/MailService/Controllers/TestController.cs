using MailService.Events;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace MailService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TestController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TestController> _logger;

    public TestController(IConfiguration configuration, ILogger<TestController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("simulate-user-registered")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult SimulateUserRegistered([FromBody] UserRegistrationRequest request)
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

            int.TryParse(_configuration["RabbitMQ:Port"], out int port);
            if (port > 0)
            {
                factory.Port = port;
            }

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(
                exchange: "domain_events",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            var userRegisteredEvent = new UserRegisteredEvent(request.Email, request.Username);
            
            var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(userRegisteredEvent));
            
            channel.BasicPublish(
                exchange: "domain_events",
                routingKey: "user.registered",
                basicProperties: null,
                body: messageBytes);

            _logger.LogInformation($"Simulated user registered event for {request.Email}");
            
            return Ok(new { message = $"User registration event simulation successful: {request.Email}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during event simulation");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("simulate-investment-user-created")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult SimulateInvestmentUserCreated([FromBody] UserCreationRequest request)
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

            int.TryParse(_configuration["RabbitMQ:Port"], out int port);
            if (port > 0)
            {
                factory.Port = port;
            }

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(
                exchange: "investment_exchange",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            var userCreatedEvent = new UserCreatedEvent(
                request.Id, 
                request.Email, 
                request.Name, 
                request.Surname
            );
            
            var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(userCreatedEvent));
            
            channel.BasicPublish(
                exchange: "investment_exchange",
                routingKey: "UserCreatedEvent",
                basicProperties: null,
                body: messageBytes);

            _logger.LogInformation($"Simulated investment user created event for {request.Email}");
            
            return Ok(new { message = $"Investment user created event simulation successful: {request.Email}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during investment event simulation");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("debug-investment-messages")]
    public IActionResult DebugInvestmentMessages()
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

            int.TryParse(_configuration["RabbitMQ:Port"], out int port);
            if (port > 0)
            {
                factory.Port = port;
            }

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            var queueInfo = channel.QueueDeclarePassive("user_created_queue");
            var messageCount = queueInfo.MessageCount;

            var messages = new List<object>();
            var getInfo = new List<string>();

            if (messageCount > 0)
            {
                int messagesToRead = Math.Min(5, (int)messageCount);
                for (int i = 0; i < messagesToRead; i++)
                {
                    var result = channel.BasicGet("user_created_queue", false);
                    if (result != null)
                    {
                        var body = result.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        getInfo.Add($"Message {i+1}: {message}");
                        
                        try
                        {
                            var jsonObj = JsonSerializer.Deserialize<object>(message);
                            messages.Add(jsonObj);
                        }
                        catch
                        {
                            messages.Add($"(Could not parse as JSON) Raw: {message}");
                        }
                        
                        channel.BasicAck(result.DeliveryTag, false);
                    }
                }
            }

            var testEvent = new UserCreatedEvent(
                "test-id-123", 
                "test@example.com", 
                "Test", 
                "User"
            );
            
            var serializedEvent = JsonSerializer.Serialize(testEvent);

            return Ok(new { 
                QueueMessageCount = messageCount,
                Messages = messages,
                GetInfo = getInfo,
                ExpectedFormat = serializedEvent,
                RabbitMQSettings = new {
                    Host = _configuration["RabbitMQ:Host"],
                    Port = _configuration["RabbitMQ:Port"],
                    VirtualHost = _configuration["RabbitMQ:VirtualHost"]
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error debugging RabbitMQ messages");
            return BadRequest(new { error = ex.ToString() });
        }
    }

    [HttpPost("simulate-exact-investment-message")]
    public IActionResult SimulateExactInvestmentMessage([FromBody] object customMessageObject)
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

            int.TryParse(_configuration["RabbitMQ:Port"], out int port);
            if (port > 0)
            {
                factory.Port = port;
            }

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(
                exchange: "investment_exchange",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);
            
            var message = JsonSerializer.Serialize(customMessageObject);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            
            channel.BasicPublish(
                exchange: "investment_exchange",
                routingKey: "UserCreatedEvent",
                basicProperties: null,
                body: messageBytes);

            _logger.LogInformation($"Published exact simulation message: {message}");
            
            return Ok(new { 
                message = "Exact simulation message published to investment_exchange",
                sentMessage = message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during exact message simulation");
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class UserRegistrationRequest
{
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

public class UserCreationRequest
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
}