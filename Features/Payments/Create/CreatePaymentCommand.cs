using PaymentGateway.Common.Interfaces;

namespace PaymentGateway.Features.Payments.Create;

public record CreatePaymentCommand(
    decimal Amount,
    string Currency,
    string PaymentMethodId,
    string UserId,
    string AppId,
    string IdempotencyKey) : ICommand<CreatePaymentResult>;

public record CreatePaymentResult
{
    public bool Success { get; init; }
    public Guid? PaymentId { get; init; }
    public string? TransactionId { get; init; }
    public string? Error { get; init; }

    public static CreatePaymentResult Succeeded(Guid paymentId, string transactionId) =>
        new() { Success = true, PaymentId = paymentId, TransactionId = transactionId };

    public static CreatePaymentResult Failed(string error) =>
        new() { Success = false, Error = error };
}
