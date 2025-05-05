using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using MailService.Services;

namespace MailService.Tests.Integration;

[Trait("Category", "Integration")]
public class EmailServiceIntegrationTests
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly EmailService _emailService;

    public EmailServiceIntegrationTests()
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

        _logger = loggerFactory.CreateLogger<EmailService>();
        _emailService = new EmailService(_configuration, _logger);
    }

    [Fact]
    public void SendEmail_ValidParameters_EmailSentSuccessfully()
    {
        var email = _configuration["TestEmail"] ?? "test@example.com";
        var subject = "Integration Test Email";
        var message = @"
            <html>
            <body>
                <h2>Hello Test User,</h2>
                <p>This is an integration test email sent from the MailService tests.</p>
                <p>If you are receiving this email, the test was successful.</p>
            </body>
            </html>";


        _emailService.SendEmail(email, subject, message);
    }
} 