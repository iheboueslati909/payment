namespace PaymentGateway.Features.Payments.Create;

public record CreatePaymentRequest(
    decimal Amount,
    string Currency,
    string PaymentMethodId,
    string UserId,
    string IdempotencyKey);
