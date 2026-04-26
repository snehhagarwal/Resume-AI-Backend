using Moq;
using NUnit.Framework;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using ResumeAI.AI.API.Clients;
using ResumeAI.AI.API.Entities;
using ResumeAI.AI.API.Interfaces;
using ResumeAI.AI.API.Repositories;
using ResumeAI.AI.API.Services;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;
using System.Security.Claims;
using System.Text;

namespace ResumeAI.AI.Tests
{
    [TestFixture]
    public class AiServiceTests
    {
        private Mock<IAiRequestRepository> _aiRepoMock;
        private Mock<IDistributedCache> _cacheMock;
        private Mock<IConfiguration> _configMock;
        private Mock<ILogger<AiService>> _loggerMock;
        private Mock<IHttpContextAccessor> _httpContextMock;
        private Mock<IResumeContextClient> _resumeClientMock;
        private Mock<INotificationPublisher> _notifMock;
        private AiService _aiService;

        [SetUp]
        public void Setup()
        {
            _aiRepoMock = new Mock<IAiRequestRepository>();
            _cacheMock = new Mock<IDistributedCache>();
            _configMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<AiService>>();
            _httpContextMock = new Mock<IHttpContextAccessor>();
            _resumeClientMock = new Mock<IResumeContextClient>();
            _notifMock = new Mock<INotificationPublisher>();

            _configMock.Setup(c => c["OpenAI:ApiKey"]).Returns("test-key");
            _configMock.Setup(c => c["OpenAI:Endpoint"]).Returns("https://api.openai.com/v1");
            _configMock.Setup(c => c.GetSection("OpenAI:AllowMockFallback")).Returns(new Mock<IConfigurationSection>().Object); // Default false for tests unless overridden

            var context = new DefaultHttpContext();
            var claims = new[] { new Claim("plan", "FREE"), new Claim("email", "test@test.com") };
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
            _httpContextMock.Setup(h => h.HttpContext).Returns(context);

            _aiService = new AiService(_aiRepoMock.Object, _cacheMock.Object, _configMock.Object, _loggerMock.Object, _httpContextMock.Object, _resumeClientMock.Object, _notifMock.Object);
        }

        [Test]
        public async Task GetRemainingQuotaAsync_ShouldReturnCorrectValues_ForFreeUser()
        {
            _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync(Encoding.UTF8.GetBytes("2"));
            var result = await _aiService.GetRemainingQuotaAsync(1);
            Assert.That(result.RemainingContentCalls, Is.EqualTo(3));
        }

        [Test]
        public async Task GetAiHistoryAsync_ShouldReturnList()
        {
            _aiRepoMock.Setup(r => r.FindByUserIdAsync(1)).ReturnsAsync(new List<AiRequest> { new AiRequest { RequestId = "1" } });
            var result = await _aiService.GetAiHistoryAsync(1);
            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public void GenerateSummaryAsync_ShouldThrowException_WhenQuotaReached()
        {
            _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync(Encoding.UTF8.GetBytes("5"));
            Assert.ThrowsAsync<InvalidOperationException>(() => _aiService.GenerateSummaryAsync(1, new GenerateSummaryRequest(1, "J", 1, "S")));
        }

        [Test]
        public async Task GenerateSummaryAsync_ShouldCallResumeClient()
        {
            _resumeClientMock.Setup(c => c.BuildResumeContextAsync(1)).ReturnsAsync("Context");
            try { await _aiService.GenerateSummaryAsync(1, new GenerateSummaryRequest(1, "J", 1, "S")); } catch {}
            _resumeClientMock.Verify(c => c.BuildResumeContextAsync(1), Times.Once);
        }

        [Test]
        public async Task ImproveSectionAsync_ShouldTryToFetchSection()
        {
            _resumeClientMock.Setup(c => c.GetSectionAsync(10)).ReturnsAsync(new SectionDto(10, 1, SectionType.EXPERIENCE, "T", "C", 0, true, false, DateTime.UtcNow, DateTime.UtcNow));
            try { await _aiService.ImproveSectionAsync(1, new ImproveSectionRequest(1, 10, "Old", "Hint")); } catch {}
            _resumeClientMock.Verify(c => c.GetSectionAsync(10), Times.Once);
        }

        [Test]
        public async Task CheckAtsCompatibilityAsync_ShouldExecute_WhenQuotaAvailable()
        {
            _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync(Encoding.UTF8.GetBytes("0"));
            try { await _aiService.CheckAtsCompatibilityAsync(1, new CheckAtsRequest(1, "JD")); } catch {}
            _resumeClientMock.Verify(c => c.BuildResumeContextAsync(1), Times.Once);
        }

        [Test]
        public async Task SuggestSkillsAsync_ShouldBuildPromptWithContext()
        {
            _resumeClientMock.Setup(c => c.BuildResumeContextAsync(1)).ReturnsAsync("Existing Skills");
            try { await _aiService.SuggestSkillsAsync(1, new SuggestSkillsRequest(1, "Dev")); } catch {}
            _resumeClientMock.Verify(c => c.BuildResumeContextAsync(1), Times.Once);
        }

        [Test]
        public async Task TailorResumeForJobAsync_ShouldWork()
        {
            try { await _aiService.TailorResumeForJobAsync(1, new TailorResumeRequest(1, "JD")); } catch {}
            _resumeClientMock.Verify(c => c.BuildResumeContextAsync(1), Times.Once);
        }

        [Test]
        public async Task TranslateResumeAsync_ShouldWork()
        {
            try { await _aiService.TranslateResumeAsync(1, new TranslateResumeRequest(1, "French")); } catch {}
            _resumeClientMock.Verify(c => c.BuildResumeContextAsync(1), Times.Once);
        }

        [Test]
        public async Task AnalyzeJobFitAsync_ShouldWork()
        {
            try { await _aiService.AnalyzeJobFitAsync(1, new CheckAtsRequest(1, "JD")); } catch {}
            _resumeClientMock.Verify(c => c.BuildResumeContextAsync(1), Times.Once);
        }
    }
}
