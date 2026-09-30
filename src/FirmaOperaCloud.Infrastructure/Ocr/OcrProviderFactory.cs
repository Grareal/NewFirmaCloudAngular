using FirmaOperaCloud.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace FirmaOperaCloud.Infrastructure.Ocr;

public sealed class OcrProviderFactory
    : IOcrProviderFactory
{
    private readonly IServiceProvider serviceProvider;

    public OcrProviderFactory(
        IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public IOcrService GetEngine(
        OcrEngineType engine)
    {
        return engine switch
        {
            OcrEngineType.IronOcr =>
                serviceProvider.GetRequiredService<IronOcrService>(),

            OcrEngineType.Leadtools =>
                serviceProvider.GetRequiredService<LeadtoolsOcrService>(),

            _ =>
                serviceProvider.GetRequiredService<TesseractOcrService>()
        };
    }
}