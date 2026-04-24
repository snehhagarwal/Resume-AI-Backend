using ResumeAI.Shared.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ResumeAI.AI.API.Interfaces;

public interface IAiService
{
    Task<AiRequestDto> GenerateSummaryAsync(int userId, GenerateSummaryRequest request);
    Task<AiRequestDto> GenerateBulletPointsAsync(int userId, GenerateBulletsRequest request);
    Task<AiRequestDto> GenerateCoverLetterAsync(int userId, GenerateCoverLetterRequest request);
    Task<AiRequestDto> ImproveSectionAsync(int userId, ImproveSectionRequest request);
    Task<AiRequestDto> CheckAtsCompatibilityAsync(int userId, CheckAtsRequest request);
    Task<AiRequestDto> SuggestSkillsAsync(int userId, SuggestSkillsRequest request);
    Task<AiRequestDto> TailorResumeForJobAsync(int userId, TailorResumeRequest request);
    Task<AiRequestDto> TranslateResumeAsync(int userId, TranslateResumeRequest request);
    Task<AiRequestDto> AnalyzeJobFitAsync(int userId, CheckAtsRequest request);
    Task<IList<AiRequestDto>> GetAiHistoryAsync(int userId);
    Task<AiQuotaDto> GetRemainingQuotaAsync(int userId);
}
