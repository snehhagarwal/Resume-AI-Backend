using Moq;
using NUnit.Framework;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ResumeAI.JobMatch.API.Entities;
using ResumeAI.JobMatch.API.Interfaces;
using ResumeAI.JobMatch.API.Services;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;
using System.Security.Claims;

namespace ResumeAI.JobMatch.Tests
{
    [TestFixture]
    public class JobMatchServiceTests
    {
        private Mock<IJobMatchRepository> _matchRepoMock;
        private Mock<IAiServiceClient> _aiClientMock;
        private Mock<IJobSearchClient> _jobSearchMock;
        private Mock<INotificationPublisher> _notifMock;
        private Mock<IHttpContextAccessor> _httpContextMock;
        private Mock<ILogger<JobMatchService>> _loggerMock;
        private JobMatchService _jobMatchService;

        [SetUp]
        public void Setup()
        {
            _matchRepoMock = new Mock<IJobMatchRepository>();
            _aiClientMock = new Mock<IAiServiceClient>();
            _jobSearchMock = new Mock<IJobSearchClient>();
            _notifMock = new Mock<INotificationPublisher>();
            _httpContextMock = new Mock<IHttpContextAccessor>();
            _loggerMock = new Mock<ILogger<JobMatchService>>();

            var context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("email", "test@test.com") }));
            _httpContextMock.Setup(h => h.HttpContext).Returns(context);

            _jobMatchService = new JobMatchService(_matchRepoMock.Object, _aiClientMock.Object, _jobSearchMock.Object, _notifMock.Object, _httpContextMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task AnalyzeJobFit_ShouldCreateMatchEntry()
        {
            _aiClientMock.Setup(c => c.AnalyzeJobFit(1, "Desc")).ReturnsAsync(new JobMatchAiResponse(85, "C#, SQL", "Recs"));
            _matchRepoMock.Setup(r => r.Add(It.IsAny<ResumeAI.JobMatch.API.Entities.JobMatch>())).ReturnsAsync(new ResumeAI.JobMatch.API.Entities.JobMatch { MatchId = 1, MatchScore = 85 });

            var result = await _jobMatchService.AnalyzeJobFit(1, new AnalyzeJobFitRequest(1, "Dev", "Desc", "Google", "Remote", JobMatchSource.MANUAL));
            Assert.That(result.MatchScore, Is.EqualTo(85));
        }

        [Test]
        public async Task GetMatchesByResume_ShouldReturnList()
        {
            _matchRepoMock.Setup(r => r.FindByResumeId(1)).ReturnsAsync(new List<ResumeAI.JobMatch.API.Entities.JobMatch> { new ResumeAI.JobMatch.API.Entities.JobMatch { MatchId = 1 } });
            var result = await _jobMatchService.GetMatchesByResume(1);
            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetMatchesByUser_ShouldReturnList()
        {
            _matchRepoMock.Setup(r => r.FindByUserId(1)).ReturnsAsync(new List<ResumeAI.JobMatch.API.Entities.JobMatch> { new ResumeAI.JobMatch.API.Entities.JobMatch { MatchId = 1 } });
            var result = await _jobMatchService.GetMatchesByUser(1);
            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetMatchById_ShouldReturnDto_WhenFound()
        {
            _matchRepoMock.Setup(r => r.FindByMatchId(1)).ReturnsAsync(new ResumeAI.JobMatch.API.Entities.JobMatch { MatchId = 1, JobTitle = "Found" });
            var result = await _jobMatchService.GetMatchById(1);
            Assert.That(result.JobTitle, Is.EqualTo("Found"));
        }

        [Test]
        public async Task GetTopMatches_ShouldFilterByScoreAndUser()
        {
            var matches = new List<ResumeAI.JobMatch.API.Entities.JobMatch> { new ResumeAI.JobMatch.API.Entities.JobMatch { MatchId = 1, UserId = 1, MatchScore = 90 } };
            _matchRepoMock.Setup(r => r.FindByMatchScoreGreaterThan(70)).ReturnsAsync(matches);
            var result = await _jobMatchService.GetTopMatches(1, 70);
            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task BookmarkMatch_ShouldCallRepo()
        {
            await _jobMatchService.BookmarkMatch(1, true);
            _matchRepoMock.Verify(r => r.BookmarkMatch(1, true), Times.Once);
        }

        [Test]
        public async Task FetchJobsFromLinkedIn_ShouldCallSearchClient()
        {
            _jobSearchMock.Setup(c => c.SearchJobs(1, 1, "C#")).ReturnsAsync(new List<JobMatchDto> { new JobMatchDto(1, 1, 1, "T", "D", "C", "L", 0, "", "", JobMatchSource.LINKEDIN, DateTime.UtcNow, false) });
            var result = await _jobMatchService.FetchJobsFromLinkedIn(1, 1, "C#");
            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetTailoringRecommendations_ShouldReturnString()
        {
            _matchRepoMock.Setup(r => r.FindByMatchId(1)).ReturnsAsync(new ResumeAI.JobMatch.API.Entities.JobMatch { Recommendations = "Recs" });
            var result = await _jobMatchService.GetTailoringRecommendations(1);
            Assert.That(result, Is.EqualTo("Recs"));
        }

        [Test]
        public async Task DeleteMatch_ShouldCallRepo()
        {
            await _jobMatchService.DeleteMatch(1);
            _matchRepoMock.Verify(r => r.DeleteByMatchId(1), Times.Once);
        }

        [Test]
        public async Task AnalyzeJobFit_ShouldUseDefaultValues_WhenAiFails()
        {
            _aiClientMock.Setup(c => c.AnalyzeJobFit(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync((JobMatchAiResponse)null);
            _matchRepoMock.Setup(r => r.Add(It.IsAny<ResumeAI.JobMatch.API.Entities.JobMatch>())).ReturnsAsync(new ResumeAI.JobMatch.API.Entities.JobMatch { MatchId = 1 });
            var result = await _jobMatchService.AnalyzeJobFit(1, new AnalyzeJobFitRequest(1, "T", "D", "C", "L", JobMatchSource.MANUAL));
            Assert.That(result.MatchScore, Is.EqualTo(0));
        }
    }
}
