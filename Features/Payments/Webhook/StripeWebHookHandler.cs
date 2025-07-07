using PaymentGateway.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Infrastructure.Database;
using PaymentGateway.Infrastructure.Messaging.Contracts;
using PaymentGateway.Common.Enums;
using Stripe;
using Stripe.Checkout;
using System.Text;
using PaymentGateway.Features.Payments.Webhook;
using PaymentGateway.Infrastructure.Outbox;
using System.Text.Json;

namespace PaymentGateway.Features.Payments
{
    public class StripeWebhookHandler : ICommandHandler<StripeWebHookCommand, StripeWebhookCommandResult>
    {
        private readonly AppDbContext _db;
        private readonly IStripeSignatureVerifier _verifier;
        private readonly ILogger<StripeWebhookHandler> _logger;

        public StripeWebhookHandler(
            AppDbContext db, //TODO use a repository pattern instead of direct DbContext
            IStripeSignatureVerifier verifier,
            ILogger<StripeWebhookHandler> logger)
        {
            _db = db;
            _verifier = verifier;
            _logger = logger;
        }

        public async Task<StripeWebhookCommandResult> Handle(StripeWebHookCommand command, CancellationToken cancellationToken)
        {
            var request = command.HttpRequest;
            request.EnableBuffering();
            string json;
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
            {
                json = await reader.ReadToEndAsync();
                request.Body.Position = 0;
            }
            var signature = request.Headers["Stripe-Signature"];
            // Use the verifier to get the endpoint secret for this app (multi-tenant)
            var endpointSecret = _verifier.GetEndpointSecret(request); // Implement this method as needed

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, signature, endpointSecret);
            }
            catch (StripeException ex)
            {
                _logger.LogWarning("Stripe signature verification failed: {Message}", ex.Message);
                return new StripeWebhookCommandResult { PaymentStatus = PaymentStatus.Failed.ToString(), EventType = "SignatureVerificationFailed" };
            }

            // Handle event types
            switch (stripeEvent.Type)
            {
                case EventTypes.CheckoutSessionCompleted:
                case EventTypes.PaymentIntentSucceeded:
                    {
                        PaymentIntent paymentIntent = null;
                        Session session = null;
                        string ticketIntentId = null;
                        string appId = null;
                        string paymentId = null;
                        string userId = null;
                        decimal amount = 0;
                        if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
                        {
                            paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                            ticketIntentId = paymentIntent?.Metadata?["TicketIntentId"] ?? paymentIntent?.Metadata?["IntendId"];
                            appId = paymentIntent?.Metadata?["AppId"];
                        }
                        else if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
                        {
                            session = stripeEvent.Data.Object as Session;
                            ticketIntentId = session?.Metadata?["TicketIntentId"] ?? session?.Metadata?["IntendId"];
                            appId = session?.Metadata?["AppId"];
                        }

                        var payment = await _db.Payments.FirstOrDefaultAsync(
                            p => p.IntendId == ticketIntentId && p.AppId == appId, cancellationToken);

                        if (payment != null)
                        {
                            payment.Status = PaymentStatus.Successful;

                            var paymentEvent = new PaymentProcessedEvent(
                                payment.Id,
                                payment.IntendId,
                                payment.AppId,
                                payment.Amount,
                                payment.UserId,
                                DateTime.UtcNow,
                                "Succeeded"
                            );

                            var outboxMessage = new OutboxMessage
                            {
                                Id = Guid.NewGuid(),
                                Type = nameof(PaymentProcessedEvent),
                                Content = JsonSerializer.Serialize(paymentEvent),
                                CreatedAt = DateTime.UtcNow
                            };

                            _db.Set<OutboxMessage>().Add(outboxMessage);

                            await _db.SaveChangesAsync(cancellationToken); // Save payment + outbox atomically
                        }

                        return new StripeWebhookCommandResult
                        {
                            PaymentId = paymentId,
                            EventType = stripeEvent.Type,
                            PaymentIntentId = paymentIntent?.Id,
                            CheckoutSessionId = session?.Id,
                            PaymentStatus = PaymentStatus.Successful.ToString(),
                            CustomerId = paymentIntent?.CustomerId ?? session?.CustomerId,
                            PaymentMethodId = paymentIntent?.PaymentMethodId,
                        };
                    }
                case EventTypes.PaymentIntentPaymentFailed:
                    {
                        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                        var ticketIntentId = paymentIntent?.Metadata?["TicketIntentId"] ?? paymentIntent?.Metadata?["IntendId"];
                        var appId = paymentIntent?.Metadata?["AppId"];
                        var payment = await _db.Payments.FirstOrDefaultAsync(
                            p => p.IntendId == ticketIntentId && p.AppId == appId, cancellationToken);

                        if (payment != null)
                        {
                            payment.Status = PaymentStatus.Failed;

                            var paymentEvent = new PaymentProcessedEvent(
                                payment.Id,
                                payment.IntendId,
                                payment.AppId,
                                payment.Amount,
                                payment.UserId,
                                DateTime.UtcNow,
                                "Failed"
                            );

                            var outboxMessage = new OutboxMessage
                            {
                                Id = Guid.NewGuid(),
                                Type = nameof(PaymentProcessedEvent),
                                Content = JsonSerializer.Serialize(paymentEvent),
                                CreatedAt = DateTime.UtcNow
                            };

                            _db.Set<OutboxMessage>().Add(outboxMessage);

                            await _db.SaveChangesAsync(cancellationToken); // Save payment + outbox atomically
                        }

                        return new StripeWebhookCommandResult
                        {
                            PaymentId = payment?.Id.ToString(),
                            EventType = stripeEvent.Type,
                            PaymentIntentId = paymentIntent?.Id,
                            PaymentStatus = PaymentStatus.Failed.ToString(),
                            CustomerId = paymentIntent?.CustomerId,
                            PaymentMethodId = paymentIntent?.PaymentMethodId,
                        };
                    }
                default:
                    _logger.LogInformation("Unhandled Stripe event type: {Type}", stripeEvent.Type);
                    return new StripeWebhookCommandResult { EventType = stripeEvent.Type };
            }
        }
    }
}