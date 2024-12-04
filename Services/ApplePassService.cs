using System.Security.Cryptography.X509Certificates;
using PassKitHelper;
using QRBusinessCard.Models;

namespace QRBusinessCard.Services;

public class ApplePassService
{
    private readonly byte[] _passCertificate;
    private readonly byte[] _passCertificatePassword;

    public ApplePassService()
    {
        // See `https://github.com/justdmitry/PassKitHelper/blob/master/how_to_create_pfx.md` to create your onw pfx file
        // TODO: As per above, need to enroll to an Apple Developer Account, so can only test this once the enrollment is complete
        // TODO: Retrieve from configuration or KeyVault
        _passCertificate = File.ReadAllBytes("/path/to/your/certificate.p12");
        _passCertificatePassword = File.ReadAllBytes("your_certificate_password");
    }

    public async Task<byte[]> GenerateApplePass(ContactInfo contactInfo)
    {
        // Create the default options for the pass
        var options = new PassKitOptions()
        {
            PassCertificate = new X509Certificate2(_passCertificate),
            AppleCertificate = new X509Certificate2(_passCertificatePassword),
            // See https://developer.apple.com/documentation/walletpasses/pass for detail on properties below
            ConfigureNewPass =
                p =>
                    p.Standard
                        .PassTypeIdentifier("pass.com.apple.devpubs.example") // from Apple Developer Program account
                        .TeamIdentifier("A93A5CM278") // from Apple Developer Program account
                        .OrganizationName("PassKit") // from Apple Developer Program account
                    .VisualAppearance
                        .LogoText("Business Card"),
        };

        // Create a new pass
        // See https://developer.apple.com/documentation/walletpasses/pass for detail on properties below
        var passKitHelper = new PassKitHelper.PassKitHelper(options);
        var pass = passKitHelper.CreateNewPass()
            .Standard
                .SerialNumber(contactInfo.Id)
                .Description($"QR Business Card for {contactInfo.FirstName} {contactInfo.LastName}")
            .VisualAppearance
                .Barcodes(QRCodeService.BuildVCard(contactInfo), BarcodeFormat.QR)
                .ForegroundColor("rgb(44, 62, 80)")
                .BackgroundColor("rgb(149, 165, 166)")
                .LabelColor("rgb(236, 240, 241)")
            .Generic
                .PrimaryFields
                    .Add("name", "Name", $"{contactInfo.FirstName} {contactInfo.LastName}")
                    .Add("company", "Company", contactInfo.Company)
                .SecondaryFields
                    .Add("job", "Job Title", contactInfo.JobTitle)
                    .Add("email", "Email", contactInfo.Email)
                    .Add("phone", "Phone", contactInfo.Phone)
            ;

        var package = await passKitHelper.CreateNewPassPackage(pass)
            // .Icon(await File.ReadAllBytesAsync("images/icon.png"))
            // .Icon2X(await File.ReadAllBytesAsync("images/icon@2x.png"))
            // .Icon3X(await File.ReadAllBytesAsync("images/icon@3x.png"))
            // .Logo(await File.ReadAllBytesAsync("images/logo.jpg"))
            // .Strip(await File.ReadAllBytesAsync("images/strip.jpg"))
            // .Strip2X(await File.ReadAllBytesAsync("images/strip@2x.jpg"))
            // .Strip3X(await File.ReadAllBytesAsync("images/strip@3x.jpg"))
            .SignAndBuildAsync();

        return package.ToArray();
    }
}