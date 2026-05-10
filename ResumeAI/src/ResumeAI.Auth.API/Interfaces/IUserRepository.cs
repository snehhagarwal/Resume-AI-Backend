using ResumeAI.Auth.API.Entities;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Auth.API.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email);
    Task<User?> FindByUserIdAsync(int userId);
    Task<bool> ExistsByEmailAsync(string email);
    Task<IList<User>> FindAllByRoleAsync(Role role);
    Task<IList<User>> FindBySubscriptionPlanAsync(SubscriptionPlan plan, int page, int pageSize);
    Task<int> CountBySubscriptionPlanAsync(SubscriptionPlan plan);
    Task<IList<User>> FindByIsActiveAsync(bool isActive);
    Task<IList<User>> FindAllAsync();
    Task<User> AddAsync(User user);
    Task<User> UpdateAsync(User user);
    Task DeleteByUserIdAsync(int userId);
}
