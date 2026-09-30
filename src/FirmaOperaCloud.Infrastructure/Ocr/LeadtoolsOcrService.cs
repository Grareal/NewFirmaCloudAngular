using FirmaOperaCloud.Application.Contracts;
using Leadtools;
using Leadtools.Codecs;
using Leadtools.Ocr;
 

namespace FirmaOperaCloud.Infrastructure.Ocr;

public sealed class LeadtoolsOcrService : IOcrService
{


private static bool _licenseLoaded;

private static void EnsureLicense()
{
    if (_licenseLoaded)
        return;

    var licenseFile =
        @"C:\LEADTOOLS23\Support\Common\License\LEADTOOLS.LIC";

    var keyFile =
        @"C:\LEADTOOLS23\Support\Common\License\LEADTOOLS.LIC.KEY";

    var developerKey =
        File.ReadAllText(keyFile);

    RasterSupport.SetLicense(
        licenseFile,
        developerKey);

    Console.WriteLine(
        $"License loaded. KernelExpired={RasterSupport.KernelExpired}");

    _licenseLoaded = true;
}

    //Revisar si la configuracion es adecuada para el leadtools y usarla en el lector ocr
    private const string RuntimePath =
    @"C:\Users\enriquemeza\Desktop\migracion\FirmaOperaCloud-Angular\src\FirmaOperaCloud.Api\bin\Debug\net10.0\OcrLEADRuntime";

         
    public Task<OcrReadResult> RecognizeAsync(
        byte[] imageBytes,
        string language,
        CancellationToken ct)
    {
        Console.WriteLine(typeof(RasterCodecs).Assembly.Location);
        Console.WriteLine(typeof(Leadtools.RasterException).Assembly.Location);
        EnsureLicense();
        using var codecs = new RasterCodecs();
        using var stream = new MemoryStream(imageBytes);

        

        var image = codecs.Load(stream);

        using var engine =
            OcrEngineManager.CreateEngine(
                Leadtools.Ocr.OcrEngineType.LEAD);

        engine.Startup(
            codecs,
            null,
            null,
            RuntimePath);

        using var page =
            engine.CreatePage(
                image,
                OcrImageSharingMode.AutoDispose);

        page.AutoPreprocess(
            OcrAutoPreprocessPageCommand.All,
            null);

        page.AutoZone(null);

        page.Recognize(null);

        var text = page.GetText(-1);

        return Task.FromResult(
            new OcrReadResult(
                text ?? string.Empty,
                1f,
                "Leadtools"));
    }
}