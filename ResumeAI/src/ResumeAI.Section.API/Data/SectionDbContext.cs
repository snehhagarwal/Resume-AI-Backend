using Microsoft.EntityFrameworkCore;
using ResumeAI.Section.API.Entities;

namespace ResumeAI.Section.API.Data;

public class SectionDbContext(DbContextOptions<SectionDbContext> options) : DbContext(options)
{
    public DbSet<ResumeSection> ResumeSections => Set<ResumeSection>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<ResumeSection>(e =>
        {
            e.HasKey(s => s.SectionId);
            e.Property(s => s.SectionType).HasConversion<string>();
            e.HasIndex(s => s.ResumeId);
            e.HasIndex(s => new { s.ResumeId, s.DisplayOrder });
        });
    }
}
