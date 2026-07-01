using Microsoft.EntityFrameworkCore;

namespace Backend.Data.Models;

[PrimaryKey(nameof(Id))]
public class ServerMember
{
    public Guid Id { get; set; }
    public Guid ServerId { get; set; }
    public Server Server { get; set; } = null!;
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public DateTime JoinedAt { get; set; }
    public List<MemberRole> MemberRoles { get; set; } = [];
}
