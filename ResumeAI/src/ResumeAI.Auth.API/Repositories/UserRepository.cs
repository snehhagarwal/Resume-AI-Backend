using Microsoft.EntityFrameworkCore;
using ResumeAI.Auth.API.Data;
using ResumeAI.Auth.API.Entities;
using ResumeAI.Shared.Enums;
using ResumeAI.Auth.API.Interfaces;

namespace ResumeAI.Auth.API.Repositories;

public class UserRepository(AuthDbContext db) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string email)
        => db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> FindByUserIdAsync(int userId)
        => db.Users.FindAsync(userId).AsTask();

    public Task<bool> ExistsByEmailAsync(string email)
        => db.Users.AnyAsync(u => u.Email == email);

    public Task<IList<User>> FindAllByRoleAsync(Role role)
        => db.Users.Where(u => u.Role == role).ToListAsync()
              .ContinueWith(t => (IList<User>)t.Result);

    public Task<IList<User>> FindBySubscriptionPlanAsync(SubscriptionPlan plan, int page, int pageSize)
        => db.Users.Where(u => u.SubscriptionPlan == plan)
               .Skip((page - 1) * pageSize).Take(pageSize)
               .ToListAsync()
               .ContinueWith(t => (IList<User>)t.Result);

    public Task<int> CountBySubscriptionPlanAsync(SubscriptionPlan plan)
        => db.Users.CountAsync(u => u.SubscriptionPlan == plan);

    public Task<IList<User>> FindByIsActiveAsync(bool isActive)
        => db.Users.Where(u => u.IsActive == isActive).ToListAsync()
              .ContinueWith(t => (IList<User>)t.Result);

    public Task<IList<User>> FindAllAsync()
        => db.Users.ToListAsync()
              .ContinueWith(t => (IList<User>)t.Result);

    public async Task<User> AddAsync(User user)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task DeleteByUserIdAsync(int userId)
    {
        await db.Users.Where(u => u.UserId == userId).ExecuteDeleteAsync();
    }
}
