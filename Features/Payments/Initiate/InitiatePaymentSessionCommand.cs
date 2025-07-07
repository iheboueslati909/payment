using PaymentGateway.Common.Interfaces;

namespace Features.Payments.InitiateSession
{
    public class InitiatePaymentSessionCommand : ICommand<InitiatePaymentSessionResult>
    {
        public string IntendId { get; set; }
        public string UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string PaymentMethod { get; set; } // e.g., "stripe"
        public string IdempotencyKey { get; set; }
        public PaymentProvider Provider { get; set; } = PaymentProvider.Stripe; // Default to Stripe
    }
}