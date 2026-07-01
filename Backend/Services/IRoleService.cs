using Backend.Data.Models;
using Backend.Models;

namespace Backend.Services;

public interface IRoleService
{
    Task<List<Role>> GetAllForServerAsync(Guid serverId);
    Task<Role?> GetByIdAsync(Guid roleId);
    Task<Role> CreateAsync(Guid serverId, string name, long permissions, int position = 0, string? color = null, bool isDefault = false);
    Task<Role?> UpdateAsync(Guid roleId, string? name = null, long? permissions = null, int? position = null, string? color = null, bool? isDefault = null);
    Task<bool> DeleteAsync(Guid roleId);

    Task AssignRoleToMemberAsync(Guid memberId, Guid roleId);
    Task RemoveRoleFromMemberAsync(Guid memberId, Guid roleId);
    Task<List<Role>> GetMemberRolesAsync(Guid memberId);
    Task<long> GetEffectivePermissionsAsync(Guid memberId);
}
