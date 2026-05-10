using System.Net.Http.Json;
using System.Text.Json;
using ResumeAI.JobMatch.API.Interfaces;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;

namespace ResumeAI.JobMatch.API.Services;

public class RapidApiJobSearchClient(
    HttpClient httpClient, 
    IConfiguration config,
    ILogger<RapidApiJobSearchClient> logger) : IJobSearchClient
{
    public async Task<IList<JobMatchDto>> SearchJobs(int userId, int resumeId, string keywords)
    {
        var apiKey = config["ExternalApis:RapidApiKey"] ?? "";
        var apiHost = config["ExternalApis:RapidApiHost"] ?? "jsearch.p.rapidapi.com";

        var url = $"/search?query={Uri.EscapeDataString(keywords)}&num_pages=1";

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Key", apiKey);
        httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Host", apiHost);

        try
        {
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<JSearchResponse>();
                if (result?.Data != null)
                {
                    return result.Data.Select(j => {
                        var location = !string.IsNullOrEmpty(j.JobCity) ? $"{j.JobCity}, {j.JobCountry}" : j.JobCountry;
                        return new JobMatchDto(
                            0, resumeId, userId, j.JobTitle, j.JobDescription ?? j.JobTitle, 
                            j.EmployerName, location,
                            0, "", "", JobMatchSource.LINKEDIN, DateTime.UtcNow, false
                        );
                    }).ToList();
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogWarning("RapidAPI returned {StatusCode}: {Error}", response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling RapidAPI JSearch");
        }

        return new List<JobMatchDto>();
    }

    private class JSearchResponse
    {
        public List<JSearchJob> Data { get; set; } = [];
    }

    private class JSearchJob
    {
        [System.Text.Json.Serialization.JsonPropertyName("job_title")]
        public string JobTitle { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("job_description")]
        public string? JobDescription { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("employer_name")]
        public string? EmployerName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("job_city")]
        public string? JobCity { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("job_country")]
        public string? JobCountry { get; set; }
    }
}
