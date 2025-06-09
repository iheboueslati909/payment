namespace PaymentGateway.Infrastructure.PaymentProviders;

public interface IPaymentProvider
{
    Task<PaymentProviderResult> ChargeAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        string userId,
        string paymentMethodId,
        CancellationToken cancellationToken);
}

public record PaymentProviderResult
{
    public bool Success { get; init; }
    public string? TransactionId { get; init; }
    public string? Error { get; init; }

    public static PaymentProviderResult Succeeded(string transactionId) =>
        new() { Success = true, TransactionId = transactionId };

    public static PaymentProviderResult Failed(string error) =>
        new() { Success = false, Error = error };
}
