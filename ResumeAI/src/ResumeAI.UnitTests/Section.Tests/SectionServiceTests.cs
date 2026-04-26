using Moq;
using NUnit.Framework;
using ResumeAI.Section.API.Entities;
using ResumeAI.Section.API.Interfaces;
using ResumeAI.Section.API.Repositories;
using ResumeAI.Section.API.Services;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Section.Tests
{
    [TestFixture]
    public class SectionServiceTests
    {
        private Mock<ISectionRepository> _sectionRepoMock;
        private SectionService _sectionService;

        [SetUp]
        public void Setup()
        {
            _sectionRepoMock = new Mock<ISectionRepository>();
            _sectionService = new SectionService(_sectionRepoMock.Object);
        }

        [Test]
        public async Task AddSectionAsync_ShouldCreateSection()
        {
            var request = new AddSectionRequest(1, SectionType.EXPERIENCE, "Work Exp", "Content", 1, true);
            _sectionRepoMock.Setup(r => r.AddAsync(It.IsAny<ResumeSection>()))
                            .ReturnsAsync(new ResumeSection { SectionId = 1, ResumeId = 1 });

            var result = await _sectionService.AddSectionAsync(1, request);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.SectionId, Is.EqualTo(1));
        }

        [Test]
        public async Task GetSectionsByResumeAsync_ShouldReturnOrderedSections()
        {
            _sectionRepoMock.Setup(r => r.FindByResumeIdOrderByDisplayOrderAsync(1, 1))
                            .ReturnsAsync(new List<ResumeSection> { new ResumeSection { SectionId = 1, DisplayOrder = 0 } });
            var result = await _sectionService.GetSectionsByResumeAsync(1, 1);
            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task UpdateSectionAsync_ShouldSucceed_WhenOwnerUpdates()
        {
            var section = new ResumeSection { SectionId = 1, UserId = 1, Title = "Old" };
            _sectionRepoMock.Setup(r => r.FindBySectionIdAsync(1)).ReturnsAsync(section);
            _sectionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ResumeSection>())).ReturnsAsync(section);

            var result = await _sectionService.UpdateSectionAsync(1, 1, new UpdateSectionRequest("New", "Content", 1, true, false));
            Assert.That(result.Title, Is.EqualTo("New"));
        }

        [Test]
        public void UpdateSectionAsync_ShouldThrowException_WhenNotOwner()
        {
            var section = new ResumeSection { SectionId = 1, UserId = 2 };
            _sectionRepoMock.Setup(r => r.FindBySectionIdAsync(1)).ReturnsAsync(section);
            Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sectionService.UpdateSectionAsync(1, 1, new UpdateSectionRequest("T", "C", 1, true, false)));
        }

        [Test]
        public async Task ToggleVisibilityAsync_ShouldFlipValue()
        {
            var section = new ResumeSection { SectionId = 1, UserId = 1, IsVisible = true };
            _sectionRepoMock.Setup(r => r.FindBySectionIdAsync(1)).ReturnsAsync(section);
            _sectionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ResumeSection>())).ReturnsAsync(section);

            var result = await _sectionService.ToggleVisibilityAsync(1, 1);
            Assert.That(result.IsVisible, Is.False);
        }

        [Test]
        public async Task ReorderSectionsAsync_ShouldCallRepoMultipleTimes()
        {
            await _sectionService.ReorderSectionsAsync(1, 1, new ReorderSectionsRequest(new List<int> { 10, 20, 30 }));
            _sectionRepoMock.Verify(r => r.UpdateDisplayOrderAsync(It.IsAny<int>(), 1, It.IsAny<int>()), Times.Exactly(3));
        }

        [Test]
        public async Task BulkUpdateSectionsAsync_ShouldUpdateOnlyValidSections()
        {
            var s1 = new ResumeSection { SectionId = 1, UserId = 1 };
            _sectionRepoMock.Setup(r => r.FindBySectionIdAsync(1)).ReturnsAsync(s1);
            var result = await _sectionService.BulkUpdateSectionsAsync(1, new BulkUpdateSectionsRequest(new List<UpdateSectionItem> { new UpdateSectionItem(1, "T1", "C1", 0, true, true) }));
            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task CopySectionAsync_ShouldCreateNewEntry()
        {
            var original = new ResumeSection { SectionId = 1, UserId = 1, Title = "Original" };
            _sectionRepoMock.Setup(r => r.FindBySectionIdAsync(1)).ReturnsAsync(original);
            _sectionRepoMock.Setup(r => r.AddAsync(It.IsAny<ResumeSection>())).ReturnsAsync(new ResumeSection { SectionId = 2, Title = "Original" });

            var result = await _sectionService.CopySectionAsync(1, 1, 2);
            Assert.That(result.SectionId, Is.EqualTo(2));
        }

        [Test]
        public async Task CountSectionsByResumeAsync_ShouldReturnCount()
        {
            _sectionRepoMock.Setup(r => r.CountByResumeIdAsync(1, 1)).ReturnsAsync(5);
            var result = await _sectionService.CountSectionsByResumeAsync(1, 1);
            Assert.That(result, Is.EqualTo(5));
        }

        [Test]
        public async Task GetSectionsByTypeAsync_ShouldReturnFilteredList()
        {
            _sectionRepoMock.Setup(r => r.FindByResumeIdAndSectionTypeAsync(1, SectionType.SKILLS, 1)).ReturnsAsync(new List<ResumeSection> { new ResumeSection { SectionType = SectionType.SKILLS } });
            var result = await _sectionService.GetSectionsByTypeAsync(1, SectionType.SKILLS, 1);
            Assert.That(result.Count, Is.EqualTo(1));
        }
    }
}
