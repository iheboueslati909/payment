namespace PaymentGateway.Features.Idempotency;

public class IdempotencyKey
{
    public string Key { get; set; } = null!;
    public string AppId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string Operation { get; set; } = null!;
    public string? LinkedEntityId { get; set; }
    public DateTime CreatedAt { get; set; }
}
