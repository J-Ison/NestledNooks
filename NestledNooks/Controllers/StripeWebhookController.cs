using Microsoft.AspNetCore.Mvc;
using NestledNooks.Services;

namespace NestledNooks.Controllers;

[ApiController]
[Route("api/webhooks/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly IStripePaymentService _stripe;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(IStripePaymentService stripe, ILogger<StripeWebhookController> logger)
    {
        _stripe = stripe;
        _logger = logger;
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Post(CancellationToken cancellationToken)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(cancellationToken)
            .ConfigureAwait(false);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        if (string.IsNullOrWhiteSpace(signature))
            return BadRequest();

        try
        {
            await _stripe.HandleWebhookAsync(json, signature, cancellationToken).ConfigureAwait(false);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stripe webhook processing failed.");
            return BadRequest();
        }
    }
}
