using ResumeAI.Shared.Enums;

namespace ResumeAI.Auth.API.Entities;

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Role Role { get; set; } = Role.USER;
    public AuthProvider Provider { get; set; } = AuthProvider.LOCAL;
    public bool IsActive { get; set; } = true;
    public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.FREE;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
