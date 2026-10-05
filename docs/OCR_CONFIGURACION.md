# Configuración OCR

La API permite elegir Tesseract, IronOCR o LEADTOOLS desde la pantalla de captura. Los tres motores entregan el mismo resultado normalizado y la revisión humana sigue siendo obligatoria.

## IronOCR

Solo hace falta establecer la licencia como secreto:

```powershell
$env:IronOcr__LicenseKey = "SU-LICENCIA"
```

La aplicación valida la licencia antes del primer reconocimiento. El español reutiliza `src/FirmaOperaCloud.Api/tessdata/spa.traineddata`, que ya se copia al publicar la API; no hay que instalar un paquete antiguo de idioma. Si se aloja el archivo en otra ubicación, use `IronOcr__SpanishTrainedDataPath`.

Iron Software recomienda asignar la clave antes de usar la librería y comprobar `License.IsLicensed`: [documentación de licencia](https://ironsoftware.com/ocr/csharp/get-started/license-keys/). Su documentación de español menciona un paquete NuGet que actualmente solo tiene versiones antiguas; por eso esta integración usa el mecanismo oficial de archivo Tesseract personalizado incluido en la API.

## LEADTOOLS 23

LEADTOOLS entrega dos valores: el archivo de licencia `.LIC` y la developer key. Configure:

```powershell
$env:Leadtools__LicenseFile = "C:\LEADTOOLS23\Support\Common\License\LEADTOOLS.LIC"
$env:Leadtools__DeveloperKeyFile = "C:\LEADTOOLS23\Support\Common\License\LEADTOOLS.LIC.KEY"
```

También puede poner el contenido de la key directamente en `Leadtools__DeveloperKey`, preferiblemente mediante User Secrets, Key Vault o el almacén de secretos del despliegue.

Los binarios y archivos de idioma provienen del paquete oficial `Leadtools.Ocr` 23.0.0.7. Normalmente el runtime OCR se autodetecta desde la salida publicada. Con una instalación local especial se pueden indicar:

```powershell
$env:Leadtools__OcrRuntimePath = "C:\LEADTOOLS23\Bin\Common\OcrLEADRuntime"
$env:Leadtools__NativeLibraryPath = "C:\LEADTOOLS23\Bin\CDLL\x64"
```

LEADTOOLS exige llamar `SetLicense` antes de cualquier otra función y, en .NET 6+, configurar `Platform.LibraryPath` cuando se usan binarios nativos del instalador: [tutorial oficial de licencia](https://www.leadtools.com/help/sdk/tutorials/dotnet-console-add-references-and-set-a-license.html). El motor OCR debe iniciarse con `Startup` y liberarse al terminar: [documentación de Startup](https://www.leadtools.com/help/sdk/v23/dh/to/starting-and-shutting-down-the-ocr-engine.html).

Para Docker, monte el `.LIC` y opcionalmente `.LIC.KEY` como secretos/archivos de solo lectura y use rutas internas del contenedor. No copie licencias dentro de la imagen.

## Validación

```powershell
dotnet restore FirmaOperaCloud.slnx
dotnet build FirmaOperaCloud.slnx --no-restore -m:1
dotnet test tests\FirmaOperaCloud.Tests\FirmaOperaCloud.Tests.csproj --no-build
```

En esta versión del SDK, la compilación paralela puede terminar con cero errores pero código de salida 1 al consultar proyectos referenciados. `-m:1` evita ese problema del entorno y muestra los errores reales.
