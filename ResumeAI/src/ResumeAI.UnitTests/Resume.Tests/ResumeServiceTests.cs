using Moq;
using Moq.Protected;
using NUnit.Framework;
using ResumeAI.Resume.API.Entities;
using ResumeAI.Resume.API.Interfaces;
using ResumeAI.Resume.API.Repositories;
using ResumeAI.Resume.API.Services;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;
using System.Net;
using System.Net.Http.Json;

namespace ResumeAI.Resume.Tests
{
    [TestFixture]
    public class ResumeServiceTests
    {
        private Mock<IResumeRepository> _resumeRepoMock;
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private ResumeService _resumeService;

        [SetUp]
        public void Setup()
        {
            _resumeRepoMock = new Mock<IResumeRepository>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(new ApiResponse<TemplateDto>(true, new TemplateDto(1, "Classic", "Desc", "url", "Html", "Css", TemplateCategory.PROFESSIONAL, false, true, 0, DateTime.UtcNow), ""))
                });

            var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://template-api/") };
            _httpClientFactoryMock.Setup(f => f.CreateClient("TemplateApi")).Returns(httpClient);

            _resumeService = new ResumeService(_resumeRepoMock.Object, _httpClientFactoryMock.Object);
        }

        [Test]
        public async Task CreateResumeAsync_ShouldSucceed_ForFreeUserUnderLimit()
        {
            _resumeRepoMock.Setup(r => r.CountByUserIdAsync(1)).ReturnsAsync(1);
            _resumeRepoMock.Setup(r => r.AddAsync(It.IsAny<ResumeRecord>())).ReturnsAsync(new ResumeRecord { ResumeId = 101, UserId = 1 });

            var result = await _resumeService.CreateResumeAsync(1, SubscriptionPlan.FREE, new CreateResumeRequest("My Resume", "Dev", 1, "English"));

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ResumeId, Is.EqualTo(101));
        }

        [Test]
        public async Task GetResumeByIdAsync_ShouldReturnDto_WhenFound()
        {
            _resumeRepoMock.Setup(r => r.FindByResumeIdAsync(1)).ReturnsAsync(new ResumeRecord { ResumeId = 1, Title = "Found" });
            var result = await _resumeService.GetResumeByIdAsync(1);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Title, Is.EqualTo("Found"));
        }

        [Test]
        public async Task UpdateResumeAsync_ShouldSucceed_WhenOwnerUpdates()
        {
            var resume = new ResumeRecord { ResumeId = 1, UserId = 1, Title = "Old" };
            _resumeRepoMock.Setup(r => r.FindByResumeIdAsync(1)).ReturnsAsync(resume);
            _resumeRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ResumeRecord>())).ReturnsAsync(resume);

            var result = await _resumeService.UpdateResumeAsync(1, 1, SubscriptionPlan.FREE, new UpdateResumeRequest("New", "Dev", 1, "En", ResumeStatus.DRAFT));
            Assert.That(result.Title, Is.EqualTo("New"));
        }
    }
}
