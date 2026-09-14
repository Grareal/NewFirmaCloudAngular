using System.Text.Json;
using FirmaOperaCloud.Infrastructure.Opera;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirmaOperaCloud.Api.Controllers;

[ApiController, Authorize(Policy = "Promotions.Manage"), Route("api/promotion-catalog/udf-values")]
public sealed class UdfCatalogController(OperaUdfCatalog catalog) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        try { return Ok(await catalog.GetAsync(ct)); }
        catch (HttpRequestException ex) { return StatusCode(502, new { message = ex.Message }); }
        catch (InvalidDataException ex) { return StatusCode(502, new { message = ex.Message }); }
        catch (JsonException) { return StatusCode(502, new { message = "OPERA devolvió una respuesta UDF no válida." }); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { return StatusCode(504, new { message = "OPERA tardó demasiado en responder. Intenta actualizar el catálogo." }); }
    }
}
