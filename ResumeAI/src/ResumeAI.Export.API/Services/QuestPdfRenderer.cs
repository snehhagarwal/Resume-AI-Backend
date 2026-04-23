using PuppeteerSharp;
using PuppeteerSharp.Media;
using ResumeAI.Export.API.Interfaces;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ResumeAI.Export.API.Services;

public class QuestPdfRenderer(ILogger<QuestPdfRenderer> logger) : IPdfRenderer
{
    public byte[] GeneratePdf(ExportData data)
    {
        return Task.Run(() => GeneratePdfAsync(data)).GetAwaiter().GetResult();
    }

    private async Task<byte[]> GeneratePdfAsync(ExportData data)
    {
        logger.LogInformation("--- PDF GENERATION START ---");
        logger.LogInformation("Resume: {ResumeId}, Template: {TemplateId} ({TemplateName})", 
            data.Resume.ResumeId, data.Template.TemplateId, data.Template.Name);

        var template = data.Template;
        var html = template.HtmlLayout;
        var css = template.CssStyles;

        // DEBUG: Check if data is actually coming from the DB
        if (string.IsNullOrWhiteSpace(css)) 
            logger.LogWarning("⚠️ WARNING: CSS content is EMPTY for this template!");
        else
            logger.LogInformation("CSS Loaded. Length: {Len} chars. Preview: {Preview}...", css.Length, css.Substring(0, Math.Min(50, css.Length)));

        // 1. Data Merging (Handlebar replacement)
        html = html.Replace("{{FullName}}", data.User.FullName);
        html = html.Replace("{{Email}}", data.User.Email);
        html = html.Replace("{{Phone}}", data.User.Phone ?? "");
        html = html.Replace("{{TargetJobTitle}}", data.Resume.TargetJobTitle);

        foreach (var section in data.Sections.Where(s => s.IsVisible).OrderBy(s => s.DisplayOrder))
        {
            var placeholder = $"{{{{{section.SectionType}}}}}";
            html = html.Replace(placeholder, section.Content);
        }

        foreach (SectionType type in Enum.GetValues(typeof(SectionType)))
        {
            html = html.Replace($"{{{{{type}}}}}", "");
        }

        // 2. Prepare HTML Wrapper
        var finalHtml = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <style>
                    body {{ 
                        margin: 0; 
                        padding: 0; 
                        -webkit-print-color-adjust: exact !important; 
                        print-color-adjust: exact !important;
                    }}
                </style>
            </head>
            <body>
                {html}
            </body>
            </html>";

        // 3. Render with Puppeteer
        var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();
        
        using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { 
            Headless = true,
            Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
        });

        using var page = await browser.NewPageAsync();
        
        // CRITICAL: Force 'screen' media so grid/colors aren't stripped by 'print' defaults
        await page.EmulateMediaTypeAsync(MediaType.Screen);
        await page.SetViewportAsync(new ViewPortOptions { Width = 800, Height = 1100 });

        // Set content and then inject CSS separately for better reliability
        await page.SetContentAsync(finalHtml);
        await page.AddStyleTagAsync(new AddTagOptions { Content = css });
        
        // AUTO-CLEANUP: Remove sections that have no content
        await page.EvaluateFunctionAsync(@"() => {
            const sections = document.querySelectorAll('section, .content-block, .neon-section');
            sections.forEach(s => {
                // Look for content divs within the section
                const contentDiv = s.querySelector('.content, .text, .content-block .text');
                if (contentDiv && contentDiv.innerText.trim() === '') {
                    s.remove();
                }
            });
        }");

        // Wait a small moment for any CSS calculations to finish
        await Task.Delay(500);

        logger.LogInformation("Rendering PDF...");

        var pdfBytes = await page.PdfDataAsync(new PdfOptions
        {
            Format = PaperFormat.A4,
            PrintBackground = true,
            MarginOptions = new MarginOptions { Top = "0", Bottom = "0", Left = "0", Right = "0" }
        });

        logger.LogInformation("--- PDF GENERATION COMPLETE ({Size} KB) ---", pdfBytes.Length / 1024);
        return pdfBytes;
    }
}
