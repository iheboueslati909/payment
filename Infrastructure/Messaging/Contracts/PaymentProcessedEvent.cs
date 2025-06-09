namespace PaymentGateway.Infrastructure.Messaging.Contracts;

public record PaymentProcessedEvent(
    Guid PaymentId,
    string AppId,
    decimal Amount,
    DateTime ProcessedAt = default)
{
    public DateTime ProcessedAt { get; init; } = ProcessedAt == default ? DateTime.UtcNow : ProcessedAt;
}
