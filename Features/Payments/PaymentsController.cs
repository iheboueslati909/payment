using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Common.Interfaces;
using PaymentGateway.Features.Payments.Create;

namespace PaymentGateway.Features.Payments;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly ICommandDispatcher _dispatcher;

    public PaymentsController(ICommandDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePayment(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return Unauthorized("Missing authorization header");
        }

        var command = new CreatePaymentCommand(
            request.Amount,
            request.Currency,
            request.PaymentMethodId,
            request.UserId,
            request.AppId,
            request.IdempotencyKey);

        var result = await _dispatcher.Send(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new
        {
            paymentId = result.PaymentId,
            transactionId = result.TransactionId
        });
    }
}
