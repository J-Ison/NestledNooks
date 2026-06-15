# Production deploy checklist (NestledNooks)
# Run after deploy; fix any failures before opening public booking.

Write-Host "NestledNooks production deploy checklist" -ForegroundColor Cyan
Write-Host ""

$checks = @(
    @{
        Name = "Azure SQL connection string (not LocalDB)"
        Hint = "Portal -> NestledNooks -> Configuration -> ConnectionStrings__DefaultConnection"
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
