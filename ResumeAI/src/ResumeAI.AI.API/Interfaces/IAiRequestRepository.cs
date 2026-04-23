using ResumeAI.AI.API.Entities;
using ResumeAI.Shared.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ResumeAI.AI.API.Interfaces;

public interface IAiRequestRepository
{
    Task<IList<AiRequest>> FindByUserIdAsync(int userId);
    Task<IList<AiRequest>> FindByResumeIdAsync(int resumeId);
    Task<AiRequest?> FindByRequestIdAsync(string requestId);
    Task<IList<AiRequest>> FindByRequestTypeAsync(AiRequestType type);
    Task<IList<AiRequest>> FindByStatusAsync(AiRequestStatus status);
    Task<int> CountByUserIdTodayAsync(int userId);
    Task<long> SumTokensByUserIdAsync(int userId);
    Task<AiRequest> AddAsync(AiRequest request);
    Task<AiRequest> UpdateAsync(AiRequest request);
}
