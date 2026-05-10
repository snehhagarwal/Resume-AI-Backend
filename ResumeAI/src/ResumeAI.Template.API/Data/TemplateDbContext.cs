using Microsoft.EntityFrameworkCore;
using ResumeAI.Template.API.Entities;

namespace ResumeAI.Template.API.Data;

public class TemplateDbContext(DbContextOptions<TemplateDbContext> options) : DbContext(options)
{
    public DbSet<ResumeTemplate> ResumeTemplates => Set<ResumeTemplate>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<ResumeTemplate>(e =>
        {
            e.HasKey(t => t.TemplateId);
            e.Property(t => t.Category).HasConversion<string>();
            e.Property(t => t.HtmlLayout).HasColumnType("text");
            e.Property(t => t.CssStyles).HasColumnType("text");
            e.HasIndex(t => t.Category);
            e.HasIndex(t => t.IsPremium);
        });
    }
}
