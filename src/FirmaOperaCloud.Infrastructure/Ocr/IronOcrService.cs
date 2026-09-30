using IronOcr;
using FirmaOperaCloud.Application.Contracts;
using Microsoft.Extensions.Configuration;

namespace FirmaOperaCloud.Infrastructure.Ocr;

public sealed class IronOcrService : IOcrService
{
    public IronOcrService(IConfiguration configuration)
    {
        var key = configuration["IronOcr:LicenseKey"];
        Console.WriteLine("============");
        Console.WriteLine($"IRON KEY: {key}");
        Console.WriteLine("============");
        License.LicenseKey = "IRONSUITE.GRAREALMEZA.OUTLOOK.COM.24332-98E65F9790-AXIFK-W6JJA56FNTLY-XK5RKLF4R7XN-DMHOT64KWUB4-R2FUWMEGCGII-MQOHYHLGNQXH-MHJEO4UORMZI-NYSLDM-TLSIYGHSOPWRUA-DEPLOYMENT.TRIAL-SWQTZS.TRIAL.EXPIRES.30.OCT.2026";

        License.LicenseKey =
            configuration["IronOcr:LicenseKey"];

     }

    public Task<OcrReadResult> RecognizeAsync(
        byte[] imageBytes,
        string language,
        CancellationToken ct)
    {
        using var ms = new MemoryStream(imageBytes);

        var input = new OcrInput(ms);

        var ocr = new IronTesseract();

        var result = ocr.Read(input);

        return Task.FromResult(
            new OcrReadResult(
                result.Text ?? "",
                (float)(result.Confidence / 100.0),
                "IronOCR"));
    }
}