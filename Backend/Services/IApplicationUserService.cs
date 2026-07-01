using Backend.Data.Models;

namespace Backend.Services;

public interface IApplicationUserService
{
    Task<List<ApplicationUser>> GetAllAsync();
    Task<ApplicationUser?> GetByIdAsync(Guid id);
    Task<ApplicationUser?> GetByUserNameAsync(string userName);
    Task<ApplicationUser> CreateAsync(string userName, string password);
    Task<ApplicationUser?> UpdateAsync(Guid id, string userName);
    Task<bool> DeleteAsync(Guid id);
}
