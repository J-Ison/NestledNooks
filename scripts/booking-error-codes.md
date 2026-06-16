# Booking request error codes (guest-facing)

When a booking request fails, the guest sees a message plus **Ref: BK-xxx**. Use the code when troubleshooting.

| Code | Guest sees (typical) | What it means for you |
|------|----------------------|------------------------|
| **BK-001** | Direct booking is temporarily unavailable… | `DirectBookingsEnabled` is off and submitter is not Owner. |
| **BK-002** | Unknown property. | Property slug not in `Booking:Properties` config. |
| **BK-003** | Check-in and check-out are required. | Dates missing on submit. |
| **BK-004** | Maximum N guests. | Over `MaxGuests`. |
| **BK-005** | Maximum N pets. | Over `MaxPets`. |
| **BK-006** | Minimum stay is N nights. / pricing message | Quote/pricing failed (min stay, DB rates table, etc.). |
| **BK-007** | Those dates are no longer available… | Overlap with calendar hold or external block. |
| **BK-008** | Could not save booking… | SQL error saving `BookingRequests` (migration/schema). |
| **BK-009** | Could not submit… | Unexpected server error — check Log stream. |

Success redirects to `/booking/confirmation?ref=…` — no error code.
