using Microsoft.EntityFrameworkCore;
using ResumeAI.Export.API.Entities;

namespace ResumeAI.Export.API.Data;

public class ExportDbContext(DbContextOptions<ExportDbContext> options) : DbContext(options)
{
    public DbSet<ExportJob> ExportJobs => Set<ExportJob>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<ExportJob>(e =>
        {
            e.HasKey(j => j.JobId);
            e.Property(j => j.Format).HasConversion<string>();
            e.Property(j => j.Status).HasConversion<string>();
            e.HasIndex(j => j.UserId);
            e.HasIndex(j => j.ResumeId);
            e.HasIndex(j => j.ExpiresAt);
        });
    }
}
