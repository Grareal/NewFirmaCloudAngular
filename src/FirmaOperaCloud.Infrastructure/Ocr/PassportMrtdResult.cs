namespace FirmaOperaCloud.Infrastructure.Ocr;

public sealed record PassportMrtdResult(
    string? PassportNumber,
    string? FirstName,
    string? LastName,
    string? Nationality,
    string? Country,
    string? Gender,
    string? BirthDate,
    string? ExpirationDate,
    string[]? MrzLines);