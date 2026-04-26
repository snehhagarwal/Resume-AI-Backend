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
        public async Task GetTemplateByIdAsync_ShouldReturnDto()
        {
            _templateRepoMock.Setup(r => r.FindByTemplateIdAsync(1)).ReturnsAsync(new ResumeTemplate { TemplateId = 1, Name = "T1" });
            var result = await _templateService.GetTemplateByIdAsync(1);
            Assert.That(result.Name, Is.EqualTo("T1"));
        }

        [Test]
        public async Task GetFreeTemplatesAsync_ShouldFilterByIsPremiumFalse()
        {
            _templateRepoMock.Setup(r => r.FindByIsPremiumAsync(false)).ReturnsAsync(new List<ResumeTemplate> { new ResumeTemplate { IsPremium = false } });
            var result = await _templateService.GetFreeTemplatesAsync();
            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetByCategoryAsync_ShouldFilterByCategory()
        {
            _templateRepoMock.Setup(r => r.FindByCategoryAsync(TemplateCategory.CREATIVE)).ReturnsAsync(new List<ResumeTemplate> { new ResumeTemplate { Category = TemplateCategory.CREATIVE } });
            var result = await _templateService.GetByCategoryAsync(TemplateCategory.CREATIVE);
            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task UpdateTemplateAsync_ShouldModifyExisting()
        {
            var template = new ResumeTemplate { TemplateId = 1, Name = "Old" };
            _templateRepoMock.Setup(r => r.FindByTemplateIdAsync(1)).ReturnsAsync(template);
            _templateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ResumeTemplate>())).ReturnsAsync(template);

            var result = await _templateService.UpdateTemplateAsync(1, new UpdateTemplateRequest("New", "D", "U", "H", "C", TemplateCategory.PROFESSIONAL, true));
            Assert.That(result.Name, Is.EqualTo("New"));
        }

        [Test]
        public async Task CanUserAccessTemplateAsync_ShouldReturnTrue_ForFreeTemplate()
        {
            _templateRepoMock.Setup(r => r.FindByTemplateIdAsync(1)).ReturnsAsync(new ResumeTemplate { IsPremium = false });
            var result = await _templateService.CanUserAccessTemplateAsync(1, SubscriptionPlan.FREE);
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CanUserAccessTemplateAsync_ShouldReturnFalse_ForPremiumTemplateWithFreePlan()
        {
            _templateRepoMock.Setup(r => r.FindByTemplateIdAsync(1)).ReturnsAsync(new ResumeTemplate { IsPremium = true });
            var result = await _templateService.CanUserAccessTemplateAsync(1, SubscriptionPlan.FREE);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ValidateTemplateLayoutAsync_ShouldFail_IfNoPlaceholder()
        {
            var (valid, error) = await _templateService.ValidateTemplateLayoutAsync("<div>No Placeholder</div>", ".css");
            Assert.That(valid, Is.False);
        }

        [Test]
        public async Task ValidateTemplateLayoutAsync_ShouldFail_IfScriptPresent()
        {
            var (valid, error) = await _templateService.ValidateTemplateLayoutAsync("<div>{{Name}}</div><script></script>", ".css");
            Assert.That(valid, Is.False);
        }

        [Test]
        public async Task DeactivateTemplateAsync_ShouldSetIsActiveToFalse()
        {
            var template = new ResumeTemplate { TemplateId = 1, IsActive = true };
            _templateRepoMock.Setup(r => r.FindByTemplateIdAsync(1)).ReturnsAsync(template);
            await _templateService.DeactivateTemplateAsync(1);
            Assert.That(template.IsActive, Is.False);
        }
    }
}
