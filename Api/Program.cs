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
using Asp.Versioning;
using Eventify.Payment.Api.Infrastructure.Messaging;
// using PaymentGateway.Api.Middlewares; // For ExceptionHandlingMiddleware

var builder = WebApplication.CreateBuilder(args);

// --- Service Registration ---

// Add global exception handling middleware (custom)
// builder.Services.AddTransient<ExceptionHandlingMiddleware>();

// API Versioning (future-proofing)
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

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

// Validate critical configuration
builder.Services.AddOptions<StripeOptions>()
    .Bind(builder.Configuration.GetSection("Stripe"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<RabbitMqConfig>()
    .Bind(builder.Configuration.GetSection("RabbitMQ"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Auth & Commands
builder.Services.AddSingleton<IJwtValidator, JwtValidator>();
builder.Services.AddScoped<ICommandDispatcher, CommandDispatcher>();

// Command handlers registration (vertical slice)
builder.Services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)))
    .AsImplementedInterfaces()
    .WithScopedLifetime());

// Payment providers (abstracted)
builder.Services.Configure<StripeOptions>(
    builder.Configuration.GetSection("Stripe"));
builder.Services.AddSingleton<IStripeSignatureVerifier, StripeSignatureVerifier>();

// builder.Services.AddScoped<IPaymentProvider, StripePaymentProvider>();
builder.Services.AddScoped<StripePaymentProvider>();
// services.AddScoped<PayPalPaymentProvider>();
builder.Services.AddScoped<IPaymentProviderFactory, PaymentProviderFactory>();


// MassTransit/RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // x.SetKebabCaseEndpointNameFormatter();
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitConfig = builder.Configuration.GetSection("RabbitMQ").Get<RabbitMqConfig>();
        cfg.Host(new Uri(rabbitConfig.Host), h =>
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

// Register idempotency cleanup worker (future-proof, if implemented)
// builder.Services.AddHostedService<IdempotencyCleanupWorker>(); // <-- implement this worker as needed

builder.Services.AddHealthChecks();
builder.Services.AddSingleton<IRabbitMqConnectionChecker, RabbitMqConnectionChecker>();


var stripeWebhookSecret = builder.Configuration["STRIPE_WEBHOOK_SECRET"];
if (string.IsNullOrWhiteSpace(stripeWebhookSecret))
{
    throw new InvalidOperationException("Missing required environment variable: STRIPE_WEBHOOK_SECRET");
}

// --- App Pipeline ---
var app = builder.Build();

// Global exception handling
// app.UseMiddleware<ExceptionHandlingMiddleware>();

// Check RabbitMQ connection before starting
using (var scope = app.Services.CreateScope())
{
    var rabbitChecker = scope.ServiceProvider.GetRequiredService<IRabbitMqConnectionChecker>();
    try
    {
        await rabbitChecker.EnsureConnectionIsAvailableAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogCritical(ex, "RabbitMQ is unavailable. Application will shut down.");
        return;
    }
}

// Check DB connection at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var connection = await db.Database.CanConnectAsync();
    if (connection)
        app.Logger.LogInformation("✅ Successfully connected to the database");
    else
        app.Logger.LogCritical("❌ Failed to connect to the database");
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
    .WithOrigins(builder.Configuration.GetSection("AllowedHosts").Get<string[]>() ?? Array.Empty<string>())
    .AllowAnyMethod()
    .AllowAnyHeader());

app.UseAuthentication();
app.UseAuthorization();

// Routing
app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();

// --- Records & Configs ---
// Change from record to class with parameterless constructor and init-only properties


// ...implement IdempotencyCleanupWorker and ExceptionHandlingMiddleware as needed...