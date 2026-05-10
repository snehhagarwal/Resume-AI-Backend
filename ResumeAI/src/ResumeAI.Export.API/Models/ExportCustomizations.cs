namespace ResumeAI.Export.API.Models;

public class ExportCustomizations
{
    public string PrimaryColor { get; set; } = "#2563EB"; // Default Blue
    public string AccentColor { get; set; } = "#4B5563";  // Default Grey
    public int FontSize { get; set; } = 11;
    public string FontFamily { get; set; } = "Verdana";
    public bool ShowPageNumbers { get; set; } = true;
}
