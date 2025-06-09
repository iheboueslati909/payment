using Stripe;
using Microsoft.Extensions.Options;

namespace PaymentGateway.Infrastructure.PaymentProviders.Stripe;

public class StripePaymentProvider : IPaymentProvider
{
    private readonly string _apiKey;

    public StripePaymentProvider(IOptions<StripeOptions> options)
    {
        _apiKey = options.Value.ApiKey;
        StripeConfiguration.ApiKey = _apiKey;
    }

    public async Task<PaymentProviderResult> ChargeAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        string userId,
        string paymentMethodId,
        CancellationToken cancellationToken)
    {
        try
        {
            var service = new PaymentIntentService();
            var createOptions = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100),
                Currency = currency.ToLower(),
                PaymentMethod = paymentMethodId,
                Metadata = new Dictionary<string, string>
                {
                    ["PaymentId"] = paymentId.ToString(),
                    ["UserId"] = userId
                },
                Confirm = true, // Immediately confirm the payment
                PaymentMethodTypes = new List<string> { "card" },
                CaptureMethod = "automatic"
            };

            var intent = await service.CreateAsync(createOptions, cancellationToken: cancellationToken);

            return intent.Status == "succeeded"
                ? PaymentProviderResult.Succeeded(intent.Id)
                : PaymentProviderResult.Failed($"Payment failed: {intent.LastPaymentError?.Message ?? "Unknown error"}");
        }
        catch (StripeException ex)
        {
            return PaymentProviderResult.Failed($"Stripe error: {ex.Message}");
        }
    }
}

public class StripeOptions
{
    public string ApiKey { get; set; } = string.Empty;
}
