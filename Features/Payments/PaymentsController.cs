using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Common.Interfaces;

namespace PaymentGateway.Features.Payments;

[ApiController]
[Route("api/payments/webhooks/stripe")]
public class StripeWebhooksController : ControllerBase
{
    private readonly ICommandDispatcher _dispatcher;

    public StripeWebhooksController(ICommandDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost]
    public async Task<IActionResult> Handle()
    {
        var result = await _dispatcher.Send(new StripeWebHookCommand { HttpRequest = Request });
        return Ok(result);
    }
}