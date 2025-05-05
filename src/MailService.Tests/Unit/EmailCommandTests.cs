using System.Text.Json;
using MailService.Events;
using Xunit;

namespace MailService.Tests.Unit;

public class EmailCommandTests
{
    [Fact]
    public void EmailCommand_SerializeDeserialize_PreservesValues()
    {
        var command = new EmailCommand
        {
            Email = "test@example.com",
            Name = "Test",
            Surname = "User",
            MailType = "Welcome",
            FailureReason = null
        };

        var json = JsonSerializer.Serialize(command);
        var deserializedCommand = JsonSerializer.Deserialize<EmailCommand>(json);

        Assert.NotNull(deserializedCommand);
        Assert.Equal(command.Email, deserializedCommand.Email);
        Assert.Equal(command.Name, deserializedCommand.Name);
        Assert.Equal(command.Surname, deserializedCommand.Surname);
        Assert.Equal(command.MailType, deserializedCommand.MailType);
        Assert.Equal(command.FailureReason, deserializedCommand.FailureReason);
    }

    [Fact]
    public void EmailCommand_SerializeWithPropertyNames_UsesJsonPropertyNames()
    {
        var command = new EmailCommand
        {
            Email = "test@example.com",
            Name = "Test",
            Surname = "User",
            MailType = "Welcome",
            FailureReason = "Test failure"
        };

        var json = JsonSerializer.Serialize(command);

        Assert.Contains("\"email\":\"test@example.com\"", json);
        Assert.Contains("\"name\":\"Test\"", json);
        Assert.Contains("\"surname\":\"User\"", json);
        Assert.Contains("\"mailType\":\"Welcome\"", json);
        Assert.Contains("\"failureReason\":\"Test failure\"", json);
    }

    [Fact]
    public void EmailCommand_DeserializeFromJson_HandlesNullValues()
    {
        var json = @"{
            ""email"": ""test@example.com"",
            ""name"": null,
            ""surname"": null,
            ""mailType"": ""Welcome"",
            ""failureReason"": null
        }";

        var command = JsonSerializer.Deserialize<EmailCommand>(json);

        Assert.NotNull(command);
        Assert.Equal("test@example.com", command.Email);
        Assert.Null(command.Name);
        Assert.Null(command.Surname);
        Assert.Equal("Welcome", command.MailType);
        Assert.Null(command.FailureReason);
    }

    [Fact]
    public void EmailCommand_DeserializeWithMissingProperties_UsesDefaultValues()
    {
        var json = @"{
            ""email"": ""test@example.com"",
            ""mailType"": ""Welcome""
        }";

        var command = JsonSerializer.Deserialize<EmailCommand>(json);

        Assert.NotNull(command);
        Assert.Equal("test@example.com", command.Email);
        Assert.Null(command.Name);
        Assert.Null(command.Surname);
        Assert.Equal("Welcome", command.MailType);
        Assert.Null(command.FailureReason);
    }
} 