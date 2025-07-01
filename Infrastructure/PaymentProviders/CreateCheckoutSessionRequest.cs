public class CreateCheckoutSessionRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string IntendId { get; set; }
    public string UserId { get; set; }
    public string AppId { get; set; }
    public string IdempotencyKey { get; set; }
}