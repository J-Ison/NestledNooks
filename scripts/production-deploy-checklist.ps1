# Production deploy checklist (NestledNooks)
# Run after deploy; fix any failures before opening public booking.

Write-Host "NestledNooks production deploy checklist" -ForegroundColor Cyan
Write-Host ""

$checks = @(
    @{
        Name = "Calendar + PriceLabs sync intervals (Azure app settings)"
        Hint = "Booking__CalendarSyncIntervalMinutes=180 (3h iCal). PriceLabs__SyncIntervalMinutes=360 (6h). Avoid values under 60 unless debugging."
    },
    @{
        Name = "Guest-facing SQL cache (optional tuning)"
        Hint = "GuestFacingCache__PropertyMinutes=15, UnavailableDatesMinutes=15 — reduces repeat homepage/booking DB reads"
    },
    @{
        Name = "Stripe live/test keys + WebhookSecret"
        Hint = "App settings or Key Vault: Stripe__PublishableKey, Stripe__SecretKey, Stripe__WebhookSecret, Stripe__Enabled=true"
    },
    @{
        Name = "Stripe webhook endpoint"
        Hint = "https://nestlednooks-bvd3htchb9hwhzex.centralus-01.azurewebsites.net/api/webhooks/stripe"
    },
    @{
        Name = "SMTP password (Smtp__Password)"
        Hint = "Gmail app password in Azure app settings / user secrets locally"
    },
    @{
        Name = "Direct bookings OFF for setup (Manage site toggle)"
        Hint = "/account/manage/site -> uncheck 'Accept booking requests from the public'"
    },
    @{
        Name = "Airbnb/Vrbo iCal import URLs"
        Hint = "Booking:Properties:0:AirbnbIcalUrl and VrboIcalUrl in app settings"
    },
    @{
        Name = "Export holds.ics URL into Airbnb"
        Hint = "https://YOUR-SITE/api/calendar/deerfield-retreat/holds.ics"
    },
    @{
        Name = "Guest legal documents reviewed (attorney)"
        Hint = "Manage Property -> Guest legal documents — replace DRAFT text before public booking"
    },
    @{
        Name = "SD lodging license + Rapid City vacation home registration"
        Hint = "doh.sd.gov lodging license; rcgov.org vacation home registration (2026+)"
    },
    @{
        Name = "Short-term rental insurance (not homeowners only)"
        Hint = "Covers guest injury claims — required for Airbnb and direct bookings alike"
    },
    @{
        Name = "Airbnb/Vrbo house rules in listing"
        Hint = "Platform terms apply to channel guests; mirror house rules on each listing"
    },
    @{
        Name = "Owner account can sign in on live"
        Hint = "Admin:OwnerEmails includes your email; Owner role assigned on first login"
    }
)

$i = 1
foreach ($c in $checks) {
    Write-Host ("[{0}] {1}" -f $i, $c.Name)
    Write-Host ("     {0}" -f $c.Hint) -ForegroundColor DarkGray
    $i++
}

Write-Host ""
Write-Host "After deploy, check Azure Log stream for:" -ForegroundColor Yellow
Write-Host "  - 'Database migrations: X applied, Y pending'"
Write-Host "  - 'All critical schema checks passed.'"
Write-Host ""
Write-Host "If schema checks fail, restart the app once (schema repair runs on startup)."
Write-Host "Do not enable public booking until Stripe test payment + email work end-to-end."
