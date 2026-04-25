using Microsoft.EntityFrameworkCore;
using ResumeAI.Notification.API.Entities;

namespace ResumeAI.Notification.API.Data;

public class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<NotificationRecord> Notifications => Set<NotificationRecord>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<NotificationRecord>(e =>
        {
            e.HasKey(n => n.NotificationId);
            e.Property(n => n.Type).HasConversion<string>();
            e.Property(n => n.Channel).HasConversion<string>();
            e.HasIndex(n => n.RecipientId);
            e.HasIndex(n => n.IsRead);
        });
    }
}
