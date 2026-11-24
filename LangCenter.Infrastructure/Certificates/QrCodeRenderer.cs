using LangCenter.Application.Certificates;
using QRCoder;

namespace LangCenter.Infrastructure.Certificates;

public class QrCodeRenderer : IQrCodeRenderer
{
    public byte[] RenderPng(string value) => PngByteQRCodeHelper.GetQRCode(value, QRCodeGenerator.ECCLevel.Q, 8);
}
