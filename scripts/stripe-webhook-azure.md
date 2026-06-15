# Azure production webhook setup (run once after deploy)
#
# 1. Open Stripe Dashboard (test or live mode):
#    https://dashboard.stripe.com/test/webhooks
#
# 2. Add endpoint:
#    URL: https://nestlednooks-bvd3htchb9hwhzex.centralus-01.azurewebsites.net/api/webhooks/stripe
#    Events: checkout.session.completed
#
# 3. Copy the signing secret (whsec_...) and set in Azure App Service:
#    Configuration → Application settings → New application setting
#    Name:  Stripe__WebhookSecret
#    Value: whsec_...
#
# Also ensure these exist in Azure (same Configuration blade):
#    Stripe__Enabled = true
#    Stripe__PublishableKey = pk_test_... or pk_live_...
#    Stripe__SecretKey = sk_test_... or sk_live_...
#
# 4. Save and restart the app.

Write-Host @"

Azure Stripe webhook checklist
==============================

Endpoint URL:
  https://nestlednooks-bvd3htchb9hwhzex.centralus-01.azurewebsites.net/api/webhooks/stripe

Stripe event to subscribe:
  checkout.session.completed

Azure App Setting:
  Stripe__WebhookSecret = whsec_... (from Stripe Dashboard → Webhooks → your endpoint → Signing secret)

Dashboard link (test mode):
  https://dashboard.stripe.com/test/webhooks

"@
