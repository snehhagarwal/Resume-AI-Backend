using ResumeAI.Shared.Enums;

namespace ResumeAI.AI.API.Entities;

public class AiRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public int UserId { get; set; }
    public int ResumeId { get; set; }
    public AiRequestType RequestType { get; set; }
    public string InputPrompt { get; set; } = string.Empty;
    public string AiResponse { get; set; } = string.Empty;
    public AiModel Model { get; set; } = AiModel.GPT4O;
    public int TokensUsed { get; set; }
    public AiRequestStatus Status { get; set; } = AiRequestStatus.QUEUED;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
