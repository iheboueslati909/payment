namespace PaymentGateway.Infrastructure.Messaging.Contracts;

public record PaymentProcessedEvent(
    Guid PaymentId,
    string IntentId,
    string AppId,
    decimal Amount,
    string UserId,
    DateTime ProcessedAt = default,
    string Status = "Succeeded")
{
    public DateTime ProcessedAt { get; init; } = ProcessedAt == default ? DateTime.UtcNow : ProcessedAt;
}
