using Moq;
using Moq.Protected;
using NUnit.Framework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using ResumeAI.Export.API.Entities;
using ResumeAI.Export.API.Interfaces;
using ResumeAI.Export.API.Services;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using ResumeAI.Export.API.Models;

namespace ResumeAI.Export.Tests
{
    [TestFixture]
    public class ExportServiceTests
    {
        private Mock<IExportRepository> _exportRepoMock;
        private Mock<IConfiguration> _configMock;
        private Mock<IPdfRenderer> _pdfRendererMock;
        private Mock<INotificationPublisher> _notifMock;
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private Mock<IHttpContextAccessor> _httpContextMock;
        private Mock<ILogger<ExportService>> _loggerMock;
        private ExportService _exportService;

        [SetUp]
        public void Setup()
        {
            _exportRepoMock = new Mock<IExportRepository>();
            _configMock = new Mock<IConfiguration>();
            _pdfRendererMock = new Mock<IPdfRenderer>();
            _notifMock = new Mock<INotificationPublisher>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpContextMock = new Mock<IHttpContextAccessor>();
            _loggerMock = new Mock<ILogger<ExportService>>();

            _configMock.Setup(c => c["AzureBlob:ConnectionString"]).Returns("");

            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "Bearer test-token";
            _httpContextMock.Setup(h => h.HttpContext).Returns(context);

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) => {
                    if (req.RequestUri.ToString().Contains("resumes"))
                        return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new ApiResponse<ResumeDto>(true, new ResumeDto(1, 1, "T", "J", 1, 0, ResumeStatus.DRAFT, "En", false, 0, DateTime.UtcNow, DateTime.UtcNow, null), "")) };
                    if (req.RequestUri.ToString().Contains("sections"))
                        return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new ApiResponse<IList<SectionDto>>(true, new List<SectionDto>(), "")) };
                    if (req.RequestUri.ToString().Contains("profile"))
                        return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new ApiResponse<UserDto>(true, new UserDto(1, "Name", "email@test.com", "P", Role.USER, AuthProvider.LOCAL, true, SubscriptionPlan.FREE, DateTime.UtcNow), "")) };
                    if (req.RequestUri.ToString().Contains("templates"))
                        return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new ApiResponse<TemplateDto>(true, new TemplateDto(1, "Name", "D", "U", "H", "C", TemplateCategory.PROFESSIONAL, false, true, 0, DateTime.UtcNow), "")) };
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };
                });

            var httpClient = new HttpClient(handlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _exportService = new ExportService(_exportRepoMock.Object, _configMock.Object, _pdfRendererMock.Object, _notifMock.Object, _httpClientFactoryMock.Object, _httpContextMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task ExportToPdfAsync_ShouldCreateJobAndComplete()
        {
            var job = new ExportJob { JobId = "123", ResumeId = 1, UserId = 1, Status = ExportStatus.QUEUED };
            _exportRepoMock.Setup(r => r.AddAsync(It.IsAny<ExportJob>())).ReturnsAsync(job);
            _exportRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ExportJob>())).ReturnsAsync(job);
            _pdfRendererMock.Setup(r => r.GeneratePdf(It.IsAny<ExportData>())).Returns(new byte[1024]);

            var result = await _exportService.ExportToPdfAsync(1, new ExportRequest(1, ""));
            Assert.That(result.Status, Is.EqualTo(ExportStatus.COMPLETED));
        }

        [Test]
        public async Task ExportToDocxAsync_ShouldCreateJobAndComplete()
        {
            var job = new ExportJob { JobId = "456", ResumeId = 1, UserId = 1, Status = ExportStatus.QUEUED };
            _exportRepoMock.Setup(r => r.AddAsync(It.IsAny<ExportJob>())).ReturnsAsync(job);
            _exportRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ExportJob>())).ReturnsAsync(job);

            var result = await _exportService.ExportToDocxAsync(1, new ExportRequest(1, ""));
            Assert.That(result.Status, Is.EqualTo(ExportStatus.COMPLETED));
        }

        [Test]
        public async Task ExportToJsonAsync_ShouldCreateJobAndComplete()
        {
            var job = new ExportJob { JobId = "789", ResumeId = 1, UserId = 1, Status = ExportStatus.QUEUED };
            _exportRepoMock.Setup(r => r.AddAsync(It.IsAny<ExportJob>())).ReturnsAsync(job);
            _exportRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ExportJob>())).ReturnsAsync(job);

            var result = await _exportService.ExportToJsonAsync(1, new ExportRequest(1, ""));
            Assert.That(result.Status, Is.EqualTo(ExportStatus.COMPLETED));
        }

        [Test]
        public async Task GetJobStatusAsync_ShouldReturnDto()
        {
            _exportRepoMock.Setup(r => r.FindByJobIdAsync("123")).ReturnsAsync(new ExportJob { JobId = "123" });
            var result = await _exportService.GetJobStatusAsync("123");
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetExportsByUserAsync_ShouldReturnList()
        {
            _exportRepoMock.Setup(r => r.FindByUserIdAsync(1)).ReturnsAsync(new List<ExportJob> { new ExportJob { JobId = "1" } });
            var result = await _exportService.GetExportsByUserAsync(1);
            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task DeleteExportAsync_ShouldCallRepo()
        {
            await _exportService.DeleteExportAsync("123");
            _exportRepoMock.Verify(r => r.DeleteByJobIdAsync("123"), Times.Once);
        }

        [Test]
        public async Task CleanupExpiredExportsAsync_ShouldCallRepo()
        {
            await _exportService.CleanupExpiredExportsAsync();
            _exportRepoMock.Verify(r => r.DeleteExpiredJobsAsync(It.IsAny<DateTime>()), Times.Once);
        }

        [Test]
        public async Task GetExportStatsAsync_ShouldReturnDictionary()
        {
            _exportRepoMock.Setup(r => r.CountByStatusAndUserAsync(It.IsAny<ExportStatus>(), 1)).ReturnsAsync(5);
            var result = await _exportService.GetExportStatsAsync(1);
            Assert.That(result.ContainsKey("COMPLETED"), Is.True);
        }

        [Test]
        public async Task ExportToPdfAsync_ShouldSetFailed_OnException()
        {
            var job = new ExportJob { JobId = "error", ResumeId = 1, UserId = 1 };
            _exportRepoMock.Setup(r => r.AddAsync(It.IsAny<ExportJob>())).ReturnsAsync(job);
            _exportRepoMock.Setup(r => r.UpdateAsync(job)).ReturnsAsync(job);
            _pdfRendererMock.Setup(r => r.GeneratePdf(It.IsAny<ExportData>())).Throws(new Exception("Fail"));

            var result = await _exportService.ExportToPdfAsync(1, new ExportRequest(1, ""));
            Assert.That(result.Status, Is.EqualTo(ExportStatus.FAILED));
        }

        [Test]
        public void DownloadFileAsync_ShouldThrow_IfJobNotFound()
        {
            _exportRepoMock.Setup(r => r.FindByJobIdAsync("none")).ReturnsAsync((ExportJob)null);
            Assert.ThrowsAsync<KeyNotFoundException>(() => _exportService.DownloadFileAsync("none"));
        }
    }
}
