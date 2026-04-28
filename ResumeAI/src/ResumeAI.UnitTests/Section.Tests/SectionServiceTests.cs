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
        public async Task UpdateSectionAsync_ShouldSucceed_WhenOwnerUpdates()
        {
            var section = new ResumeSection { SectionId = 1, UserId = 1, Title = "Old" };
            _sectionRepoMock.Setup(r => r.FindBySectionIdAsync(1)).ReturnsAsync(section);
            _sectionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ResumeSection>())).ReturnsAsync(section);

            var result = await _sectionService.UpdateSectionAsync(1, 1, new UpdateSectionRequest("New", "Content", 1, true, false));
            Assert.That(result.Title, Is.EqualTo("New"));
        }

        [Test]
        public async Task ReorderSectionsAsync_ShouldCallRepoMultipleTimes()
        {
            await _sectionService.ReorderSectionsAsync(1, 1, new ReorderSectionsRequest(new List<int> { 10, 20, 30 }));
            _sectionRepoMock.Verify(r => r.UpdateDisplayOrderAsync(It.IsAny<int>(), 1, It.IsAny<int>()), Times.Exactly(3));
        }
    }
}
