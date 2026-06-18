using Microsoft.AspNetCore.Mvc;
using NestledNooks.Services;

namespace NestledNooks.Controllers;

[ApiController]
public class PayController : ControllerBase
{
    private readonly IStripePaymentService _stripe;

    public PayController(IStripePaymentService stripe) => _stripe = stripe;

    /// <summary>Legacy email links — send guests through the review/acceptance page first.</summary>
    [HttpGet("/pay/{token}")]
    public IActionResult Pay(string token) =>
        Redirect($"/pay/review/{token}");

    [HttpGet("/pay/{token}/checkout")]
    public async Task<IActionResult> Checkout(string token, CancellationToken cancellationToken)
    {
        var siteBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var checkoutUrl = await _stripe.GetCheckoutRedirectUrlAsync(token, siteBaseUrl, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(checkoutUrl))
            return NotFound("This payment link is invalid or has already been used.");

        return Redirect(checkoutUrl);
    }
}
