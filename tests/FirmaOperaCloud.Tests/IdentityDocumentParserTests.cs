using FirmaOperaCloud.Domain.Entities;
using FirmaOperaCloud.Infrastructure.Ocr;
using Xunit;

namespace FirmaOperaCloud.Tests;

public sealed class IdentityDocumentParserTests
{
    [Fact]
    public void PassportMrz_WithValidCheckDigits_IsAccepted()
    {
        const string line = "L898902C36UTO7408122F1204159ZE184226B<<<<<10";
        Assert.True(IdentityDocumentParser.VerifyPassportMrz(line));
    }

    [Fact]
    public void PassportMrz_WhenDocumentCheckDigitChanges_IsRejected()
    {
        const string line = "L898902C30UTO7408122F1204159ZE184226B<<<<<10";
        Assert.False(IdentityDocumentParser.VerifyPassportMrz(line));
    }

    [Theory]
    [InlineData("SECRETARIA DE MOVILIDAD\nLICENCIA DE CONDUCIR\nNO. DE LICENCIA ABC123456\nVENCE 2029", "Licencia de conducir", "ABC123456")]
    [InlineData("INSTITUTO NACIONAL DE MIGRACION\nTARJETA DE RESIDENTE TEMPORAL\nNUMERO DE TARJETA RT-987654", "Tarjeta de residencia", "RT-987654")]
    [InlineData("UNITED STATES OF AMERICA\nVISA\nDOCUMENT NUMBER V12345678", "Visa", "V12345678")]
    public void AdditionalIdentityDocuments_AreDetected(string text, string expectedType, string expectedNumber)
    {
        var parsed = IdentityDocumentParser.Parse(text, null, "Auto");
        Assert.Equal(expectedType, parsed.DocType);
        Assert.Equal(expectedNumber, parsed.DocumentNumber);
    }

    [Fact]
    public void NewLegalIdentifiers_AreVersionSevenGuids()
    {
        Assert.Equal(7, new ReservationFile().Id.Version);
        Assert.Equal(7, new AuditEvent().Id.Version);
        Assert.Equal(7, new LocalDocument().Id.Version);
    }
}
