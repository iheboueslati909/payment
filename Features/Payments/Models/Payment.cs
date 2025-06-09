using PaymentGateway.Common.Enums;

namespace PaymentGateway.Features.Payments.Models;

public class Payment
{
    public Guid Id { get; set; }
    public string AppId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string? ProviderPaymentId { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
