using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Infrastructure.Database;
using PaymentGateway.Infrastructure.Messaging.Contracts;
using System.Text.Json;

namespace PaymentGateway.Infrastructure.Outbox;

public class OutboxProcessor
{
    private readonly AppDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public OutboxProcessor(AppDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var messages = await _dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                // Deserialize and publish based on message type
                switch (message.Type)
                {
                    case nameof(PaymentProcessedEvent):
                        var paymentEvent = JsonSerializer.Deserialize<PaymentProcessedEvent>(message.Content) ?? throw new InvalidOperationException("Deserialized PaymentProcessedEvent is null.");
                        await _publishEndpoint.Publish(paymentEvent, cancellationToken);
                        // Log the event or perform additional actions if needed
                        Console.WriteLine($"Published PaymentProcessedEvent: {paymentEvent.PaymentId}");
                        break;
                    // Add other event types here
                }

                message.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                message.Error = ex.Message;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
