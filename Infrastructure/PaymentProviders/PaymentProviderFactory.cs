using PaymentGateway.Infrastructure.PaymentProviders;
using PaymentGateway.Infrastructure.PaymentProviders.Stripe;

public class PaymentProviderFactory : IPaymentProviderFactory
{
    private readonly IServiceProvider _sp;

    public PaymentProviderFactory(IServiceProvider sp)
    {
        _sp = sp;
    }

    public IPaymentProvider Resolve(PaymentProvider provider)
    {
        return provider switch
        {
            PaymentProvider.Stripe => _sp.GetRequiredService<StripePaymentProvider>(),
            // PaymentProvider.PayPal => _sp.GetRequiredService<PayPalPaymentProvider>(),
            _ => throw new NotSupportedException($"Provider {provider} is not supported")
        };
    }
}
