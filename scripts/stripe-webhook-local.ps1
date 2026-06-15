# Forward Stripe webhooks to your local NestledNooks app.
# Keep this window open while testing payments.
#
# Prerequisites:
#   - Stripe CLI (winget install Stripe.StripeCli)
#   - Stripe keys in user secrets (Stripe:SecretKey)
#   - NestledNooks running (https profile: https://localhost:7225)

$ErrorActionPreference = "Stop"

$ProjectDir = Split-Path -Parent $PSScriptRoot | Join-Path -ChildPath "NestledNooks"
$ForwardUrl = "https://localhost:7225/api/webhooks/stripe"

function Get-StripeSecretKey {
    Push-Location $ProjectDir
    try {
        $line = dotnet user-secrets list 2>$null | Where-Object { $_ -match "^Stripe:SecretKey\s*=" } | Select-Object -First 1
        if (-not $line) {
            throw "Stripe:SecretKey not found in user secrets. Run Payment settings setup first."
        }
        return ($line -split "=", 2)[1].Trim()
    }
    finally {
        Pop-Location
    }
}

$stripe = Get-Command stripe -ErrorAction SilentlyContinue
if (-not $stripe) {
    throw "Stripe CLI not found. Install with: winget install Stripe.StripeCli"
}

$apiKey = Get-StripeSecretKey

Write-Host ""
Write-Host "NestledNooks Stripe webhook forwarder" -ForegroundColor Cyan
Write-Host "  Forwarding to: $ForwardUrl"
Write-Host "  Events:        checkout.session.completed"
Write-Host ""
Write-Host "Updating Stripe:WebhookSecret in user secrets..." -ForegroundColor Yellow

Push-Location $ProjectDir
$whsec = stripe listen --print-secret --api-key $apiKey 2>&1 | Select-Object -Last 1
if ($whsec -notmatch "^whsec_") {
    throw "Could not get webhook signing secret. Try: stripe login"
}
dotnet user-secrets set "Stripe:WebhookSecret" $whsec | Out-Null
Pop-Location

Write-Host "  Webhook secret saved." -ForegroundColor Green
Write-Host ""
Write-Host "Restart NestledNooks if it is already running, then leave this window open." -ForegroundColor Yellow
Write-Host "Press Ctrl+C to stop forwarding." -ForegroundColor DarkGray
Write-Host ""

stripe listen `
    --api-key $apiKey `
    --events checkout.session.completed `
    --forward-to $ForwardUrl
