using ResumeAI.Shared.DTOs;

namespace ResumeAI.Export.API.Interfaces;

public record ExportData(ResumeDto Resume, UserDto User, IList<SectionDto> Sections);

public interface IPdfRenderer
{
    byte[] GeneratePdf(ExportData data);
}
