using Moq;
using NUnit.Framework;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;
using ResumeAI.Template.API.Entities;
using ResumeAI.Template.API.Interfaces;
using ResumeAI.Template.API.Repositories;
using ResumeAI.Template.API.Services;

namespace ResumeAI.Template.Tests
{
    [TestFixture]
    public class TemplateServiceTests
    {
        private Mock<ITemplateRepository> _templateRepoMock;
        private TemplateService _templateService;

        [SetUp]
        public void Setup()
        {
            _templateRepoMock = new Mock<ITemplateRepository>();
            _templateService = new TemplateService(_templateRepoMock.Object);
        }

        [Test]
        public async Task CreateTemplateAsync_ShouldSaveToRepo()
        {
            var request = new CreateTemplateRequest("Modern", "Desc", "url", "<div>{{FullName}}</div>", ".css", TemplateCategory.PROFESSIONAL, false);
            _templateRepoMock.Setup(r => r.AddAsync(It.IsAny<ResumeTemplate>())).ReturnsAsync(new ResumeTemplate { TemplateId = 1, Name = "Modern" });

            var result = await _templateService.CreateTemplateAsync(request);
            Assert.That(result.Name, Is.EqualTo("Modern"));
        }

        [Test]
        public async Task CanUserAccessTemplateAsync_ShouldReturnFalse_ForPremiumTemplateWithFreePlan()
        {
            _templateRepoMock.Setup(r => r.FindByTemplateIdAsync(1)).ReturnsAsync(new ResumeTemplate { IsPremium = true });
            var result = await _templateService.CanUserAccessTemplateAsync(1, SubscriptionPlan.FREE);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ValidateTemplateLayoutAsync_ShouldFail_IfScriptPresent()
        {
            var (valid, error) = await _templateService.ValidateTemplateLayoutAsync("<div>{{Name}}</div><script></script>", ".css");
            Assert.That(valid, Is.False);
        }
    }
}
