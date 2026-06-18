using NestledNooks.Models;

namespace NestledNooks.Tests.Services;

public sealed class LegalAcceptanceRecordTests
{
    [Fact]
    public void Hash_IsStableForSameText()
    {
        var a = LegalAcceptanceRecord.Hash("Same document text.");
        var b = LegalAcceptanceRecord.Hash("Same document text.");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Serialize_RoundTrips()
    {
        var record = new LegalAcceptanceRecord
        {
            Phase = LegalAcceptanceRecord.PhaseBooking,
            LegalDocumentsVersion = 2,
            AcceptedAtUtc = new DateTime(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc),
            ClientIp = "203.0.113.10",
            RentalAgreementHash = "ABC",
            HouseRulesHash = "DEF",
            LiabilityAcknowledgmentHash = "GHI",
        };

        var json = LegalAcceptanceRecord.Serialize(record);
        var parsed = LegalAcceptanceRecord.TryDeserialize(json);

        Assert.NotNull(parsed);
        Assert.Equal(record.Phase, parsed.Phase);
        Assert.Equal(record.LegalDocumentsVersion, parsed.LegalDocumentsVersion);
        Assert.Equal(record.ClientIp, parsed.ClientIp);
    }
}
