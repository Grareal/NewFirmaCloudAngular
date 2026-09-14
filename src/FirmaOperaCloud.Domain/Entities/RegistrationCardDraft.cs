namespace FirmaOperaCloud.Domain.Entities;

/// <summary>Avance recuperable por reserva. No es un documento enviado a OPERA.</summary>
public sealed class RegistrationCardDraft
{
    public string HotelId { get; set; } = string.Empty;
    public string ConfirmationNumber { get; set; } = string.Empty;
    public string ReservationId { get; set; } = string.Empty;
    public Guid Revision { get; set; } = Guid.NewGuid();
    public string InputJson { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
}
