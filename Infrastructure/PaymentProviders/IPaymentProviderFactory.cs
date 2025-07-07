using PaymentGateway.Infrastructure.PaymentProviders;

public interface IPaymentProviderFactory
{
    IPaymentProvider Resolve(PaymentProvider provider);
}
