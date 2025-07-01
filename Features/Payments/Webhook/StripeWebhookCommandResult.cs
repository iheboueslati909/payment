
namespace PaymentGateway.Features.Payments.Webhook;

public class StripeWebhookCommandResult
{
    public string PaymentId { get; set; }
    public string SessionId { get; set; }
    public string CustomerId { get; set; }
    public string EventType { get; set; }
    public string PaymentIntentId { get; set; }
    public string CheckoutSessionId { get; set; }
    public string PaymentMethodId { get; set; }
    public string PaymentStatus { get; set; } // Use the PaymentStatus enum for this property
}