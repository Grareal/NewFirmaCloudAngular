using FirmaOperaCloud.Application.Contracts;
using Leadtools;
using Leadtools.Codecs;
using Leadtools.Ocr;
using Microsoft.Extensions.Configuration;

namespace FirmaOperaCloud.Infrastructure.Ocr;

public sealed class LeadtoolsOcrService(IConfiguration configuration) : IOcrService
{
    private static readonly object LicenseLock = new();
    private static string? loadedLicense;

    public Task<OcrReadResult> RecognizeAsync(
        byte[] imageBytes,
        string language,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        if (imageBytes.Length == 0)
            throw new ArgumentException("La imagen está vacía.", nameof(imageBytes));

        return Task.Run(() => Recognize(imageBytes, language), ct);
    }

    private OcrReadResult Recognize(byte[] imageBytes, string language)
    {
        ConfigureNativeLibraries();
        ConfigureLicense();

        using var codecs = new RasterCodecs();
        using var stream = new MemoryStream(imageBytes, writable: false);
        using var image = codecs.Load(stream);
        using var engine = OcrEngineManager.CreateEngine(Leadtools.Ocr.OcrEngineType.LEAD);

        var runtimePath = NullIfBlank(configuration["Leadtools:OcrRuntimePath"]);
        engine.Startup(codecs, null, null, runtimePath);
        EnableRequestedLanguages(engine, language);

        using var page = engine.CreatePage(image, OcrImageSharingMode.AutoDispose);
        page.AutoPreprocess(OcrAutoPreprocessPageCommand.All, null);
        page.AutoZone(null);
        page.Recognize(null);

        return new OcrReadResult(
            page.GetText(-1)?.Trim() ?? string.Empty,
            CalculateConfidence(page),
            string.Join('+', engine.LanguageManager.GetEnabledLanguages()));
    }

    private void ConfigureLicense()
    {
        var licenseFile = configuration["Leadtools:LicenseFile"];
        var developerKey = configuration["Leadtools:DeveloperKey"];
        var developerKeyFile = configuration["Leadtools:DeveloperKeyFile"];

        if (string.IsNullOrWhiteSpace(licenseFile))
            throw new InvalidOperationException(
                "LEADTOOLS no está configurado. Defina Leadtools__LicenseFile con la ruta de LEADTOOLS.LIC.");
        if (!File.Exists(licenseFile))
            throw new FileNotFoundException("No se encontró el archivo de licencia de LEADTOOLS.", licenseFile);

        if (string.IsNullOrWhiteSpace(developerKey) && !string.IsNullOrWhiteSpace(developerKeyFile))
        {
            if (!File.Exists(developerKeyFile))
                throw new FileNotFoundException("No se encontró el archivo de clave de LEADTOOLS.", developerKeyFile);
            developerKey = File.ReadAllText(developerKeyFile).Trim();
        }
        if (string.IsNullOrWhiteSpace(developerKey))
            throw new InvalidOperationException(
                "Defina Leadtools__DeveloperKey o Leadtools__DeveloperKeyFile.");

        lock (LicenseLock)
        {
            if (string.Equals(loadedLicense, licenseFile, StringComparison.OrdinalIgnoreCase)) return;

            RasterSupport.SetLicense(licenseFile, developerKey);
            if (RasterSupport.KernelExpired)
                throw new InvalidOperationException("La licencia de LEADTOOLS está vencida o no es válida.");
            loadedLicense = licenseFile;
        }
    }

    private void ConfigureNativeLibraries()
    {
        var path = NullIfBlank(configuration["Leadtools:NativeLibraryPath"]);
        if (path is not null) Platform.LibraryPath = path;
    }

    private static void EnableRequestedLanguages(IOcrEngine engine, string language)
    {
        var requested = language.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(MapLanguage)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(engine.LanguageManager.IsLanguageSupported)
            .ToArray();

        if (requested.Length > 0)
            engine.LanguageManager.EnableLanguages(requested);
    }

    private static string MapLanguage(string language) => language.ToLowerInvariant() switch
    {
        "spa" or "es" => "es",
        "eng" or "en" => "en",
        _ => language
    };

    private static float CalculateConfidence(IOcrPage page)
    {
        var values = page.GetRecognizedCharacters()
            .SelectMany(zone => zone)
            .Select(character => character.Confidence)
            .ToArray();
        return values.Length == 0
            ? 0f
            : (float)Math.Clamp(values.Average() / 100d, 0d, 1d);
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
