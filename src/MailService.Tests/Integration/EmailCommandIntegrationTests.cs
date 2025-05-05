using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MailService.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace MailService.Tests.Integration;

[Trait("Category", "Integration")]
public class EmailCommandIntegrationTests 
{
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;
    private readonly HttpClient? _client;

    public EmailCommandIntegrationTests()
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

        _logger = loggerFactory.CreateLogger<EmailCommandIntegrationTests>();
        
        _client = null;
    }

    [Fact]
    public async Task PostEmailCommand_ViaTestController_ReturnsSuccessStatusCode()
    {
        
        var client = new HttpClient { BaseAddress = new Uri("http://localhost:5110") };
        
        var command = new EmailCommand
        {
            Email = "test@example.com",
            Name = "Integration",
            Surname = "Test",
            MailType = "Welcome"
        };

        var response = await client.PostAsJsonAsync("/api/Test/simulate-email-command", command);

        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response: {responseString}");
        Assert.Contains("successful", responseString);
    }

    [Fact]
    public async Task PostFailureEmailCommand_ViaTestController_ReturnsSuccessStatusCode()
    {
        
        var client = new HttpClient { BaseAddress = new Uri("http://localhost:5110") };
        
        var command = new EmailCommand
        {
            Email = "test@example.com",
            Name = "Integration",
            Surname = "Test",
            MailType = "Failure",
            FailureReason = "Test failure reason"
        };

        var response = await client.PostAsJsonAsync("/api/Test/simulate-email-command", command);

        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response: {responseString}");
        Assert.Contains("successful", responseString);
    }
} 