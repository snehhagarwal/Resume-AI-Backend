using Microsoft.EntityFrameworkCore;
using ResumeAI.Resume.API.Entities;

namespace ResumeAI.Resume.API.Data;

public class ResumeDbContext(DbContextOptions<ResumeDbContext> options) : DbContext(options)
{
    public DbSet<ResumeRecord> Resumes => Set<ResumeRecord>();
    public DbSet<ResumeSection> ResumeSections => Set<ResumeSection>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<ResumeRecord>(e =>
        {
            e.HasKey(r => r.ResumeId);
            e.Property(r => r.Status).HasConversion<string>();
            e.HasIndex(r => r.UserId);
            e.HasIndex(r => r.IsPublic);

            // One-to-Many Relationship
            e.HasMany(r => r.Sections)
             .WithOne()
             .HasForeignKey(s => s.ResumeId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<ResumeSection>(e =>
        {
            e.HasKey(s => s.SectionId);
            e.Property(s => s.SectionType).HasConversion<string>();
        });
    }
}
