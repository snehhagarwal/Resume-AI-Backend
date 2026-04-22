using ResumeAI.Resume.API.Entities;
using ResumeAI.Resume.API.Interfaces;
using ResumeAI.Resume.API.Repositories;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Resume.API.Services;

public class ResumeService(IResumeRepository resumeRepo) : IResumeService
{
    public async Task<ResumeDto> CreateResumeAsync(int userId, SubscriptionPlan plan, CreateResumeRequest request)
    {
        if (plan == SubscriptionPlan.FREE)
        {
            var count = await resumeRepo.CountByUserIdAsync(userId);
            if (count >= 3)
                throw new InvalidOperationException("Free tier limit (3 resumes) reached. Upgrade to Premium for unlimited resumes.");
        }

        var resume = new Resume.API.Entities.ResumeRecord
        {
            UserId = userId,
            Title = request.Title,
            TargetJobTitle = request.TargetJobTitle,
            TemplateId = request.TemplateId,
            Language = request.Language
        };
        var saved = await resumeRepo.AddAsync(resume);
        return MapToDto(saved);
    }

    public async Task<ResumeDto?> GetResumeByIdAsync(int resumeId)
    {
        var resume = await resumeRepo.FindByResumeIdAsync(resumeId);
        return resume is null ? null : MapToDto(resume);
    }

    public async Task<IList<ResumeDto>> GetResumesByUserAsync(int userId)
    {
        var resumes = await resumeRepo.FindByUserIdAsync(userId);
        return resumes.Select(MapToDto).ToList();
    }

    public async Task<ResumeDto> UpdateResumeAsync(int resumeId, int userId, UpdateResumeRequest request)
    {
        var resume = await resumeRepo.FindByResumeIdAsync(resumeId)
                     ?? throw new KeyNotFoundException("Resume not found.");

        if (resume.UserId != userId)
            throw new UnauthorizedAccessException("You do not own this resume.");

        resume.Title = request.Title;
        resume.TargetJobTitle = request.TargetJobTitle;
        resume.TemplateId = request.TemplateId;
        resume.Language = request.Language;
        resume.Status = request.Status;
        var updated = await resumeRepo.UpdateAsync(resume);
        return MapToDto(updated);
    }

    public async Task DeleteResumeAsync(int resumeId, int userId)
    {
        var resume = await resumeRepo.FindByResumeIdAsync(resumeId)
                     ?? throw new KeyNotFoundException("Resume not found.");

        if (resume.UserId != userId)
            throw new UnauthorizedAccessException("You do not own this resume.");

        await resumeRepo.DeleteByResumeIdAsync(resumeId);
    }

    public async Task<ResumeDto> DuplicateResumeAsync(int resumeId, int userId)
    {
        var original = await resumeRepo.FindByResumeIdAsync(resumeId)
                       ?? throw new KeyNotFoundException("Resume not found.");

        if (original.UserId != userId && !original.IsPublic)
            throw new UnauthorizedAccessException("You do not have permission to duplicate this resume.");

        // Deep copy using no-tracking pattern
        var copy = new Resume.API.Entities.ResumeRecord
        {
            UserId = userId,
            Title = $"{original.Title} (Copy)",
            TargetJobTitle = original.TargetJobTitle,
            TemplateId = original.TemplateId,
            Language = original.Language,
            Status = ResumeStatus.DRAFT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var saved = await resumeRepo.AddAsync(copy);
        return MapToDto(saved);
    }

    public Task UpdateAtsScoreAsync(int resumeId, int score)
        => resumeRepo.UpdateAtsScoreAsync(resumeId, score);

    public async Task<ResumeDto> PublishResumeAsync(int resumeId, int userId)
    {
        var resume = await resumeRepo.FindByResumeIdAsync(resumeId)
                     ?? throw new KeyNotFoundException("Resume not found.");

        if (resume.UserId != userId)
            throw new UnauthorizedAccessException("You do not own this resume.");

        resume.IsPublic = true;
        var updated = await resumeRepo.UpdateAsync(resume);
        return MapToDto(updated);
    }

    public async Task<ResumeDto> UnpublishResumeAsync(int resumeId, int userId)
    {
        var resume = await resumeRepo.FindByResumeIdAsync(resumeId)
                     ?? throw new KeyNotFoundException("Resume not found.");

        if (resume.UserId != userId)
            throw new UnauthorizedAccessException("You do not own this resume.");

        resume.IsPublic = false;
        var updated = await resumeRepo.UpdateAsync(resume);
        return MapToDto(updated);
    }

    public async Task<IList<ResumeDto>> GetPublicResumesAsync()
    {
        var resumes = await resumeRepo.FindByIsPublicAsync(true);
        return resumes.Select(MapToDto).ToList();
    }

    public Task IncrementViewCountAsync(int resumeId)
        => resumeRepo.IncrementViewCountAsync(resumeId);

    public async Task<IList<ResumeDto>> GetResumesByTemplateAsync(int templateId)
    {
        var resumes = await resumeRepo.FindByTemplateIdAsync(templateId);
        return resumes.Select(MapToDto).ToList();
    }

    private static ResumeDto MapToDto(Resume.API.Entities.ResumeRecord r) =>
        new(r.ResumeId, r.UserId, r.Title, r.TargetJobTitle,
            r.TemplateId, r.AtsScore, r.Status, r.Language,
            r.IsPublic, r.ViewCount, r.CreatedAt, r.UpdatedAt);
}
