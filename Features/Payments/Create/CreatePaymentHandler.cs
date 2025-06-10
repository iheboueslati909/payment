using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Common.Enums;
using PaymentGateway.Common.Interfaces;
using PaymentGateway.Features.Idempotency;
using PaymentGateway.Features.Payments.Models;
using PaymentGateway.Infrastructure.Auth;
using PaymentGateway.Infrastructure.Database;
using PaymentGateway.Infrastructure.PaymentProviders;
using PaymentGateway.Infrastructure.Outbox;
using PaymentGateway.Infrastructure.Messaging.Contracts;

namespace PaymentGateway.Features.Payments.Create;

public class CreatePaymentHandler : ICommandHandler<CreatePaymentCommand, CreatePaymentResult>
{
    private readonly IHttpContextAccessor _httpContext;
    private readonly IJwtValidator _jwtValidator;
    private readonly IPaymentProvider _paymentProvider;
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public CreatePaymentHandler(
        IHttpContextAccessor httpContext,
        IJwtValidator jwtValidator,
        IPaymentProvider paymentProvider,
        AppDbContext dbContext,
        IConfiguration configuration)
    {
        _httpContext = httpContext;
        _jwtValidator = jwtValidator;
        _paymentProvider = paymentProvider;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<CreatePaymentResult> Handle(
        CreatePaymentCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Validate JWT and extract appId
        var token = _httpContext.HttpContext?.Request.Headers["Authorization"]
            .ToString().Replace("Bearer ", "");

        if (string.IsNullOrEmpty(token))
        {
            return CreatePaymentResult.Failed("No authorization token provided");
        }

        var jwtKey = _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtKey))
        {
            return CreatePaymentResult.Failed("Invalid JWT configuration - missing JWT key");
        }

        var jwtResult = await _jwtValidator.ValidateToken(token, jwtKey);
        if (!jwtResult.IsValid)
        {
            return CreatePaymentResult.Failed($"Invalid token: {jwtResult.Error}");
        }

        // 2. Check idempotency
        var existingKey = await _dbContext.Set<IdempotencyKey>()
            .FirstOrDefaultAsync(x => 
                x.Key == command.IdempotencyKey && 
                x.AppId == jwtResult.AppId && 
                x.UserId == command.UserId,
                cancellationToken);

        if (existingKey?.LinkedEntityId != null)
        {
            var existingPayment = await _dbContext.Payments
                .FirstAsync(p => p.Id == Guid.Parse(existingKey.LinkedEntityId), cancellationToken);
            return CreatePaymentResult.Succeeded(existingPayment.Id, existingPayment.ProviderPaymentId!);
        }

        // 3. Create Payment entity
        var payment = new Features.Payments.Models.Payment
        {
            Id = Guid.NewGuid(),
            AppId = jwtResult.AppId!,
            UserId = command.UserId,
            Amount = command.Amount,
            Currency = command.Currency,
            Provider = "Stripe",
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // 4. Process payment with Stripe
        try
        {
            var chargeResult = await _paymentProvider.ChargeAsync(
                payment.Id,
                payment.Amount,
                payment.Currency,
                payment.UserId,
                command.PaymentMethodId,
                cancellationToken);

            payment.Status = chargeResult.Success ? PaymentStatus.Successful : PaymentStatus.Failed;
            payment.ProviderPaymentId = chargeResult.TransactionId;

            // 5. Save payment and idempotency key
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            await _dbContext.AddAsync(payment, cancellationToken);
            
            await _dbContext.AddAsync(new IdempotencyKey
            {
                Key = command.IdempotencyKey,
                AppId = jwtResult.AppId!,
                UserId = command.UserId,
                Operation = "CreatePayment",
                LinkedEntityId = payment.Id.ToString(),
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            // 6. Create outbox message for payment processed event
            if (chargeResult.Success)
            {
                var @event = new PaymentProcessedEvent(payment.Id, payment.AppId, payment.Amount);
                await _dbContext.AddAsync(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = typeof(PaymentProcessedEvent).Name,
                    Content = System.Text.Json.JsonSerializer.Serialize(@event),
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return chargeResult.Success
                ? CreatePaymentResult.Succeeded(payment.Id, chargeResult.TransactionId!)
                : CreatePaymentResult.Failed(chargeResult.Error!);
        }
        catch (Exception ex)
        {
            return CreatePaymentResult.Failed($"Payment processing failed: {ex.Message}");
        }
    }
}
