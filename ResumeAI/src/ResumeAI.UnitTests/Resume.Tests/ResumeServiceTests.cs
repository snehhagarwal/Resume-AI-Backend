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
        public void CreateResumeAsync_ShouldThrowException_WhenFreeLimitReached()
        {
            _resumeRepoMock.Setup(r => r.CountByUserIdAsync(1)).ReturnsAsync(3);
            Assert.ThrowsAsync<InvalidOperationException>(() => _resumeService.CreateResumeAsync(1, SubscriptionPlan.FREE, new CreateResumeRequest("T", "J", 1, "E")));
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

        [Test]
        public void UpdateResumeAsync_ShouldThrowException_WhenNotOwner()
        {
            var resume = new ResumeRecord { ResumeId = 1, UserId = 2 };
            _resumeRepoMock.Setup(r => r.FindByResumeIdAsync(1)).ReturnsAsync(resume);
            Assert.ThrowsAsync<UnauthorizedAccessException>(() => _resumeService.UpdateResumeAsync(1, 1, SubscriptionPlan.FREE, new UpdateResumeRequest("T", "J", 1, "L", ResumeStatus.DRAFT)));
        }

        [Test]
        public async Task DuplicateResumeAsync_ShouldCreateCopy_WhenPublic()
        {
            var original = new ResumeRecord { ResumeId = 1, UserId = 2, IsPublic = true, Sections = new List<ResumeSection>() };
            _resumeRepoMock.Setup(r => r.FindWithSectionsAsync(1)).ReturnsAsync(original);
            _resumeRepoMock.Setup(r => r.AddAsync(It.IsAny<ResumeRecord>())).ReturnsAsync(new ResumeRecord { ResumeId = 2, Title = "Copy" });

            var result = await _resumeService.DuplicateResumeAsync(1, 1);
            Assert.That(result.ResumeId, Is.EqualTo(2));
        }

        [Test]
        public async Task PublishResumeAsync_ShouldSetIsPublicTrue()
        {
            var resume = new ResumeRecord { ResumeId = 1, UserId = 1, IsPublic = false };
            _resumeRepoMock.Setup(r => r.FindByResumeIdAsync(1)).ReturnsAsync(resume);
            _resumeRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ResumeRecord>())).ReturnsAsync(resume);

            await _resumeService.PublishResumeAsync(1, 1);
            Assert.That(resume.IsPublic, Is.True);
        }

        [Test]
        public async Task UnpublishResumeAsync_ShouldSetIsPublicFalse()
        {
            var resume = new ResumeRecord { ResumeId = 1, UserId = 1, IsPublic = true };
            _resumeRepoMock.Setup(r => r.FindByResumeIdAsync(1)).ReturnsAsync(resume);
            _resumeRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ResumeRecord>())).ReturnsAsync(resume);

            await _resumeService.UnpublishResumeAsync(1, 1);
            Assert.That(resume.IsPublic, Is.False);
        }

        [Test]
        public async Task GetResumesByUserAsync_ShouldReturnList()
        {
            _resumeRepoMock.Setup(r => r.FindByUserIdAsync(1)).ReturnsAsync(new List<ResumeRecord> { new ResumeRecord { ResumeId = 1 } });
            var result = await _resumeService.GetResumesByUserAsync(1);
            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task ChangeTemplateAsync_ShouldUpdateTemplateId()
        {
            var resume = new ResumeRecord { ResumeId = 1, UserId = 1, TemplateId = 1 };
            _resumeRepoMock.Setup(r => r.FindByResumeIdAsync(1)).ReturnsAsync(resume);
            await _resumeService.ChangeTemplateAsync(1, 1, 5);
            Assert.That(resume.TemplateId, Is.EqualTo(5));
        }
    }
}
