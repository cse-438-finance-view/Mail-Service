using MailService.HostedServices;
using MailService.Services;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Mail Service API", 
        Version = "v1",
        Description = "API for mail microservice",
        Contact = new OpenApiContact
        {
            Name = "Mail Service Team",
            Email = "admin@example.com"
        }
    });
    
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();
builder.Services.AddHostedService<RabbitMQHostedService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mail Service API v1");
        c.RoutePrefix = string.Empty; 
    });
}

app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/", () => "Mail Service is running! Visit /swagger for API documentation.");

app.MapGet("/send-test-mail", (IEmailService mailService) => 
{
    try 
    {
        mailService.SendEmail(
            "test@example.com", 
            "Test Mail", 
            "<h1>Test Mail</h1><p>This is a test mail.</p>"
        );
        return Results.Ok("Test mail sent successfully");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error sending mail: {ex.Message}");
    }
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

// Integration testlerinin erişim sağlayabilmesi için Program sınıfını açıkça tanımlıyoruz
public partial class Program { }
