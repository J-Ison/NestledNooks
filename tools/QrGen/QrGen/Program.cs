using QRCoder;

const string url = "http://backhillsnestlednooks.com/";
var outputPath = args.Length > 0
    ? args[0]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "NestledNooks", "wwwroot", "images", "backhillsnestlednooks-qrcode.png"));

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

using var generator = new QRCodeGenerator();
using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
using var png = new PngByteQRCode(data);
var bytes = png.GetGraphic(20);

File.WriteAllBytes(outputPath, bytes);
Console.WriteLine($"QR code saved: {outputPath}");
Console.WriteLine($"URL encoded: {url}");
