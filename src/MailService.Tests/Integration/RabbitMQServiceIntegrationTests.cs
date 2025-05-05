using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Xunit;
using MailService.Events;
using MailService.Services;
using Moq;

namespace MailService.Tests.Integration;

[Trait("Category", "Integration")]
public class RabbitMQServiceIntegrationTests : IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMQService> _logger;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly RabbitMQService? _rabbitMQService;
    private IConnection? _connection;
    private IModel? _channel;
    private bool _isConnected = false;

    public RabbitMQServiceIntegrationTests()
    {
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.test.json", optional: true)
            .AddEnvironmentVariables();

        _configuration = configBuilder.Build();
        
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("MailService", LogLevel.Debug)
                .AddConsole();
        });

        _logger = loggerFactory.CreateLogger<RabbitMQService>();
        _emailServiceMock = new Mock<IEmailService>();
        
        _emailServiceMock.Setup(e => e.SendEmail(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>()))
            .Callback<string, string, string>((email, subject, message) => 
            {
                Console.WriteLine($">>> SendEmail called with email={email}, subject={subject}");
                Console.WriteLine($">>> Message content: {message}");
            });
        
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest",
            VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/"
        };

        if (int.TryParse(_configuration["RabbitMQ:Port"], out int port) && port > 0)
        {
            factory.Port = port;
        }

        try 
        {
            Console.WriteLine("Attempting to connect to RabbitMQ...");
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            if (_connection.IsOpen && _channel.IsOpen)
            {
                Console.WriteLine("Successfully connected to RabbitMQ");
                _isConnected = true;
                
                DeclareQueuesAndExchanges();
                _rabbitMQService = new RabbitMQService(_configuration, _logger, _emailServiceMock.Object);
            }
            else
            {
                Console.WriteLine("Failed to open RabbitMQ connection or channel");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to RabbitMQ: {ex.Message}");
            _logger.LogError(ex, "Failed to connect to RabbitMQ for integration tests. Tests will be skipped.");
            _rabbitMQService = null;
            _connection = null;
            _channel = null;
        }
    }

    private void DeclareQueuesAndExchanges()
    {
        if (_channel == null) return;
        
        try 
        {
            var exchange = _configuration["RabbitMQ:SagaExchange"] ?? "saga.commands";
            var queue = _configuration["RabbitMQ:SagaQueue"] ?? "email.command.queue";
            var routingKey = _configuration["RabbitMQ:SagaRoutingKey"] ?? "saga.email.command";
            
            Console.WriteLine($"Declaring exchange: {exchange}, queue: {queue}, routing key: {routingKey}");
            
            _channel.ExchangeDeclare(
                exchange: exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _channel.QueueDeclare(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            _channel.QueueBind(
                queue: queue,
                exchange: exchange,
                routingKey: routingKey);
                
            Console.WriteLine("Queue and exchange declared successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error declaring queues and exchanges: {ex.Message}");
        }
    }


    
    [Fact]
    public void RabbitMQService_HandleEmailCommandDirectly_SendsEmail()
    {
        var service = new RabbitMQService(_configuration, _logger, _emailServiceMock.Object);
        var command = new EmailCommand
        {
            Email = "test@example.com",
            Name = "Test",
            Surname = "User",
            MailType = "Failure",
            FailureReason = "Account already exists"
        };

        service.HandleEmailCommand(command);

        _emailServiceMock.Verify(
            e => e.SendEmail(
                It.Is<string>(email => email == command.Email),
                It.Is<string>(subject => subject.Contains("Failed")),
                It.Is<string>(message => message.Contains(command.Name) && 
                                        message.Contains(command.Surname) && 
                                        message.Contains(command.FailureReason))),
            Times.Once);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        (_rabbitMQService as IDisposable)?.Dispose();
    }
} 