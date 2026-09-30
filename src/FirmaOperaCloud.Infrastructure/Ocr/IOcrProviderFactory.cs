using FirmaOperaCloud.Application.Contracts;

namespace FirmaOperaCloud.Infrastructure.Ocr;

public interface IOcrProviderFactory
{
    IOcrService GetEngine(
        OcrEngineType engine);
}