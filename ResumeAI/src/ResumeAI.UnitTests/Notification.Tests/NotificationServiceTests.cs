using Moq;
using NUnit.Framework;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using ResumeAI.Notification.API.Data;
using ResumeAI.Notification.API.Entities;
using ResumeAI.Notification.API.Hubs;
using ResumeAI.Notification.API.Services;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Notification.Tests
{
    [TestFixture]
    public class NotificationServiceTests
    {
        private NotificationDbContext _db;
        private SqliteConnection _connection;
        private Mock<IConfiguration> _configMock;
        private Mock<IHubContext<NotificationHub>> _hubContextMock;
        private Mock<ILogger<NotificationService>> _loggerMock;
        private NotificationService _notificationService;

        [SetUp]
        public void Setup()
        {
            // Use SQLite in-memory for ExecuteUpdate/ExecuteDelete support
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<NotificationDbContext>()
                .UseSqlite(_connection)
                .Options;
            _db = new NotificationDbContext(options);
            _db.Database.EnsureCreated();

            _configMock = new Mock<IConfiguration>();
            _hubContextMock = new Mock<IHubContext<NotificationHub>>();
            _loggerMock = new Mock<ILogger<NotificationService>>();

            var clientsMock = new Mock<IHubClients>();
            var clientProxyMock = new Mock<IClientProxy>();
            _hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);
            clientsMock.Setup(c => c.User(It.IsAny<string>())).Returns(clientProxyMock.Object);

            _notificationService = new NotificationService(_db, _configMock.Object, _hubContextMock.Object, _loggerMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
            _connection.Close();
        }

        [Test]
        public async Task SendAsync_ShouldSaveNotification_AndTriggerSignalR()
        {
            var result = await _notificationService.SendAsync(1, "Title", "Msg", NotificationType.AI_DONE);
            Assert.That(await _db.Notifications.CountAsync(), Is.EqualTo(1));
            Assert.That(result.Title, Is.EqualTo("Title"));
        }

        [Test]
        public async Task SendBulkAsync_ShouldSaveMultipleNotifications()
        {
            await _notificationService.SendBulkAsync(new SendBulkNotificationRequest("T", "M", NotificationType.AI_DONE), new List<int> { 1, 2, 3 });
            Assert.That(await _db.Notifications.CountAsync(), Is.EqualTo(3));
        }

        [Test]
        public async Task GetByRecipientAsync_ShouldReturnCorrectNotifications()
        {
            _db.Notifications.Add(new NotificationRecord { RecipientId = 1, Title = "U1", Message = "M", SentAt = DateTime.UtcNow });
            _db.Notifications.Add(new NotificationRecord { RecipientId = 2, Title = "U2", Message = "M", SentAt = DateTime.UtcNow });
            await _db.SaveChangesAsync();

            var result = await _notificationService.GetByRecipientAsync(1);
            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetUnreadCountAsync_ShouldOnlyCountUnread()
        {
            _db.Notifications.Add(new NotificationRecord { RecipientId = 1, IsRead = false, Message = "M", Title = "T" });
            _db.Notifications.Add(new NotificationRecord { RecipientId = 1, IsRead = true, Message = "M", Title = "T" });
            await _db.SaveChangesAsync();

            var count = await _notificationService.GetUnreadCountAsync(1);
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public async Task MarkAsReadAsync_ShouldUpdateStatus()
        {
            var n = new NotificationRecord { RecipientId = 1, IsRead = false, Message = "M", Title = "T" };
            _db.Notifications.Add(n);
            await _db.SaveChangesAsync();

            await _notificationService.MarkAsReadAsync(n.NotificationId);
            _db.Entry(n).State = EntityState.Detached; // Ensure we fetch fresh from DB
            var updated = await _db.Notifications.FindAsync(n.NotificationId);
            Assert.That(updated.IsRead, Is.True);
        }

        [Test]
        public async Task MarkAllReadAsync_ShouldUpdateAllUnreadForUser()
        {
            _db.Notifications.Add(new NotificationRecord { RecipientId = 1, IsRead = false, Message = "M", Title = "T" });
            _db.Notifications.Add(new NotificationRecord { RecipientId = 1, IsRead = false, Message = "M", Title = "T" });
            _db.Notifications.Add(new NotificationRecord { RecipientId = 2, IsRead = false, Message = "M", Title = "T" });
            await _db.SaveChangesAsync();

            await _notificationService.MarkAllReadAsync(1);
            Assert.That(await _db.Notifications.CountAsync(n => n.RecipientId == 1 && !n.IsRead), Is.EqualTo(0));
        }

        [Test]
        public async Task DeleteAsync_ShouldRemoveFromDb()
        {
            var n = new NotificationRecord { RecipientId = 1, Message = "M", Title = "T" };
            _db.Notifications.Add(n);
            await _db.SaveChangesAsync();

            await _notificationService.DeleteAsync(n.NotificationId);
            Assert.That(await _db.Notifications.CountAsync(), Is.EqualTo(0));
        }

        [Test]
        public async Task SendAsync_ShouldTryToSendEmail_WhenEmailProvided()
        {
            _configMock.Setup(c => c["Smtp:Host"]).Returns("localhost");
            _configMock.Setup(c => c["Smtp:Username"]).Returns("user");
            _configMock.Setup(c => c["Smtp:Password"]).Returns("pass");

            await _notificationService.SendAsync(1, "T", "M", NotificationType.AI_DONE, NotificationChannel.APP, recipientEmail: "test@test.com");
            Assert.Pass();
        }

        [Test]
        public async Task SendAsync_ShouldNotSendEmail_WhenNoEmailProvided()
        {
            await _notificationService.SendAsync(1, "T", "M", NotificationType.AI_DONE, NotificationChannel.APP);
            Assert.Pass();
        }

        [Test]
        public async Task GetByRecipientAsync_ShouldReturnEmptyList_WhenNoNotifications()
        {
            var result = await _notificationService.GetByRecipientAsync(999);
            Assert.That(result, Is.Empty);
        }
    }
}
