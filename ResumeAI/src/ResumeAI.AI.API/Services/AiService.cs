using Ganss.Xss;
using Microsoft.Extensions.Caching.Distributed;
using OpenAI.Chat;
using OpenAI;
using ResumeAI.AI.API.Clients;
using ResumeAI.AI.API.Entities;
using ResumeAI.AI.API.Repositories;
using ResumeAI.AI.API.Interfaces;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;

using ResumeAI.Shared.Enums;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ResumeAI.AI.API.Services;

/// AI Content Service — uses Groq/OpenAI for all AI generation.
/// Quota tracked per-user per-month in Redis IDistributedCache.
///
/// Every operation fetches the real resume content from the Resume and Section
/// microservices (via <see cref="IResumeContextClient"/>) before building the
/// AI prompt, so the model works with the candidate's actual data rather than
/// only the fields provided in the request body.
/// </summary>
public class AiService(
    IAiRequestRepository aiRepo,
    IDistributedCache cache,
    IConfiguration config,
    ILogger<AiService> logger,
    IHttpContextAccessor httpContextAccessor,
    IResumeContextClient resumeContextClient,
    INotificationPublisher notificationPublisher) : IAiService
{
    private SubscriptionPlan CurrentUserPlan =>
        Enum.TryParse<SubscriptionPlan>(httpContextAccessor.HttpContext?.User.FindFirstValue("plan"), true, out var plan)
            ? plan
            : SubscriptionPlan.FREE;
    private const int FreeContentQuota = 5;
    private const int FreeAtsQuota = 3;

    private readonly HtmlSanitizer _sanitizer = new();

    // ─── Public service methods ───────────────────────────────────

    public async Task<AiRequestDto> GenerateSummaryAsync(int userId, GenerateSummaryRequest request)
    {
        var resumeContext = await resumeContextClient.BuildResumeContextAsync(request.ResumeId);
        var contextBlock = WrapContext(resumeContext);

        var prompt =
            $"Write a professional resume summary for a {_sanitizer.Sanitize(request.JobTitle)} " +
            $"with {request.YearsOfExperience} years of experience. " +
            $"Key skills: {_sanitizer.Sanitize(request.KeySkills)}. " +
            "Keep it concise, impactful, and ATS-friendly (3-4 sentences). " +
            "Make it consistent with the candidate's existing resume content below." +
            contextBlock;

        return await ExecuteAiCallAsync(userId, request.ResumeId, AiRequestType.SUMMARY, prompt);
    }

    public async Task<AiRequestDto> GenerateBulletPointsAsync(int userId, GenerateBulletsRequest request)
    {
        var resumeContext = await resumeContextClient.BuildResumeContextAsync(request.ResumeId);
        var contextBlock = WrapContext(resumeContext);

        var prompt =
            $"Generate 4-6 strong resume bullet points for the role of " +
            $"{_sanitizer.Sanitize(request.JobTitle)} at {_sanitizer.Sanitize(request.CompanyName)}. " +
            $"Responsibilities: {_sanitizer.Sanitize(request.Responsibilities)}. " +
            "Use action verbs and quantify achievements where possible. " +
            "Ensure the tone and level of seniority match the candidate's existing resume." +
            contextBlock;

        return await ExecuteAiCallAsync(userId, request.ResumeId, AiRequestType.BULLETS, prompt);
    }

    public async Task<AiRequestDto> GenerateCoverLetterAsync(int userId, GenerateCoverLetterRequest request)
    {
        var resumeContext = await resumeContextClient.BuildResumeContextAsync(request.ResumeId);
        var contextBlock = WrapContext(resumeContext);

        var prompt =
            $"Write a tailored cover letter for {_sanitizer.Sanitize(request.CompanyName)}. " +
            $"Job description: {_sanitizer.Sanitize(request.JobDescription)}. " +
            "Keep it professional, enthusiastic, and under 300 words. " +
            "Ground specific claims (skills, experience, achievements) in the candidate's actual resume below." +
            contextBlock;

        return await ExecuteAiCallAsync(userId, request.ResumeId, AiRequestType.COVER_LETTER, prompt);
    }

    public async Task<AiRequestDto> ImproveSectionAsync(int userId, ImproveSectionRequest request)
    {
        // Fetch the real stored section content from the DB using SectionId — do NOT blindly
        // trust what the client sends in CurrentContent (stale/partial/wrong data).
        // Fall back to CurrentContent only if the Section API is unreachable.
        var sectionTask = resumeContextClient.GetSectionAsync(request.SectionId);
        var contextTask = resumeContextClient.BuildResumeContextAsync(request.ResumeId);
        await Task.WhenAll(sectionTask, contextTask);

        var section = sectionTask.Result;
        var resumeContext = contextTask.Result;

        if (section is null)
            logger.LogWarning(
                "Could not fetch section {SectionId} from Section API — " +
                "falling back to client-provided CurrentContent.",
                request.SectionId);

        var contentToImprove = section?.Content ?? request.CurrentContent;
        var contextBlock = WrapContext(resumeContext);

        var hint = string.IsNullOrEmpty(request.ImprovementHint)
            ? "more impactful and professional"
            : _sanitizer.Sanitize(request.ImprovementHint);

        var prompt =
            $"Rewrite the following resume section to be {hint}:\n\n" +
            _sanitizer.Sanitize(contentToImprove) +
            "\n\nKeep the rewrite consistent with the rest of the candidate's resume below." +
            contextBlock;

        return await ExecuteAiCallAsync(userId, request.ResumeId, AiRequestType.IMPROVE, prompt);
    }

    public async Task<AiRequestDto> CheckAtsCompatibilityAsync(int userId, CheckAtsRequest request)
    {
        var resumeContext = await resumeContextClient.BuildResumeContextAsync(request.ResumeId);

        if (string.IsNullOrWhiteSpace(resumeContext))
            logger.LogWarning(
                "ATS check for resume {ResumeId} has no resume context — analysis will be shallow.",
                request.ResumeId);

        var contextBlock = WrapContext(resumeContext,
            fallback: "[No resume content could be retrieved. Provide general ATS guidance only.]");

        var prompt =
            "Analyse the candidate's resume against the job description below. " +
            "Return a JSON object with exactly these keys: " +
            "score (integer 0-100), missingKeywords (string array), suggestions (string array).\n\n" +
            $"Job Description:\n{_sanitizer.Sanitize(request.JobDescription)}" +
            contextBlock;

        return await ExecuteAiCallAsync(userId, request.ResumeId, AiRequestType.ATS, prompt,
            isAtsCall: true);
    }

    public async Task<AiRequestDto> SuggestSkillsAsync(int userId, SuggestSkillsRequest request)
    {
        var resumeContext = await resumeContextClient.BuildResumeContextAsync(request.ResumeId);
        var contextBlock = WrapContext(resumeContext);

        var prompt =
            $"List the top 15 in-demand technical and soft skills for a " +
            $"{_sanitizer.Sanitize(request.TargetJobTitle)} role in 2025. " +
            "Prioritise skills the candidate is NOT already showcasing in their resume below. " +
            "Return as a comma-separated list." +
            contextBlock;

        return await ExecuteAiCallAsync(userId, request.ResumeId, AiRequestType.SKILLS, prompt);
    }

    public async Task<AiRequestDto> TailorResumeForJobAsync(int userId, TailorResumeRequest request)
    {
        var resumeContext = await resumeContextClient.BuildResumeContextAsync(request.ResumeId);

        if (string.IsNullOrWhiteSpace(resumeContext))
            logger.LogWarning(
                "Tailor-for-job for resume {ResumeId} has no resume context — output may be generic.",
                request.ResumeId);

        var contextBlock = WrapContext(resumeContext,
            fallback: "[No resume content could be retrieved. Suggest improvements conceptually.]");

        var prompt =
            "Tailor the candidate's resume for the job description below. " +
            "Return the complete improved resume as a JSON object whose keys mirror the section titles. " +
            "Preserve all factual information — only adjust phrasing, emphasis and keyword density.\n\n" +
            $"Job Description:\n{_sanitizer.Sanitize(request.JobDescription)}" +
            contextBlock;

        return await ExecuteAiCallAsync(userId, request.ResumeId, AiRequestType.TAILOR, prompt);
    }

    public async Task<AiRequestDto> TranslateResumeAsync(int userId, TranslateResumeRequest request)
    {
        var resumeContext = await resumeContextClient.BuildResumeContextAsync(request.ResumeId);

        if (string.IsNullOrWhiteSpace(resumeContext))
            logger.LogWarning(
                "Translate for resume {ResumeId} has no resume context — nothing to translate.",
                request.ResumeId);

        var contextBlock = WrapContext(resumeContext,
            fallback: "[No resume content could be retrieved. Cannot perform translation.]");

        var prompt =
            $"Translate the candidate's resume to {_sanitizer.Sanitize(request.TargetLanguage)}, " +
            "maintaining a professional tone and standard resume formatting. " +
            "Return the translated resume as a JSON object whose keys mirror the section titles." +
            contextBlock;

        return await ExecuteAiCallAsync(userId, request.ResumeId, AiRequestType.TRANSLATE, prompt);
    }

    public async Task<AiRequestDto> AnalyzeJobFitAsync(int userId, CheckAtsRequest request)
    {
        var resumeContext = await resumeContextClient.BuildResumeContextAsync(request.ResumeId);
        var contextBlock = WrapContext(resumeContext);

        var prompt =
            "Analyze the candidate's resume against the job description below. " +
            "Return a JSON object with exactly these keys: " +
            "matchScore (integer 0-100), missingSkills (comma-separated string), recommendations (string).\n\n" +
            $"Job Description:\n{_sanitizer.Sanitize(request.JobDescription)}" +
            contextBlock;

        return await ExecuteAiCallAsync(userId, request.ResumeId, AiRequestType.JOB_MATCH, prompt, isAtsCall: true);
    }

    public async Task<IList<AiRequestDto>> GetAiHistoryAsync(int userId)
    {
        var requests = await aiRepo.FindByUserIdAsync(userId);
        return requests.Select(MapToDto).ToList();
    }

    public async Task<AiQuotaDto> GetRemainingQuotaAsync(int userId)
    {
        var contentUsed = await GetQuotaCounterAsync(userId, "content");
        var atsUsed = await GetQuotaCounterAsync(userId, "ats");
        return new AiQuotaDto(
            RemainingContentCalls: Math.Max(0, FreeContentQuota - contentUsed),
            RemainingAtsCalls: Math.Max(0, FreeAtsQuota - atsUsed),
            MaxContentCalls: FreeContentQuota,
            MaxAtsCalls: FreeAtsQuota);
    }

    // ─── Core AI execution ────────────────────────────────────────

    private async Task<AiRequestDto> ExecuteAiCallAsync(
        int userId, int resumeId, AiRequestType type, string prompt, bool isAtsCall = false)
    {
        var quotaKey = isAtsCall ? "ats" : "content";
        var limit = isAtsCall ? FreeAtsQuota : FreeContentQuota;

        // ENFORCE QUOTA: Skip check if user is PREMIUM
        if (CurrentUserPlan != SubscriptionPlan.PREMIUM)
        {
            var currentUsage = await GetQuotaCounterAsync(userId, quotaKey);
            if (currentUsage >= limit)
            {
                throw new InvalidOperationException(
                    $"Monthly quota reached. You have used all {limit} of your free {quotaKey} AI calls. " +
                    "Please upgrade to Premium for unlimited access.");
            }
        }

        var aiReqEntity = new AiRequest
        {
            UserId = userId,
            ResumeId = resumeId,
            RequestType = type,
            InputPrompt = prompt,
            Status = AiRequestStatus.QUEUED
        };
        var saved = await aiRepo.AddAsync(aiReqEntity);

        string responseText;
        AiModel usedModel;
        int tokens;

        try
        {
            (responseText, tokens) = await CallOpenAiAsync(prompt);
            usedModel = AiModel.GPT4O;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI call failed.");
            saved.Status = AiRequestStatus.FAILED;
            await aiRepo.UpdateAsync(saved);
            throw new InvalidOperationException("AI service unavailable. Please try again later.");
        }

        saved.AiResponse = responseText.Replace("```json", "")
        .Replace("```", "")
        .Trim();
        saved.Model = usedModel;
        saved.TokensUsed = tokens;
        saved.Status = AiRequestStatus.COMPLETED;
        saved.CompletedAt = DateTime.UtcNow;
        await aiRepo.UpdateAsync(saved);

        await IncrementQuotaCounterAsync(userId, quotaKey);

        // Fire real-time notification — swallowed if Notification API is down
        var (notifTitle, notifType) = isAtsCall
            ? ("ATS Check Complete ✅", NotificationType.ATS_COMPLETE)
            : ("AI Generation Complete ✨", NotificationType.AI_DONE);
        await notificationPublisher.PublishAsync(
            userId,
            notifTitle,
            $"{type} finished for resume #{resumeId}.",
            notifType,
            relatedId:   saved.RequestId,
            relatedType: "AiRequest");

        return MapToDto(saved);
    }

    // ─── OpenAI GPT-4o ───────────────────────────────────────────

    private async Task<(string text, int tokens)> CallOpenAiAsync(string prompt)
    {
        var endpoint = config["OpenAI:Endpoint"];
        var apiKey = config["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey not configured.");
        var model = config["OpenAI:ModelName"] ?? "llama-3.3-70b-versatile";

        // Using standard OpenAI ChatClient with Groq endpoint
        ChatClient chatClient = new(model, new System.ClientModel.ApiKeyCredential(apiKey), new OpenAIClientOptions { Endpoint = new Uri(endpoint ?? "https://api.openai.com/v1") });
        
        var completion = await chatClient.CompleteChatAsync(
            [new UserChatMessage(prompt)]);

        var text = completion.Value.Content[0].Text;
        var tokens = completion.Value.Usage.TotalTokenCount;
        return (text, tokens);
    }


    // ─── Redis quota helpers ──────────────────────────────────────

    private async Task<int> GetQuotaCounterAsync(int userId, string type)
    {
        var key = QuotaKey(userId, type);
        var val = await cache.GetStringAsync(key);
        return val is null ? 0 : int.Parse(val);
    }

    private async Task IncrementQuotaCounterAsync(int userId, string type)
    {
        var key = QuotaKey(userId, type);
        var current = await GetQuotaCounterAsync(userId, type);
        var expiry = new DateTimeOffset(
            DateTime.UtcNow.Year, DateTime.UtcNow.Month,
            DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month),
            23, 59, 59, TimeSpan.Zero);
        await cache.SetStringAsync(key, (current + 1).ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpiration = expiry });
    }

    private static string QuotaKey(int userId, string type)
    {
        var now = DateTime.UtcNow;
        return $"ai-quota:{userId}:{type}:{now.Year}-{now.Month:D2}";
    }

    // ─── Prompt helpers ───────────────────────────────────────────

    /// <summary>
    /// Wraps resume context in a clearly delimited block for injection into
    /// prompts. If <paramref name="resumeContext"/> is empty the
    /// <paramref name="fallback"/> message is used instead (defaults to an
    /// empty string, meaning nothing extra is appended).
    /// </summary>
    private static string WrapContext(string resumeContext, string fallback = "")
    {
        if (string.IsNullOrWhiteSpace(resumeContext))
            return string.IsNullOrWhiteSpace(fallback) ? string.Empty : $"\n\n{fallback}";

        return $"\n\n=== CANDIDATE'S CURRENT RESUME ===\n{resumeContext.Trim()}\n=== END OF RESUME ===";
    }

    // ─── Mapping ─────────────────────────────────────────────────

    private static AiRequestDto MapToDto(AiRequest r) =>
        new(r.RequestId, r.UserId, r.ResumeId, r.RequestType,
            r.InputPrompt, r.AiResponse, r.Model, r.TokensUsed,
            r.Status, r.CreatedAt, r.CompletedAt);
}