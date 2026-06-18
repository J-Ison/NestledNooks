using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NestledNooks.Models;

public sealed class LegalAcceptanceRecord
{
    public const string PhaseBooking = "booking";
    public const string PhasePayment = "payment";

    public string Phase { get; set; } = PhaseBooking;

    public int LegalDocumentsVersion { get; set; }

    public DateTime AcceptedAtUtc { get; set; }

    public string? ClientIp { get; set; }

    public string RentalAgreementHash { get; set; } = "";

    public string HouseRulesHash { get; set; } = "";

    public string LiabilityAcknowledgmentHash { get; set; } = "";

    public static LegalAcceptanceRecord Create(
        string phase,
        PropertyLegalSnapshot documents,
        string? clientIp,
        DateTime? acceptedAtUtc = null) =>
        new()
        {
            Phase = phase,
            LegalDocumentsVersion = documents.LegalDocumentsVersion,
            AcceptedAtUtc = acceptedAtUtc ?? DateTime.UtcNow,
            ClientIp = clientIp,
            RentalAgreementHash = Hash(documents.RentalAgreementText),
            HouseRulesHash = Hash(documents.HouseRulesText),
            LiabilityAcknowledgmentHash = Hash(documents.LiabilityAcknowledgmentText),
        };

    public static string Hash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text.Trim()));
        return Convert.ToHexString(bytes)[..16];
    }

    public static string Serialize(LegalAcceptanceRecord record) =>
        JsonSerializer.Serialize(record, SerializerOptions);

    public static LegalAcceptanceRecord? TryDeserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<LegalAcceptanceRecord>(json, SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public string FormatSummary()
    {
        var local = AcceptedAtUtc.ToString("MMM d, yyyy h:mm tt") + " UTC";
        return $"Accepted {Phase} · v{LegalDocumentsVersion} · {local}" +
               (string.IsNullOrWhiteSpace(ClientIp) ? "" : $" · IP {ClientIp}");
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
