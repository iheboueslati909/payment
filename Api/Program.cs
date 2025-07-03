using PaymentGateway.Infrastructure.Auth;
using PaymentGateway.Infrastructure.Database;
using PaymentGateway.Infrastructure.PaymentProviders;
using PaymentGateway.Common.Interfaces;
using PaymentGateway.Infrastructure.PaymentProviders.Stripe;
using PaymentGateway.Infrastructure.Commands;
using PaymentGateway.Infrastructure.Outbox;
using PaymentGateway.BackgroundWorkers;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using PaymentGateway.Features.Payments.Webhook;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults
// builder.Services.AddServiceDefaults();

// Core services
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        x => x.MigrationsAssembly(typeof(Program).Assembly.GetName().Name)
        ));

// Auth & Commands
builder.Services.AddSingleton<IJwtValidator, JwtValidator>();
builder.Services.AddScoped<ICommandDispatcher, CommandDispatcher>();

// Command handlers registration
builder.Services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)))
    .AsImplementedInterfaces()
    .WithScopedLifetime());

// Payment providers
builder.Services.Configure<StripeOptions>(
    builder.Configuration.GetSection("Stripe"));
builder.Services.AddScoped<IPaymentProvider, StripePaymentProvider>();
// Stripe signature verification
builder.Services.AddSingleton<IStripeSignatureVerifier, StripeSignatureVerifier>();

// MassTransit/RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitConfig = builder.Configuration.GetSection("RabbitMQ").Get<RabbitMqConfig>();
        cfg.Host(rabbitConfig!.Host, "/", h =>
        {
            h.Username(rabbitConfig.Username);
            h.Password(rabbitConfig.Password);
        });
        
        cfg.ConfigureEndpoints(context);
    });
});

// Outbox Pattern
builder.Services.AddScoped<OutboxProcessor>();
builder.Services.AddHostedService<OutboxWorker>();
builder.Services.AddHealthChecks();

var app = builder.Build();

var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
using (var scope = scopeFactory.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";

    var connection = await db.Database.CanConnectAsync();
    if (connection)
    {
        Console.WriteLine($"✅ Successfully connected to the database");
    }
    else
    {
        Console.WriteLine($"❌ Failed to connect to the database");
    }
}

// Development middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

// Security middleware
app.UseHttpsRedirection();
app.UseCors(x => x
    .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
    .AllowAnyMethod()
    .AllowAnyHeader());

app.UseAuthentication();
app.UseAuthorization();

// Routing
app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();

public record RabbitMqConfig(string Host, string Username, string Password);