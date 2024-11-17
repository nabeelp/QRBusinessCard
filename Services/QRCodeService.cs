using QRCoder;

namespace QRBusinessCard.Services;

public class QRCodeService
{
    public string GenerateVCardQRCode(string firstName, string lastName, string company, string jobTitle, string email, string phone)
    {
        var vCardData = $@"BEGIN:VCARD
VERSION:3.0
N:{lastName};{firstName};;;
FN:{firstName} {lastName}
ORG:{company}
TITLE:{jobTitle}
EMAIL:{email}
TEL:{phone}
END:VCARD";

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(vCardData, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);
        return $"data:image/png;base64,{Convert.ToBase64String(qrCodeImage)}";
    }
} 