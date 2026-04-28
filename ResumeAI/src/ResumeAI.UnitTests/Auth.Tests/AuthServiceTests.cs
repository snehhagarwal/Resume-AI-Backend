using Moq;
using NUnit.Framework;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using ResumeAI.Auth.API.Entities;
using ResumeAI.Auth.API.Repositories;
using ResumeAI.Auth.API.Services;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;
using ResumeAI.Auth.API.Interfaces;

namespace ResumeAI.Auth.Tests
{
    [TestFixture]
    public class AuthServiceTests
    {
        private Mock<IUserRepository> _userRepoMock;
        private Mock<IRefreshTokenRepository> _tokenRepoMock;
        private Mock<IConfiguration> _configMock;
        private Mock<IPasswordHasher<User>> _hasherMock;
        private AuthService _authService;

        [SetUp]
        public void Setup()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _tokenRepoMock = new Mock<IRefreshTokenRepository>();
            _configMock = new Mock<IConfiguration>();
            _hasherMock = new Mock<IPasswordHasher<User>>();

            _configMock.Setup(c => c["Jwt:Secret"]).Returns("SuperSecretKeyThatIsAtLeast32CharactersLong!!");
            _configMock.Setup(c => c["Jwt:Issuer"]).Returns("ResumeAI");
            _configMock.Setup(c => c["Jwt:Audience"]).Returns("ResumeAIUsers");

            _authService = new AuthService(
                _userRepoMock.Object,
                _tokenRepoMock.Object,
                _configMock.Object,
                _hasherMock.Object
            );
        }

        [Test]
        public async Task RegisterAsync_ShouldCreateUser_WhenEmailIsNew()
        {
            var request = new RegisterRequest("Test User", "test@example.com", "password123", "1234567890");
            _userRepoMock.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);
            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync(new User { UserId = 1, Email = request.Email });
            _hasherMock.Setup(h => h.HashPassword(It.IsAny<User>(), request.Password)).Returns("hashed_password");

            var result = await _authService.RegisterAsync(request);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.User.Email, Is.EqualTo(request.Email));
        }

        [Test]
        public async Task LoginAsync_ShouldReturnResponse_WhenCredentialsAreValid()
        {
            var user = new User { UserId = 1, Email = "test@example.com", PasswordHash = "hashed", IsActive = true };
            _userRepoMock.Setup(r => r.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _hasherMock.Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, "password123"))
                       .Returns(PasswordVerificationResult.Success);

            var result = await _authService.LoginAsync(new LoginRequest(user.Email, "password123"));

            Assert.That(result, Is.Not.Null);
            Assert.That(result.User.Email, Is.EqualTo(user.Email));
        }

        [Test]
        public async Task OAuthLoginAsync_ShouldCreateNewUser_WhenFirstTime()
        {
            _userRepoMock.Setup(r => r.FindByEmailAsync("google@example.com")).ReturnsAsync((User)null);
            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync(new User { Email = "google@example.com" });

            var result = await _authService.OAuthLoginAsync(AuthProvider.GOOGLE, "google@example.com", "Google User");
            Assert.That(result, Is.Not.Null);
        }
    }
}
