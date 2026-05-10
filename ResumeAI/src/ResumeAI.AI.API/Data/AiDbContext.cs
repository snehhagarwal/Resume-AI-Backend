using Microsoft.EntityFrameworkCore;
using ResumeAI.AI.API.Entities;

namespace ResumeAI.AI.API.Data;

public class AiDbContext(DbContextOptions<AiDbContext> options) : DbContext(options)
{
    public DbSet<AiRequest> AiRequests => Set<AiRequest>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<AiRequest>(e =>
        {
            e.HasKey(a => a.RequestId);
            e.Property(a => a.RequestType).HasConversion<string>();
            e.Property(a => a.Model).HasConversion<string>();
            e.Property(a => a.Status).HasConversion<string>();
            e.HasIndex(a => a.UserId);
            e.HasIndex(a => a.ResumeId);
        });
    }
}
