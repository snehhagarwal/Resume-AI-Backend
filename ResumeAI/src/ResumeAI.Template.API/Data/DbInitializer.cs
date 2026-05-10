using Microsoft.EntityFrameworkCore;
using ResumeAI.Template.API.Entities;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Template.API.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(TemplateDbContext context)
    {
        if (await context.ResumeTemplates.AnyAsync())
        {
            return;
        }

        var seedDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SeedTemplates");

        var templates = new List<ResumeTemplate>
        {
            new()
            {
                Name = "The Executive",
                Description = "A classic, high-contrast layout suitable for corporate leadership and management roles.",
                ThumbnailUrl = "https://images.resumeai.com/templates/executive-thumb.png",
                HtmlLayout = await File.ReadAllTextAsync(Path.Combine(seedDir, "executive.html")),
                CssStyles = await File.ReadAllTextAsync(Path.Combine(seedDir, "executive.css")),
                Category = TemplateCategory.PROFESSIONAL,
                IsPremium = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Name = "Swiss Clean",
                Description = "Inspired by Swiss typography, focusing on readability and a grid-based layout.",
                ThumbnailUrl = "https://images.resumeai.com/templates/swiss-thumb.png",
                HtmlLayout = await File.ReadAllTextAsync(Path.Combine(seedDir, "swiss.html")),
                CssStyles = await File.ReadAllTextAsync(Path.Combine(seedDir, "swiss.css")),
                Category = TemplateCategory.MODERN,
                IsPremium = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Name = "Minimalist Mono",
                Description = "A clean, black and white design that emphasizes content over flair.",
                ThumbnailUrl = "https://images.resumeai.com/templates/minimal-thumb.png",
                HtmlLayout = await File.ReadAllTextAsync(Path.Combine(seedDir, "minimal.html")),
                CssStyles = await File.ReadAllTextAsync(Path.Combine(seedDir, "minimal.css")),
                Category = TemplateCategory.MINIMALIST,
                IsPremium = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Name = "Creative Neon",
                Description = "A vibrant design for designers and marketing professionals with bold accents.",
                ThumbnailUrl = "https://images.resumeai.com/templates/neon-thumb.png",
                HtmlLayout = await File.ReadAllTextAsync(Path.Combine(seedDir, "neon.html")),
                CssStyles = await File.ReadAllTextAsync(Path.Combine(seedDir, "neon.css")),
                Category = TemplateCategory.CREATIVE,
                IsPremium = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.ResumeTemplates.AddRange(templates);
        await context.SaveChangesAsync();
    }
}
