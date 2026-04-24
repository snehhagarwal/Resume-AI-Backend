using Microsoft.AspNetCore.Http;
using ResumeAI.AI.API.Interfaces;
using ResumeAI.Shared.DTOs;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ResumeAI.AI.API.Clients;

/// <summary>
/// Calls the Resume and Section internal services to fetch real resume data.
/// Forwards the caller's JWT Bearer token so downstream services can authorise
/// the request — keeps the service-to-service call in the authenticated user's
/// security context.
/// </summary>
public sealed class ResumeContextClient(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<ResumeContextClient> logger) : IResumeContextClient
{
    private static readonly JsonSerializerOptions _json =
        new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

    // ─── Public methods ───────────────────────────────────────────

    public async Task<ResumeDto?> GetResumeAsync(int resumeId)
    {
        try
        {
            var client = BuildClient("ResumeApiClient");
            var response = await client.GetAsync($"api/resumes/{resumeId}");

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Resume API returned {Status} for resume {Id}.",
                    response.StatusCode, resumeId);
                return null;
            }

            var envelope = await DeserializeAsync<ApiResponse<ResumeDto>>(response);
            return envelope?.Data;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch resume {ResumeId} from Resume API.", resumeId);
            return null;
        }
    }

    public async Task<IList<SectionDto>> GetSectionsAsync(int resumeId)
    {
        try
        {
            var client = BuildClient("SectionApiClient");
            var response = await client.GetAsync($"api/sections/by-resume/{resumeId}");

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Section API returned {Status} for resume {Id}.",
                    response.StatusCode, resumeId);
                return [];
            }

            var envelope = await DeserializeAsync<ApiResponse<IList<SectionDto>>>(response);
            return envelope?.Data ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch sections for resume {ResumeId}.", resumeId);
            return [];
        }
    }

    public async Task<SectionDto?> GetSectionAsync(int sectionId)
    {
        try
        {
            var client = BuildClient("SectionApiClient");
            var response = await client.GetAsync($"api/sections/{sectionId}");

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Section API returned {Status} for section {Id}.",
                    response.StatusCode, sectionId);
                return null;
            }

            var envelope = await DeserializeAsync<ApiResponse<SectionDto>>(response);
            return envelope?.Data;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch section {SectionId} from Section API.", sectionId);
            return null;
        }
    }

    public async Task<string> BuildResumeContextAsync(int resumeId)
    {
        var (resume, sections) = await (GetResumeAsync(resumeId), GetSectionsAsync(resumeId))
            .WhenBoth();

        if (resume is null && sections.Count == 0)
            return string.Empty;

        var sb = new System.Text.StringBuilder();

        if (resume is not null)
        {
            sb.AppendLine("=== RESUME METADATA ===");
            sb.AppendLine($"Title         : {resume.Title}");
            sb.AppendLine($"Target Role   : {resume.TargetJobTitle}");
            sb.AppendLine($"Language      : {resume.Language}");
            sb.AppendLine($"Current ATS   : {resume.AtsScore}/100");
        }

        if (sections.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("=== RESUME SECTIONS ===");
            foreach (var section in sections.Where(s => s.IsVisible)
                         .OrderBy(s => s.DisplayOrder))
            {
                sb.AppendLine($"--- {section.SectionType}: {section.Title} ---");
                sb.AppendLine(section.Content);
            }
        }

        return sb.ToString();
    }

    // ─── Helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Creates a named HttpClient and attaches the caller's Bearer token.
    /// </summary>
    private HttpClient BuildClient(string name)
    {
        var client = httpClientFactory.CreateClient(name);

        var token = ExtractBearerToken();
        if (token is not null)
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private string? ExtractBearerToken()
    {
        var header = httpContextAccessor.HttpContext?
            .Request.Headers.Authorization.FirstOrDefault();

        if (header is null) return null;

        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..]
            : header;
    }

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<T>(stream, _json);
    }
}

// ─── Minimal tuple-task helper (keeps allocations low) ───────────────────────
file static class TaskExtensions
{
    public static async Task<(T1, T2)> WhenBoth<T1, T2>(
        this (Task<T1> t1, Task<T2> t2) tasks)
    {
        await Task.WhenAll(tasks.t1, tasks.t2);
        return (tasks.t1.Result, tasks.t2.Result);
    }
}