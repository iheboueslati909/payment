
using PaymentGateway.Common.Interfaces;
using PaymentGateway.Features.Payments.Webhook;

public class StripeWebHookCommand : ICommand<StripeWebhookCommandResult>
{
    public HttpRequest HttpRequest { get; set; }
}