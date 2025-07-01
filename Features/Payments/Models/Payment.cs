using PaymentGateway.Common.Enums;

namespace PaymentGateway.Features.Payments.Models;

public class Payment
{
    public Guid Id { get; set; }
    public string IntendId { get; set; }
    public string UserId { get; set; }
    public string AppId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public PaymentStatus Status { get; set; }
    public string IdempotencyKey { get; set; }
    public string CheckoutUrl { get; set; }
    public DateTime CreatedAt { get; set; }

}