namespace NestledNooks.Services;

/// <summary>
/// Starter legal copy for owner editing. NOT legal advice — replace with attorney-reviewed text.
/// </summary>
public static class PropertyLegalDefaults
{
    public const string DraftNotice =
        "DRAFT — FOR REVIEW BY A SOUTH DAKOTA ATTORNEY BEFORE RELYING ON THIS TEXT.";

    public static string RentalAgreement(string propertyName) =>
        $"""
        {DraftNotice}

        SHORT-TERM RENTAL AGREEMENT

        Property: {propertyName}
        Host: Nestled Nooks (update with your legal entity name when your attorney advises)

        1. Agreement
        By submitting a booking request or paying for a stay, the guest ("Guest") agrees to this Rental Agreement and the House Rules incorporated by reference.

        2. Reservation
        A request is not confirmed until the host approves it in writing (email or site confirmation). Quoted totals may change if dates, guest count, or fees change before approval.

        3. Payment, cancellation, and refunds
        Payment terms are stated at approval. Unless otherwise agreed in writing, cancellation and refund rules are: [EDIT — add your cancellation policy].

        4. Occupancy and use
        Only registered guests may stay overnight. No parties, events, or commercial photography without prior written approval. Guest is responsible for the conduct of all visitors.

        5. Pets
        Pets only if approved in the booking and per House Rules. Unauthorized pets may result in cancellation without refund and additional cleaning fees.

        6. Property care
        Guest must leave the property in substantially the same condition as at check-in, ordinary wear excepted. Guest is responsible for damage caused by Guest or Guest's party.

        7. Governing law
        This agreement is governed by the laws of the State of South Dakota.

        8. Entire agreement
        This agreement, House Rules, and Liability & Risk Acknowledgment together form the entire agreement for direct bookings on this website.
        """;

    public static string HouseRules(string propertyName) =>
        $"""
        {DraftNotice}

        HOUSE RULES — {propertyName}

        Check-in: 4:00 PM · Check-out: 10:00 AM (unless we agree otherwise in writing)

        Safety & property
        • No smoking inside the home.
        • Do not disable smoke or carbon monoxide detectors.
        • Use railings on stairs; watch for uneven ground outdoors.
        • Winter (roughly November–March): ice and snow may be present on walks, steps, and driveways. Wear appropriate footwear. We may salt or plow, but surfaces can still be slippery.
        • Bathrooms: wet floors are slippery. Use bath mats; supervise children. Tub/shower use is at Guest's own risk.
        • Outdoor areas, decks, and grills: use at your own risk. Supervise children at all times.
        • Do not move heavy furniture or tamper with mechanical, electrical, or propane systems.

        Quiet hours: 10:00 PM – 8:00 AM

        Pets (if approved)
        • Pets must be house-trained, under control, and never left unattended in the home.
        • Clean up after pets indoors and outdoors.

        Emergencies
        Call 911 for emergencies. Then contact us using the number in your confirmation email.

        Violations may result in termination of the stay without refund.
        """;

    public static string LiabilityAcknowledgment(string propertyName) =>
        $"""
        {DraftNotice}

        LIABILITY & RISK ACKNOWLEDGMENT — {propertyName}

        Recreational vacation homes involve inherent risks, including but not limited to:
        • Slip, trip, and fall hazards (including ice, snow, wet surfaces, rugs, and stairs)
        • Bathroom and tub/shower injuries
        • Outdoor terrain, wildlife, and weather
        • Fire, grill, and appliance use
        • Remote or rural location and delayed emergency response

        To the fullest extent permitted by South Dakota law, Guest acknowledges these risks and agrees that Guest assumes responsibility for Guest's own safety and that of Guest's party.

        Guest agrees to release and hold harmless the property owner, Nestled Nooks, and their agents from liability for injury, loss, or damage arising from Guest's use of the property, except to the extent caused by gross negligence or willful misconduct (which cannot be waived by law).

        This acknowledgment supplements the Rental Agreement and does not replace the need for appropriate short-term rental insurance.

        Guest confirms they are at least 18 years old and have authority to accept on behalf of all members of their party.
        """;
}
