#nullable enable
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FirmaOperaCloud.Application.Contracts;
using FirmaOperaCloud.Infrastructure.Opera;
using FirmaOperaCloud.Shared.Options;
using Microsoft.Extensions.Options;
using Xunit;

namespace FirmaOperaCloud.Tests;

public sealed class OperaRegistrationCardServiceTests
{
    [Fact]
    public async Task SkipIfExists_ReturnsExistingAttachmentWithoutPosting()
    {
        var handler = new AttachmentHandler("REGCARDABC123SIGNED.pdf");
        var service = CreateService(handler, RegistrationCardAttachmentPolicies.SkipIfExists);

        var result = await service.UploadPdfAsync("HOTEL", "RES-1", "ABC-123", [1, 2, 3], "tester", documentVersion: "2");

        Assert.Equal(OperaAttachmentUploadOutcomes.SkippedExisting, result.Outcome);
        Assert.Equal(0, handler.PostCount);
    }

    [Fact]
    public async Task Replace_PostsTheStableNameWithOverwriteEnabled()
    {
        var handler = new AttachmentHandler("REGCARDABC123SIGNED.pdf");
        var service = CreateService(handler, RegistrationCardAttachmentPolicies.Replace);

        var result = await service.UploadPdfAsync("HOTEL", "RES-1", "ABC-123", [1, 2, 3], "tester", documentVersion: "2");

        Assert.Equal(OperaAttachmentUploadOutcomes.Replaced, result.Outcome);
        Assert.Equal("REGCARDABC123SIGNED.pdf", handler.PostedFileName);
        Assert.Equal("Y", handler.OverwriteExisting);
    }

    [Fact]
    public async Task KeepAllVersions_PostsAnImmutableVersionedName()
    {
        var handler = new AttachmentHandler("REGCARDABC123SIGNED.pdf");
        var service = CreateService(handler, RegistrationCardAttachmentPolicies.KeepAllVersions);

        var result = await service.UploadPdfAsync("HOTEL", "RES-1", "ABC-123", [1, 2, 3], "tester", documentVersion: "2");

        Assert.Equal(OperaAttachmentUploadOutcomes.Uploaded, result.Outcome);
        Assert.Equal("REGCARDABC123SIGNED-V2.pdf", handler.PostedFileName);
        Assert.Equal("N", handler.OverwriteExisting);
    }

    private static OperaRegistrationCardService CreateService(HttpMessageHandler handler, string policy) =>
        new(new HttpClient(handler), new TokenService(), Options.Create(new OperaCloudOptions
        {
            GatewayUrl = "https://opera.test",
            AppKey = "app-key",
            RegistrationCardAttachmentPolicy = policy
        }));

    private sealed class TokenService : IOperaTokenService
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult("token");
    }

    private sealed class AttachmentHandler(string existingFileName) : HttpMessageHandler
    {
        public int PostCount { get; private set; }
        public string? PostedFileName { get; private set; }
        public string? OverwriteExisting { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post)
            {
                PostCount++;
                using var payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
                PostedFileName = payload.RootElement.GetProperty("fileName").GetString();
                OverwriteExisting = payload.RootElement.GetProperty("overwriteExistingFileYN").GetString();
                return Json("{}");
            }

            var fileName = PostedFileName ?? existingFileName;
            return Json(JsonSerializer.Serialize(new
            {
                reservationAttachments = new[]
                {
                    new { id = "ATT-1", fileName, fileSize = 123, description = "Registration Card" }
                }
            }));
        }

        private static HttpResponseMessage Json(string value) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(value, Encoding.UTF8, "application/json")
        };
    }
}
