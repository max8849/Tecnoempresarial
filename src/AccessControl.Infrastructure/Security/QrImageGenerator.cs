using AccessControl.Application.Common.Interfaces;
using QRCoder;

namespace AccessControl.Infrastructure.Security;

public class QrImageGenerator : IQrImageGenerator
{
    public string GeneratePngBase64(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(data);
        var bytes = pngQr.GetGraphic(20);
        return Convert.ToBase64String(bytes);
    }
}
