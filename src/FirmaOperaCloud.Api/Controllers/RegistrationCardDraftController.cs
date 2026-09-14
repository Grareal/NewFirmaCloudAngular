using System.Text.Json;
using FirmaOperaCloud.Api.Services;
using FirmaOperaCloud.Application.Contracts;
using FirmaOperaCloud.Domain.Entities;
using FirmaOperaCloud.Infrastructure.Persistence;
using FirmaOperaCloud.Shared.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FirmaOperaCloud.Api.Controllers;

[ApiController, Authorize(Policy = "RegistrationCard.Use")]
[Route("api/reservations/{confirmation}/signature-draft")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RegistrationCardDraftController(
    FirmaOperaCloudDbContext db, IReservationService reservations, IOptions<OperaCloudOptions> options) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet]
    public async Task<IActionResult> Get(string confirmation, CancellationToken ct)
    {
        var reservation = await Resolve(confirmation, ct);
        if (reservation is null) return NotFound(new { message = "No se encontró la reserva." });
        var id = ReservationId(reservation);
        var draft = await db.RegistrationCardDrafts.AsNoTracking().SingleOrDefaultAsync(x =>
            x.HotelId == reservation.HotelId && x.ConfirmationNumber == confirmation && x.ReservationId == id, ct);
        return Ok(draft is null ? new SignatureDraftResponse(null, await PreviousDocument(reservation, id, ct), null) : Response(draft));
    }

    [HttpPut, RequestSizeLimit(14_000_000)]
    public async Task<IActionResult> Save(string confirmation, SaveSignatureDraft request, CancellationToken ct)
    {
        if (request.Input is null) return BadRequest(new { message = "Faltan los datos de la tarjeta." });
        var reservation = await Resolve(confirmation, ct);
        if (reservation is null) return NotFound(new { message = "No se encontró la reserva." });
        var id = ReservationId(reservation);
        var draft = await db.RegistrationCardDrafts.SingleOrDefaultAsync(x =>
            x.HotelId == reservation.HotelId && x.ConfirmationNumber == confirmation && x.ReservationId == id, ct);
        if (draft?.Revision != request.Revision) return Conflict(new { message = "Otro dispositivo guardó cambios. Recarga el avance antes de continuar; no se sobrescribió ninguna firma." });
        var previous = draft is null ? await PreviousDocument(reservation, id, ct) : JsonSerializer.Deserialize<FillOfficialRegistrationCardRequest>(draft.InputJson, JsonOptions);
        var error = RegistrationCardDraftRules.Validate(request.Input, previous);
        if (error is not null) return BadRequest(new { message = error });
        if (draft is null)
        {
            draft = new RegistrationCardDraft { HotelId = reservation.HotelId, ConfirmationNumber = confirmation, ReservationId = id };
            db.RegistrationCardDrafts.Add(draft);
        }
        draft.InputJson = JsonSerializer.Serialize(request.Input, JsonOptions);
        draft.Revision = Guid.NewGuid();
        draft.UpdatedAtUtc = DateTime.UtcNow;
        draft.UpdatedBy = User.Identity?.Name;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { message = "El avance cambió en otro dispositivo. Recarga antes de guardar." }); }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql && sql.Number is 2601 or 2627)
        { return Conflict(new { message = "Otro dispositivo inició esta tarjeta. Recarga el avance." }); }
        return Ok(Response(draft));
    }

    private async Task<Reservation?> Resolve(string confirmation, CancellationToken ct) =>
        (await reservations.GetByConfirmationNumberAsync(options.Value.DefaultHotelId, confirmation, ct))
        .FirstOrDefault(x => x.ConfirmationNumber == confirmation);

    // Cards generated before drafts were introduced can also be reopened.
    private async Task<FillOfficialRegistrationCardRequest?> PreviousDocument(Reservation r, string id, CancellationToken ct)
    {
        var document = await db.LocalDocuments.AsNoTracking().Include(x => x.Signatures).ThenInclude(x => x.StoredSignature)
            .Where(x => x.HotelId == r.HotelId && x.ConfirmationNumber == r.ConfirmationNumber && x.ReservationId == id && x.Signatures.Any())
            .OrderByDescending(x => x.Version).FirstOrDefaultAsync(ct);
        if (document is null) return null;
        var input = new FillOfficialRegistrationCardRequest
        {
            PrimaryGuestName = r.Guest.FullName, PrimarySignerId = r.Guest.Id, Email = r.Guest.Email,
            CellPhone = r.Guest.PhoneNumber, City = r.Guest.Address.City,
            State = r.Guest.Address.StateProvCode, Country = r.Guest.Address.CountryCode
        };
        foreach (var link in document.Signatures.OrderBy(x => x.Position))
        {
            var signature = link.StoredSignature;
            if (signature is null || !signature.IsActive) continue;
            var png = "data:image/png;base64," + Convert.ToBase64String(signature.SignaturePng);
            if (link.Role == "PrimaryGuest")
            {
                input.PrimaryGuestName = signature.SignerName;
                input.PrimarySignerId = signature.OperaProfileId;
                input.PrimarySignaturePngBase64 = png;
            }
            else
            {
                // Old and new document links use 0 for the primary, 1..8 for occupants.
                while (input.Occupants.Count < link.Position)
                    input.Occupants.Add(new RegistrationCardOccupantRequest { ClientId = Guid.NewGuid().ToString() });
                if (link.Position > 0) input.Occupants[link.Position - 1] = new RegistrationCardOccupantRequest
                {
                    ClientId = signature.Id.ToString(), SignerId = signature.OperaProfileId,
                    Name = signature.SignerName, SignaturePngBase64 = png
                };
            }
        }
        return input;
    }

    private static string ReservationId(Reservation r) =>
        r.ReservationIdList.FirstOrDefault(x => x.Type.Equals("Reservation", StringComparison.OrdinalIgnoreCase))?.Id
        ?? throw new InvalidOperationException("La reserva no tiene identificador OPERA.");

    private static SignatureDraftResponse Response(RegistrationCardDraft draft) => new(draft.Revision,
        JsonSerializer.Deserialize<FillOfficialRegistrationCardRequest>(draft.InputJson, JsonOptions), draft.UpdatedAtUtc);
}

public sealed record SaveSignatureDraft(Guid? Revision, FillOfficialRegistrationCardRequest Input);
public sealed record SignatureDraftResponse(Guid? Revision, FillOfficialRegistrationCardRequest? Input, DateTime? UpdatedAtUtc);
