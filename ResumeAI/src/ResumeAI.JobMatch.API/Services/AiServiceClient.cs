using ResumeAI.JobMatch.API.Interfaces;
using ResumeAI.Shared.DTOs;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ResumeAI.JobMatch.API.Services;

public class AiServiceClient(HttpClient httpClient, ILogger<AiServiceClient> logger, IHttpContextAccessor httpContextAccessor) : IAiServiceClient
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { 
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<JobMatchAiResponse?> AnalyzeJobFit(int resumeId, string jobDescription)
    {
        var request = new CheckAtsRequest(resumeId, jobDescription);

        try
        {
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString().Replace("Bearer ", ""));
            var response = await httpClient.PostAsJsonAsync("api/ai/analyze-job-fit", request);
            if (response.IsSuccessStatusCode)
            {
                var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<AiRequestDto>>(_jsonOptions);
                if (envelope?.Success == true && envelope.Data?.AiResponse != null)
                {
                    return JsonSerializer.Deserialize<JobMatchAiResponse>(envelope.Data.AiResponse, _jsonOptions);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling AI Service for job fit analysis");
        }

        return null;
    }
}
