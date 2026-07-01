using Microsoft.EntityFrameworkCore;

namespace Backend.Data.Models;

[PrimaryKey(nameof(MemberId), nameof(RoleId))]
public class MemberRole
{
    public Guid MemberId { get; set; }
    public ServerMember Member { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
