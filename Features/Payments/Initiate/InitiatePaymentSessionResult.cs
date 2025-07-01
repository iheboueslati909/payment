namespace Features.Payments.InitiateSession
{
    public class InitiatePaymentSessionResult
    {
        public string CheckoutUrl { get; set; }
        public string PaymentId { get; set; }
    }
}