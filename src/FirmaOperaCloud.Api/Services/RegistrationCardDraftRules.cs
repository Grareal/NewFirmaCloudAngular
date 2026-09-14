using FirmaOperaCloud.Api.Controllers;

namespace FirmaOperaCloud.Api.Services;

public static class RegistrationCardDraftRules
{
    public static string? Validate(FillOfficialRegistrationCardRequest input, FillOfficialRegistrationCardRequest? previous)
    {
        if (input.Occupants is null || input.Occupants.Count > 8)
            return "El mapeo actual admite hasta ocho acompañantes.";
        if (input.Occupants.Any(x => x is null || !Guid.TryParse(x.ClientId, out _) || x.Name.Length > 200) ||
            input.Occupants.Select(x => x.ClientId).Distinct().Count() != input.Occupants.Count)
            return "Los firmantes deben tener identificadores únicos y nombres de hasta 200 caracteres.";
        if ((input.PrimaryGuestName?.Length ?? 0) > 200) return "El nombre del titular es demasiado largo.";
        if (!ValidPng(input.PrimarySignaturePngBase64) || input.Occupants.Any(x => !ValidPng(x.SignaturePngBase64)))
            return "Cada firma debe ser una imagen PNG válida de hasta 1 MB.";
        if (input.Occupants.Any(x => !string.IsNullOrWhiteSpace(x.SignaturePngBase64) && string.IsNullOrWhiteSpace(x.Name)))
            return "Capture el nombre de cada persona que firma.";
        if (previous is null) return null;
        if (!string.IsNullOrWhiteSpace(previous.PrimarySignaturePngBase64) &&
            (previous.PrimarySignaturePngBase64 != input.PrimarySignaturePngBase64 ||
             previous.PrimarySignerId != input.PrimarySignerId || previous.PrimaryGuestName != input.PrimaryGuestName))
            return "La firma confirmada del titular no se puede eliminar, sustituir ni reasignar.";
        for (var i = 0; i < previous.Occupants.Count; i++)
        {
            var old = previous.Occupants[i];
            if (string.IsNullOrWhiteSpace(old.SignaturePngBase64)) continue;
            var current = input.Occupants.ElementAtOrDefault(i);
            if (current is null || old.ClientId != current.ClientId || old.Name != current.Name ||
                old.SignerId != current.SignerId || old.SignaturePngBase64 != current.SignaturePngBase64)
                return "Las firmas confirmadas deben conservar su persona y posición en el PDF.";
        }
        return null;
    }

    private static bool ValidPng(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (value.StartsWith("data:image/png;base64,", StringComparison.Ordinal)) value = value[22..];
        if (value.Length > 1_400_000) return false;
        try
        {
            var bytes = Convert.FromBase64String(value);
            return bytes.Length is > 24 and <= 1_000_000 &&
                bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        }
        catch (FormatException) { return false; }
    }
}
