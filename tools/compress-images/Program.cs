using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

const int maxWidth = 1920;
const int maxHeight = 1280;
const int webpQuality = 82;

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var srcDir = Path.Combine(repoRoot, "NestledNooks", "wwwroot", "images", "originals");
var outDir = Path.Combine(repoRoot, "NestledNooks", "wwwroot", "images");
var manifestPath = Path.Combine(repoRoot, "tools", "compress-images", "photos.generated.txt");

Directory.CreateDirectory(outDir);

var images = Directory
    .EnumerateFiles(srcDir)
    .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
             || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
             || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
    .Select(Path.GetFileName)
    .Where(n => n is not null)
    .Cast<string>()
    .ToList();

if (images.Count == 0)
{
    Console.Error.WriteLine("No images found in originals folder.");
    return 1;
}

var results = new List<PhotoResult>();

foreach (var fileName in images)
{
    var srcPath = Path.Combine(srcDir, fileName);
    var baseName = Path.GetFileNameWithoutExtension(fileName);
    var outName = $"{baseName}.webp";
    var outPath = Path.Combine(outDir, outName);
    var srcBytes = new FileInfo(srcPath).Length;

    using var image = await Image.LoadAsync(srcPath);
    image.Mutate(x => x.AutoOrient());
    image.Mutate(x => x.Resize(new ResizeOptions
    {
        Mode = ResizeMode.Max,
        Size = new Size(maxWidth, maxHeight)
    }));

    await image.SaveAsync(outPath, new WebpEncoder
    {
        Quality = webpQuality,
        Method = WebpEncodingMethod.Level4
    });

    var outBytes = new FileInfo(outPath).Length;
    results.Add(new PhotoResult(baseName, outName, AltFromBaseName(baseName), SortKey(baseName), srcBytes, outBytes));

    Console.WriteLine($"{fileName} -> {outName} ({srcBytes / 1024d / 1024d:F1}MB -> {outBytes / 1024d:F0}KB)");
}

results.Sort((a, b) => string.Compare(a.SortKey, b.SortKey, StringComparison.OrdinalIgnoreCase));

foreach (var hero in new[] { "322EliArialBack04", "322EliFrontArial01", "322EliFront01" })
{
    var idx = results.FindIndex(r => r.BaseName.Equals(hero, StringComparison.OrdinalIgnoreCase));
    if (idx <= 0)
    {
        continue;
    }

    var item = results[idx];
    results.RemoveAt(idx);
    results.Insert(0, item);
    break;
}

var totalIn = results.Sum(r => r.SrcBytes);
var totalOut = results.Sum(r => r.OutBytes);

var sb = new StringBuilder();
sb.AppendLine(CultureInfo.InvariantCulture, $"// {results.Count} photos, {totalIn / 1024d / 1024d:F1}MB -> {totalOut / 1024d / 1024d:F1}MB");
foreach (var r in results)
{
    sb.AppendLine(CultureInfo.InvariantCulture, $"        new(\"/images/{r.OutName}\", \"{r.Alt.Replace("\"", "\\\"")}\"),");
}

await File.WriteAllTextAsync(manifestPath, sb.ToString());

Console.WriteLine();
Console.WriteLine($"Done: {results.Count} images");
Console.WriteLine($"Total: {totalIn / 1024d / 1024d:F1}MB -> {totalOut / 1024d / 1024d:F1}MB");
Console.WriteLine($"Wrote {manifestPath}");
return 0;

static string AltFromBaseName(string baseName)
{
    var alt = baseName;
    alt = Regex.Replace(alt, "^322", "", RegexOptions.IgnoreCase);
    alt = Regex.Replace(alt, "Eli", " ", RegexOptions.IgnoreCase);
    alt = Regex.Replace(alt, "DrLiving", " living room ", RegexOptions.IgnoreCase);
    alt = Regex.Replace(alt, "MasterBed", " master bedroom ", RegexOptions.IgnoreCase);
    alt = Regex.Replace(alt, "MasterBath", " master bathroom ", RegexOptions.IgnoreCase);
    alt = Regex.Replace(alt, "Bedroom", " bedroom ", RegexOptions.IgnoreCase);
    alt = Regex.Replace(alt, "Kitchen", " kitchen ", RegexOptions.IgnoreCase);
    alt = Regex.Replace(alt, "Dining", " dining ", RegexOptions.IgnoreCase);
    alt = Regex.Replace(alt, "Balcony", " balcony ", RegexOptions.IgnoreCase);
    alt = Regex.Replace(alt, "Arial", " aerial ", RegexOptions.IgnoreCase);
    alt = Regex.Replace(alt, "Front", " front ", RegexOptions.IgnoreCase);
    alt = Regex.Replace(alt, "Back", " back ", RegexOptions.IgnoreCase);
    alt = Regex.Replace(alt, "Garage", " garage ", RegexOptions.IgnoreCase);
    alt = Regex.Replace(alt, "Entryway", " entryway ", RegexOptions.IgnoreCase);
    alt = Regex.Replace(alt, "Laundry", " laundry ", RegexOptions.IgnoreCase);
    alt = Regex.Replace(alt, "Swing", " swing ", RegexOptions.IgnoreCase);
    alt = Regex.Replace(alt, "RecordPlayer", " record player ", RegexOptions.IgnoreCase);
    alt = alt.Replace('_', ' ');
    alt = Regex.Replace(alt, @"\(\s*2\s*\)", " alternate ", RegexOptions.IgnoreCase);
    alt = Regex.Replace(alt, @"\s+", " ").Trim();

    return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(alt.ToLowerInvariant());
}

static string SortKey(string baseName)
{
    var lower = baseName.ToLowerInvariant();
    string[] priority =
    [
        "frontarial", "arialback", "backarial", "arial", "front", "entryway",
        "living", "dining", "kitchen", "master", "bed", "bath", "balcony",
        "back", "swing", "garage", "laundry", "record"
    ];

    var idx = Array.FindIndex(priority, lower.Contains);
    return $"{(idx < 0 ? 99 : idx):D2}_{lower}";
}

file sealed record PhotoResult(
    string BaseName,
    string OutName,
    string Alt,
    string SortKey,
    long SrcBytes,
    long OutBytes);
