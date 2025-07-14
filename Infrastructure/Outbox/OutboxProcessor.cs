using MassTransit;
using Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Infrastructure.Database;
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
                        Console.WriteLine($"----------------Published PaymentProcessedEvent: {paymentEvent}");
                        // Log the event
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
