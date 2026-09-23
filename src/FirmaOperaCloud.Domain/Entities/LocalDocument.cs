namespace FirmaOperaCloud.Domain.Entities;

public sealed class LocalDocument
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string HotelId { get; set; } = string.Empty;
    public string ConfirmationNumber { get; set; } = string.Empty;
    public string? ReservationId { get; set; }
    public string? RoomNumber { get; set; }
    public Guid? PdfTemplateId { get; set; }
    public Guid? ReservationFileId { get; set; }
    public int Version { get; set; }
    public string FileName { get; set; } = string.Empty;
    public byte[] PdfData { get; set; } = [];
    public string DocumentHash { get; set; } = string.Empty;
    public string Status { get; set; } = "Generated";
    public string? AttachmentId { get; set; }
    public string? AttachmentFileName { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UploadedAtUtc { get; set; }
    public DateTime? HumanReviewedAtUtc { get; set; }
    public string? HumanReviewedBy { get; set; }
    public DateTime? RetentionUntilUtc { get; set; }
    public bool IsLegalHold { get; set; }
    public bool IsHidden { get; set; }
    public DateTime? HiddenAtUtc { get; set; }
    public string? HiddenBy { get; set; }
    public string? HiddenReason { get; set; }
    public bool SignatureAuthorizationAccepted { get; set; }
    public bool MarketingConsent { get; set; }
    public DateTime? ConsentAcceptedAtUtc { get; set; }
    public string? ConsentTextVersion { get; set; }
    public PdfTemplate? PdfTemplate { get; set; }
    public ReservationFile? ReservationFile { get; set; }
    public ICollection<LocalDocumentSignature> Signatures { get; set; } = [];
}

public sealed class LocalDocumentSignature
{
    public Guid LocalDocumentId { get; set; }
    public Guid StoredSignatureId { get; set; }
    public string Role { get; set; } = string.Empty;
    public int Position { get; set; }
    public bool Reused { get; set; }
    public LocalDocument? LocalDocument { get; set; }
    public StoredSignature? StoredSignature { get; set; }
}
