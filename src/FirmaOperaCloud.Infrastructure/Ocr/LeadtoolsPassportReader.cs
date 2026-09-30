using Leadtools;
using Leadtools.Codecs;
 using Leadtools.Ocr;
using FirmaOperaCloud.Application.Contracts;
namespace FirmaOperaCloud.Infrastructure.Ocr;

public sealed class LeadtoolsPassportReader
{
    private readonly IOcrEngine _ocrEngine;
    private readonly RasterCodecs _codecs;

    public LeadtoolsPassportReader()
    {
        var lic =
            @"C:\LEADTOOLS23\Support\Common\License\LEADTOOLS.LIC";

        var key =
            File.ReadAllText(lic + ".KEY");
        //Configuracion inicial de la contraseña de la licencia de leadtoools . lic

        RasterSupport.SetLicense(
            lic,
            key);

        _codecs = new RasterCodecs();

        _ocrEngine =
            OcrEngineManager.CreateEngine(
                OcrEngineType.LEAD);

        _ocrEngine.Startup(
            null,
            null,
            null,
            @"C:\LEADTOOLS23\Bin\Common\OcrLEADRuntime");
    }

    public Dictionary<string,string> Read(
        byte[] bytes)
    {
        using var ms =
            new MemoryStream(bytes);

        using var image =
            _codecs.Load(ms);

        var reader =
            new MRTDReader();

        reader.OcrEngine =
            _ocrEngine;

        reader.ProcessImage(image);

        var result =
            new Dictionary<string,string>();

        foreach(var item in reader.Results)
        {
            result[item.Key.ToString()] =
                item.Value.ReadableValue;
        }

        return result;
    }
}