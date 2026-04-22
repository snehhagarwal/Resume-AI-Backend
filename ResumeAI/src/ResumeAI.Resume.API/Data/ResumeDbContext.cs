using Microsoft.EntityFrameworkCore;
using ResumeAI.Resume.API.Entities;

namespace ResumeAI.Resume.API.Data;

public class ResumeDbContext(DbContextOptions<ResumeDbContext> options) : DbContext(options)
{
    public DbSet<ResumeRecord> Resumes => Set<ResumeRecord>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<ResumeRecord>(e =>
        {
            e.HasKey(r => r.ResumeId);
            e.Property(r => r.Status).HasConversion<string>();
            e.HasIndex(r => r.UserId);
            e.HasIndex(r => r.IsPublic);
        });
    }
}
