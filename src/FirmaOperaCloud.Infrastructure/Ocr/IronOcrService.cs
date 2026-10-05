using FirmaOperaCloud.Application.Contracts;
using IronOcr;
using Microsoft.Extensions.Configuration;

namespace FirmaOperaCloud.Infrastructure.Ocr;

public sealed class IronOcrService(IConfiguration configuration) : IOcrService
{
    public Task<OcrReadResult> RecognizeAsync(
        byte[] imageBytes,
        string language,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        if (imageBytes.Length == 0)
            throw new ArgumentException("La imagen está vacía.", nameof(imageBytes));

        ConfigureLicense();

        return Task.Run(() =>
        {
            using var input = new OcrInput();
            input.LoadImage(imageBytes);
            var usesSpanish = UsesSpanish(language);
            var ocr = new IronTesseract();
            if (usesSpanish)
            {
                ocr.UseCustomTesseractLanguageFile(SpanishLanguageFile());
                if (UsesEnglish(language)) ocr.AddSecondaryLanguage(OcrLanguage.English);
            }
            else
            {
                ocr.Language = OcrLanguage.English;
            }

            var result = ocr.Read(input);
            return new OcrReadResult(
                result.Text?.Trim() ?? string.Empty,
                Math.Clamp((float)(result.Confidence / 100d), 0f, 1f),
                usesSpanish && UsesEnglish(language) ? "spa+eng" : usesSpanish ? "spa" : "eng");
        }, ct);
    }

    private void ConfigureLicense()
    {
        var key = configuration["IronOcr:LicenseKey"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException(
                "IronOCR no está configurado. Defina IronOcr__LicenseKey en el entorno o en User Secrets.");

        if (!string.Equals(License.LicenseKey, key, StringComparison.Ordinal))
            License.LicenseKey = key;

        if (!License.IsLicensed)
            throw new InvalidOperationException(
                "IronOCR rechazó la licencia. Compruebe que la clave pertenece a IronOCR y no está vencida.");
    }

    private static bool UsesSpanish(string language) =>
        string.IsNullOrWhiteSpace(language) ||
        language.Contains("spa", StringComparison.OrdinalIgnoreCase) ||
        language.Contains("es", StringComparison.OrdinalIgnoreCase);

    private static bool UsesEnglish(string language) =>
        language.Contains("eng", StringComparison.OrdinalIgnoreCase) ||
        language.Contains("en", StringComparison.OrdinalIgnoreCase);

    private string SpanishLanguageFile()
    {
        var configured = configuration["IronOcr:SpanishTrainedDataPath"];
        var path = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "tessdata", "spa.traineddata")
            : configured;

        if (!File.Exists(path))
            throw new FileNotFoundException(
                "No se encontró spa.traineddata para IronOCR. Configure IronOcr__SpanishTrainedDataPath.", path);
        return path;
    }
}
