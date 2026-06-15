using Microsoft.AspNetCore.Mvc;
using NestledNooks.Services;

namespace NestledNooks.Controllers;

[ApiController]
public class PayController : ControllerBase
{
    private readonly IStripePaymentService _stripe;

    public PayController(IStripePaymentService stripe) => _stripe = stripe;

    [HttpGet("/pay/{token}")]
    public async Task<IActionResult> Pay(string token, CancellationToken cancellationToken)
    {
        var siteBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var checkoutUrl = await _stripe.GetCheckoutRedirectUrlAsync(token, siteBaseUrl, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(checkoutUrl))
            return NotFound("This payment link is invalid or has already been used.");

        return Redirect(checkoutUrl);
    }
}
