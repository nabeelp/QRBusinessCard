using QRCoder;
using QRBusinessCard.Models; 

namespace QRBusinessCard.Services;

public class QRCodeService
{
    public string GenerateVCardQRCode(ContactInfo contactInfo) 
    {
        var vCardData = BuildVCard(contactInfo);

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(vCardData, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);
        return $"data:image/png;base64,{Convert.ToBase64String(qrCodeImage)}";
    }

    public static string BuildVCard(ContactInfo contactInfo) 
    {
        return $@"BEGIN:VCARD
VERSION:3.0
N:{contactInfo.LastName};{contactInfo.FirstName};;;
FN:{contactInfo.FirstName} {contactInfo.LastName}
ORG:{contactInfo.Company}
TITLE:{contactInfo.JobTitle}
EMAIL:{contactInfo.Email}
TEL:{contactInfo.Phone}
END:VCARD";
    }
}