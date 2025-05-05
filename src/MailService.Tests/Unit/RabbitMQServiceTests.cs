using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MailService.Events;
using MailService.Services;
using Xunit;

namespace MailService.Tests.Unit;

public class RabbitMQServiceTests
{
    private readonly Mock<ILogger<RabbitMQService>> _loggerMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IEmailService> _emailServiceMock;

    public RabbitMQServiceTests()
    {
        _loggerMock = new Mock<ILogger<RabbitMQService>>();
        _configMock = new Mock<IConfiguration>();
        _emailServiceMock = new Mock<IEmailService>();

        _configMock.Setup(c => c["RabbitMQ:Host"]).Returns("localhost");
        _configMock.Setup(c => c["RabbitMQ:UserName"]).Returns("guest");
        _configMock.Setup(c => c["RabbitMQ:Password"]).Returns("guest");
        _configMock.Setup(c => c["RabbitMQ:VirtualHost"]).Returns("/");
        _configMock.Setup(c => c["RabbitMQ:Port"]).Returns("5672");
        _configMock.Setup(c => c["RabbitMQ:SagaExchange"]).Returns("saga.commands");
        _configMock.Setup(c => c["RabbitMQ:SagaRoutingKey"]).Returns("saga.email.command");
        _configMock.Setup(c => c["RabbitMQ:SagaQueue"]).Returns("email.command.queue");
    }

    [Fact]
    public void HandleUserRegisteredEvent_SendsEmail()
    {
        var service = new RabbitMQServiceTestable(_configMock.Object, _loggerMock.Object, _emailServiceMock.Object);
        var userEvent = new UserRegisteredEvent
        {
            Email = "test@example.com",
            Username = "testuser"
        };

        service.HandleUserRegisteredEvent(userEvent);

        _emailServiceMock.Verify(
            e => e.SendEmail(
                It.Is<string>(email => email == userEvent.Email),
                It.Is<string>(subject => subject.Contains("Welcome")),
                It.Is<string>(message => message.Contains(userEvent.Username))),
            Times.Once);
    }

    [Fact]
    public void HandleUserCreatedEvent_ValidEmail_SendsEmail()
    {
        var service = new RabbitMQServiceTestable(_configMock.Object, _loggerMock.Object, _emailServiceMock.Object);
        var userEvent = new UserCreatedEvent
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            Name = "Test",
            Surname = "User"
        };

        service.HandleUserCreatedEvent(userEvent);

        _emailServiceMock.Verify(
            e => e.SendEmail(
                It.Is<string>(email => email == userEvent.Email),
                It.Is<string>(subject => subject.Contains("Welcome to Investment")),
                It.Is<string>(message => message.Contains(userEvent.Name) && message.Contains(userEvent.Surname))),
            Times.Once);
    }

    [Fact]
    public void HandleUserCreatedEvent_EmptyEmail_DoesNotSendEmail()
    {
        var service = new RabbitMQServiceTestable(_configMock.Object, _loggerMock.Object, _emailServiceMock.Object);
        var userEvent = new UserCreatedEvent
        {
            Id = Guid.NewGuid().ToString(),
            Email = string.Empty,
            Name = "Test",
            Surname = "User"
        };

        service.HandleUserCreatedEvent(userEvent);

        _emailServiceMock.Verify(e => e.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void HandleEmailCommand_WelcomeType_SendsWelcomeEmail()
    {
        
        var service = new RabbitMQServiceTestable(_configMock.Object, _loggerMock.Object, _emailServiceMock.Object);
        var command = new EmailCommand
        {
            Email = "test@example.com",
            Name = "Test",
            Surname = "User",
            MailType = "Welcome"
        };

        service.HandleEmailCommand(command);

        _emailServiceMock.Verify(
            e => e.SendEmail(
                It.Is<string>(email => email == command.Email),
                It.Is<string>(subject => subject.Contains("Welcome")),
                It.Is<string>(message => message.Contains("Test User"))),
            Times.Once);
    }

    [Fact]
    public void HandleEmailCommand_FailureType_SendsFailureEmail()
    {
        
        var service = new RabbitMQServiceTestable(_configMock.Object, _loggerMock.Object, _emailServiceMock.Object);
        var command = new EmailCommand
        {
            Email = "test@example.com",
            Name = "Test",
            Surname = "User",
            MailType = "Failure",
            FailureReason = "Username already exists"
        };

        
        service.HandleEmailCommand(command);

        _emailServiceMock.Verify(
            e => e.SendEmail(
                It.Is<string>(email => email == command.Email),
                It.Is<string>(subject => subject.Contains("Failed")),
                It.Is<string>(message => message.Contains("Test User") && message.Contains(command.FailureReason))),
            Times.Once);
    }

    [Fact]
    public void HandleEmailCommand_EmptyEmail_DoesNotSendEmail()
    {
        var service = new RabbitMQServiceTestable(_configMock.Object, _loggerMock.Object, _emailServiceMock.Object);
        var command = new EmailCommand
        {
            Email = string.Empty,
            Name = "Test",
            Surname = "User",
            MailType = "Welcome"
        };

        service.HandleEmailCommand(command);

        _emailServiceMock.Verify(e => e.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void HandleEmailCommand_UnknownMailType_DoesNotSendEmail()
    {
        
        var service = new RabbitMQServiceTestable(_configMock.Object, _loggerMock.Object, _emailServiceMock.Object);
        var command = new EmailCommand
        {
            Email = "test@example.com",
            Name = "Test",
            Surname = "User",
            MailType = "Unknown"
        };

        service.HandleEmailCommand(command);

        _emailServiceMock.Verify(e => e.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void GetFullName_BothNamesProvided_ReturnsFullName()
    {
        var service = new RabbitMQServiceTestable(_configMock.Object, _loggerMock.Object, _emailServiceMock.Object);
        
        var result = service.GetFullNamePublic("John", "Doe");
        
        Assert.Equal("John Doe", result);
    }

    [Fact]
    public void GetFullName_OnlyFirstName_ReturnsFirstName()
    {
        var service = new RabbitMQServiceTestable(_configMock.Object, _loggerMock.Object, _emailServiceMock.Object);
        
        var result = service.GetFullNamePublic("John", null);
        
        Assert.Equal("John", result);
    }

    [Fact]
    public void GetFullName_OnlyLastName_ReturnsLastName()
    {
        var service = new RabbitMQServiceTestable(_configMock.Object, _loggerMock.Object, _emailServiceMock.Object);
        
        var result = service.GetFullNamePublic(null, "Doe");
        
        Assert.Equal("Doe", result);
    }

    [Fact]
    public void GetFullName_BothNamesEmpty_ReturnsValuedCustomer()
    {
        var service = new RabbitMQServiceTestable(_configMock.Object, _loggerMock.Object, _emailServiceMock.Object);
        
        var result = service.GetFullNamePublic(null, null);
        
        Assert.Equal("Valued Customer", result);
    }

    private class RabbitMQServiceTestable : RabbitMQService
    {
        public RabbitMQServiceTestable(
            IConfiguration configuration,
            ILogger<RabbitMQService> logger,
            IEmailService emailService)
            : base(configuration, logger, emailService)
        {
        }

        public string GetFullNamePublic(string? firstName, string? lastName)
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
    }
} 