using ResumeAI.Shared.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ResumeAI.AI.API.Interfaces;

/// <summary>
/// Fetches resume metadata and section content from the Resume and Section
/// internal microservices so that AI prompts are grounded in real resume data.
/// </summary>
public interface IResumeContextClient
{
    /// <summary>Fetch resume metadata (title, target job, language, …).</summary>
    Task<ResumeDto?> GetResumeAsync(int resumeId);

    /// <summary>Fetch all visible sections for a resume, ordered by display order.</summary>
    Task<IList<SectionDto>> GetSectionsAsync(int resumeId);

    /// <summary>Fetch a single section by its ID.</summary>
    Task<SectionDto?> GetSectionAsync(int sectionId);

    /// <summary>
    /// Build a structured plain-text snapshot of the full resume, suitable
    /// for inclusion in an AI prompt.
    /// Returns an empty string if neither the resume nor its sections can
    /// be fetched (callers decide how to handle that).
    /// </summary>
    Task<string> BuildResumeContextAsync(int resumeId);
}
