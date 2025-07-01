using Stripe;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stripe.Checkout;

namespace PaymentGateway.Infrastructure.PaymentProviders.Stripe;

public class StripePaymentProvider : IPaymentProvider
{
    private readonly string _apiKey;

    public StripePaymentProvider(IOptions<StripeOptions> options)
    {
        _apiKey = options.Value.ApiKey;
        StripeConfiguration.ApiKey = _apiKey;
    }

    public async Task<CreateCheckoutSessionResult> CreateCheckoutSessionAsync(CreateCheckoutSessionRequest request)
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            Mode = "payment",
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.Currency,
                        UnitAmount = (long)(request.Amount * 100), // Stripe expects amount in cents
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Payment"
                        }
                    },
                    Quantity = 1
                }
            },
            Metadata = new Dictionary<string, string>
            {
                { "IntendId", request.IntendId },
                { "UserId", request.UserId },
                { "AppId", request.AppId },
                { "IdempotencyKey", request.IdempotencyKey }
            },
            SuccessUrl = "https://yourdomain.com/payment/success?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = "https://yourdomain.com/payment/cancel"
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return new CreateCheckoutSessionResult
        {
            CheckoutUrl = session.Url,
            SessionId = session.Id
        };
    }
}
