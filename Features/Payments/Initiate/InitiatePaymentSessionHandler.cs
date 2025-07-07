using Microsoft.EntityFrameworkCore;
using PaymentGateway.Common.Interfaces;
using PaymentGateway.Features.Payments.Models;
using PaymentGateway.Infrastructure.Database;
using PaymentGateway.Infrastructure.PaymentProviders;
using PaymentGateway.Common.Enums;

namespace Features.Payments.InitiateSession
{
    public class InitiatePaymentSessionHandler : ICommandHandler<InitiatePaymentSessionCommand, InitiatePaymentSessionResult>
    {
        // private readonly IPaymentProvider _paymentProvider;
        private readonly IPaymentProviderFactory _paymentProviderFactory;

        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;

        public InitiatePaymentSessionHandler(IPaymentProviderFactory paymentProviderFactory, AppDbContext db, IConfiguration configuration)
        {
            // _paymentProvider = paymentProvider;
                _paymentProviderFactory = paymentProviderFactory;

            _db = db;
            _configuration = configuration;
        }

        public async Task<InitiatePaymentSessionResult> Handle(InitiatePaymentSessionCommand cmd, CancellationToken ct)
        {
            // Extract AppId from configuration
            var appId = _configuration["App:Id"];
            // Check idempotency
            var existing = await _db.Payments
                .FirstOrDefaultAsync(p => p.IdempotencyKey == cmd.IdempotencyKey && p.AppId == appId, ct);
            if (existing != null)
            {
                return new InitiatePaymentSessionResult { CheckoutUrl = existing.CheckoutUrl, PaymentId = existing.Id.ToString() };
            }

            var provider = _paymentProviderFactory.Resolve(cmd.Provider);

            var session = await provider.CreateCheckoutSessionAsync(new CreateCheckoutSessionRequest
            {
                Amount = cmd.Amount,
                Currency = cmd.Currency,
                IntendId = cmd.IntendId,
                UserId = cmd.UserId,
                AppId = appId,
                IdempotencyKey = cmd.IdempotencyKey
            });

            var payment = new PaymentGateway.Features.Payments.Models.Payment
            {
                Id = Guid.NewGuid(),
                IntendId = cmd.IntendId,
                UserId = cmd.UserId,
                AppId = appId,
                Amount = cmd.Amount,
                Currency = cmd.Currency,
                Status = PaymentStatus.Pending,
                IdempotencyKey = cmd.IdempotencyKey,
                CheckoutUrl = session.CheckoutUrl,
                CreatedAt = DateTime.UtcNow,
                Provider = cmd.Provider
            };

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync(ct);

            return new InitiatePaymentSessionResult
            {
                CheckoutUrl = session.CheckoutUrl,
                PaymentId = payment.Id.ToString()
            };
        }
    }
}