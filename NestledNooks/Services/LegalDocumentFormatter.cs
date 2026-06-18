using System.Text.RegularExpressions;

namespace NestledNooks.Services;

public enum LegalBlockKind
{
    DraftNotice,
    Title,
    Subheading,
    SectionHeading,
    Paragraph,
    Bullet,
}

public sealed class LegalDocumentBlock
{
    public LegalBlockKind Kind { get; init; }

    public string Text { get; init; } = "";
}

public static partial class LegalDocumentFormatter
{
    public static IReadOnlyList<LegalDocumentBlock> Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var blocks = new List<LegalDocumentBlock>();
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0)
                continue;

            if (line.StartsWith("DRAFT —", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("DRAFT -", StringComparison.OrdinalIgnoreCase))
            {
                blocks.Add(new LegalDocumentBlock { Kind = LegalBlockKind.DraftNotice, Text = line });
                continue;
            }

            if (line.StartsWith('•') || line.StartsWith("- ", StringComparison.Ordinal))
            {
                var bullet = line.TrimStart('•', '-', ' ');
                blocks.Add(new LegalDocumentBlock { Kind = LegalBlockKind.Bullet, Text = bullet });
                continue;
            }

            if (SectionHeading().IsMatch(line))
            {
                blocks.Add(new LegalDocumentBlock { Kind = LegalBlockKind.SectionHeading, Text = line });
                continue;
            }

            if (IsDocumentTitle(line))
            {
                blocks.Add(new LegalDocumentBlock { Kind = LegalBlockKind.Title, Text = line });
                continue;
            }

            if (IsSubheading(line))
            {
                blocks.Add(new LegalDocumentBlock { Kind = LegalBlockKind.Subheading, Text = line });
                continue;
            }

            blocks.Add(new LegalDocumentBlock { Kind = LegalBlockKind.Paragraph, Text = line });
        }

        return blocks;
    }

    private static bool IsAllCapsTitle(string line)
    {
        if (line.Length < 8)
            return false;

        var letters = line.Where(char.IsLetter).ToList();
        if (letters.Count < 6)
            return false;

        return letters.All(char.IsUpper);
    }

    private static bool IsDocumentTitle(string line)
    {
        if (IsAllCapsTitle(line))
            return true;

        return line.StartsWith("HOUSE RULES", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("LIABILITY & RISK", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("SHORT-TERM RENTAL", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSubheading(string line)
    {
        if (line.StartsWith("Property:", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("Host:", StringComparison.OrdinalIgnoreCase))
            return false;

        if (line.Length > 45 || line.EndsWith('.'))
            return false;

        if (line.Contains("http", StringComparison.OrdinalIgnoreCase))
            return false;

        return !IsAllCapsTitle(line);
    }

    [GeneratedRegex(@"^\d+\.\s+\S")]
    private static partial Regex SectionHeading();
}
