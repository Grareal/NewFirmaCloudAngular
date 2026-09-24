using FirmaOperaCloud.Domain.Entities;

namespace FirmaOperaCloud.Application.Contracts;

public sealed record OperaAttachmentResult(
    string AttachmentId,
    string FileName,
    int FileSize,
    string? Description);

public static class OperaAttachmentUploadOutcomes
{
    public const string Uploaded = "Uploaded";
    public const string Replaced = "Replaced";
    public const string SkippedExisting = "SkippedExisting";
}

public sealed record OperaAttachmentUploadResult(
    string AttachmentId,
    string FileName,
    int FileSize,
    string? Description,
    string Outcome);

public interface IOperaRegistrationCardService
{
    string ResolveTemplate(Reservation reservation, string? requestedTemplate = null);

    Task<byte[]> GetOfficialPdfAsync(
        string hotelId,
        string reservationId,
        string template,
        string? language = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperaAttachmentResult>> GetAttachmentsAsync(
        string hotelId,
        string reservationId,
        CancellationToken cancellationToken = default);

    Task<OperaAttachmentUploadResult> UploadPdfAsync(
        string hotelId,
        string reservationId,
        string confirmationNumber,
        byte[] pdf,
        string userName,
        CancellationToken cancellationToken = default,
        string? documentVersion = null);
}
