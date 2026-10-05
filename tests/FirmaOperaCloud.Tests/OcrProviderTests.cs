using System;
using FirmaOperaCloud.Infrastructure.Ocr;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FirmaOperaCloud.Tests;

public sealed class OcrProviderTests
{
    [Theory]
    [InlineData(OcrEngineType.Tesseract, typeof(TesseractOcrService))]
    [InlineData(OcrEngineType.IronOcr, typeof(IronOcrService))]
    [InlineData(OcrEngineType.Leadtools, typeof(LeadtoolsOcrService))]
    public void GetReturnsRequestedEngine(OcrEngineType engine, Type expectedType)
    {
        var configuration = new ConfigurationBuilder().Build();
        var provider = new OcrProvider(
            new TesseractOcrService(),
            new IronOcrService(configuration),
            new LeadtoolsOcrService(configuration));

        Assert.IsType(expectedType, provider.Get(engine));
    }
}
