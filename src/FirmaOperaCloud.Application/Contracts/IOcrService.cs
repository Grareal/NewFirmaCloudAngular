namespace FirmaOperaCloud.Application.Contracts;

/// <summary>Resultado normalizado de cualquier proveedor OCR.</summary>
public sealed record OcrReadResult(string Text, float MeanConfidence, string Language);

public interface IOcrService
{
    Task<OcrReadResult> RecognizeAsync(byte[] imageBytes, string language, CancellationToken ct);
}
