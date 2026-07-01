using Backend.Data;
using Backend.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class ApplicationUserService(ApplicationDbContext context, IPasswordHasher<ApplicationUser> passwordHasher) : IApplicationUserService
{
    public async Task<List<ApplicationUser>> GetAllAsync()
    {
        return await context.Users.ToListAsync();
    }

    public async Task<ApplicationUser?> GetByIdAsync(Guid id)
    {
        return await context.Users.FindAsync(id);
    }

    public async Task<ApplicationUser?> GetByUserNameAsync(string userName)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
    }

    public async Task<ApplicationUser> CreateAsync(string userName, string password)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            PasswordHash = passwordHasher.HashPassword(null!, password)
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    public async Task<ApplicationUser?> UpdateAsync(Guid id, string userName)
    {
        var user = await context.Users.FindAsync(id);
        if (user is null) return null;

        user.UserName = userName;
        await context.SaveChangesAsync();

        return user;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await context.Users.FindAsync(id);
        if (user is null) return false;

        context.Users.Remove(user);
        await context.SaveChangesAsync();

        return true;
    }
}
