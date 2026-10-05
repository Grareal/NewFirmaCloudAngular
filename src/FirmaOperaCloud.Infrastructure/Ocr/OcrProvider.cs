using FirmaOperaCloud.Application.Contracts;

namespace FirmaOperaCloud.Infrastructure.Ocr;

/// <summary>Selecciona una de las implementaciones OCR registradas.</summary>
public sealed class OcrProvider(
    TesseractOcrService tesseract,
    IronOcrService ironOcr,
    LeadtoolsOcrService leadtools)
{
    public IOcrService Get(OcrEngineType engine) => engine switch
    {
        OcrEngineType.Tesseract => tesseract,
        OcrEngineType.IronOcr => ironOcr,
        OcrEngineType.Leadtools => leadtools,
        _ => throw new ArgumentOutOfRangeException(nameof(engine), engine, "Motor OCR no soportado.")
    };
}
