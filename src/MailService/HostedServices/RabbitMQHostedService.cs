using MailService.Services;

namespace MailService.HostedServices;

public class RabbitMQHostedService : BackgroundService
{
    private readonly IRabbitMQService _rabbitMQService;
    private readonly ILogger<RabbitMQHostedService> _logger;

    public RabbitMQHostedService(IRabbitMQService rabbitMQService, ILogger<RabbitMQHostedService> logger)
    {
        _rabbitMQService = rabbitMQService;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        _logger.LogInformation("RabbitMQ Hosted Service is starting");
        
        try
        {
            _rabbitMQService.StartConsuming();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting RabbitMQ consumer");
        }

        return Task.CompletedTask;
    }
} 