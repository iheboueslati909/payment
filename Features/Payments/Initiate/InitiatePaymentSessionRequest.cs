namespace Features.Payments.InitiateSession
{
    public class InitiatePaymentSessionRequest
    {
        public string IntendId { get; set; }
        public string UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string PaymentMethod { get; set; }
        public string IdempotencyKey { get; set; }
    }
}