using Features.Payments.InitiateSession;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Common.Interfaces;

namespace PaymentGateway.Features.Payments;

[ApiController]
[Route("api/payments/stripe")]
public class StripeWebhooksController : ControllerBase
{
    private readonly ICommandDispatcher _dispatcher;

    public StripeWebhooksController(ICommandDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost]
    [Route("webhook")]
    public async Task<IActionResult> HandleHook()
    {
        var result = await _dispatcher.Send(new StripeWebHookCommand { HttpRequest = Request });
        return Ok(result);
    }

    [HttpPost]
    [Route("initiate-payment")]
    public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentSessionCommand command)
    {
        var result = await _dispatcher.Send(command);
        return Ok(result);
    }
}