using Microsoft.EntityFrameworkCore;
using ResumeAI.JobMatch.API.Entities;

namespace ResumeAI.JobMatch.API.Data;

public class JobMatchDbContext(DbContextOptions<JobMatchDbContext> options) : DbContext(options)
{
    public DbSet<ResumeAI.JobMatch.API.Entities.JobMatch> JobMatches => Set<ResumeAI.JobMatch.API.Entities.JobMatch>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<ResumeAI.JobMatch.API.Entities.JobMatch>(e =>
        {
            e.HasKey(m => m.MatchId);
            e.Property(m => m.Source).HasConversion<string>();
            e.HasIndex(m => m.UserId);
            e.HasIndex(m => m.ResumeId);
            e.HasIndex(m => m.MatchScore);
        });
    }
}
