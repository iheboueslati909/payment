namespace PaymentGateway.Infrastructure.PaymentProviders;

public interface IPaymentProvider
{
        Task<CreateCheckoutSessionResult> CreateCheckoutSessionAsync(CreateCheckoutSessionRequest request);
}