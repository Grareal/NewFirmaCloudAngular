using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using FirmaOperaCloud.Application.Contracts;
using FirmaOperaCloud.Shared.Options;
using Microsoft.Extensions.Options;

namespace FirmaOperaCloud.Infrastructure.Opera;

public sealed record OperaUdfValue(string Code, string Description, int DisplayOrder, string GroupCode);

/// <summary>Catálogo maestro UDF, separado de los promotionCodes tarifarios.</summary>
public sealed class OperaUdfCatalog(HttpClient http, IOperaTokenService tokens, IOptions<OperaCloudOptions> settings)
{
    public const string GroupCode = "VIDA_PROMOTIONSTSW";
    private const string Path = "/ent/config/v1/userDefinedFieldValues";

    public async Task<IReadOnlyList<OperaUdfValue>> GetAsync(CancellationToken ct)
    {
        var options = settings.Value;
        var first = new Uri(options.GatewayUrl.TrimEnd('/') + Path + "?groupCode=" + GroupCode);
        Uri? next = first;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var result = new Dictionary<string, OperaUdfValue>(StringComparer.Ordinal);
        var token = await tokens.GetAccessTokenAsync(ct);
        while (next is not null)
        {
            if (next.Scheme != first.Scheme || next.Authority != first.Authority || next.AbsolutePath != first.AbsolutePath ||
                !visited.Add(next.AbsoluteUri) || visited.Count > 100)
                throw new InvalidDataException("OPERA devolvió una continuación inválida del catálogo UDF.");
            using var request = new HttpRequestMessage(HttpMethod.Get, next);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("x-app-key", options.AppKey);
            request.Headers.Add("x-hotelid", options.DefaultHotelId);
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"No se pudo consultar el catálogo UDF en OPERA (HTTP {(int)response.StatusCode}).", null, response.StatusCode);
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            foreach (var row in Parse(document.RootElement)) result[row.Code] = row;
            next = Next(document.RootElement, next);
        }
        return result.Values.OrderBy(x => x.Code, StringComparer.Ordinal).ToArray();
    }

    public static IReadOnlyList<OperaUdfValue> Parse(JsonElement root)
    {
        if (!root.TryGetProperty("userDefinedFieldValues", out var values) || values.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("La respuesta de OPERA no contiene el catálogo UDF esperado.");
        var rows = new List<OperaUdfValue>();
        foreach (var item in values.EnumerateArray())
        {
            var group = Text(item, "groupCode");
            if (group != GroupCode) continue;
            var code = Text(item, "code");
            if (string.IsNullOrWhiteSpace(code)) continue;
            var description = item.TryGetProperty("description", out var d) ? Text(d, "defaultText") : string.Empty;
            int.TryParse(Text(item, "displayOrder"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var order);
            rows.Add(new(code, description, order, group));
        }
        return rows;
    }

    private static Uri? Next(JsonElement root, Uri current)
    {
        if (root.TryGetProperty("links", out var links) && links.ValueKind == JsonValueKind.Array)
            foreach (var link in links.EnumerateArray())
                if (Text(link, "rel").Equals("next", StringComparison.OrdinalIgnoreCase))
                {
                    var href = Text(link, "href");
                    if (string.IsNullOrWhiteSpace(href)) throw new InvalidDataException("OPERA devolvió una página siguiente vacía.");
                    return new Uri(current, href);
                }
        if (root.TryGetProperty("hasMore", out var more) && more.ValueKind == JsonValueKind.True)
            throw new InvalidDataException("OPERA indica más resultados sin proporcionar una página siguiente. No se mostrará un catálogo incompleto.");
        return null;
    }

    private static string Text(JsonElement item, string name) => item.ValueKind == JsonValueKind.Object && item.TryGetProperty(name, out var value)
        ? value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString() : string.Empty;
}
