using Microsoft.EntityFrameworkCore;

namespace Backend.Data.Models;

[PrimaryKey(nameof(Id))]
public class Server
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? RobloxServerId { get; set; }
    public Guid OwnerId { get; set; }
    public ApplicationUser Owner { get; set; } = null!;
    public string? IconUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ServerMember> Members { get; set; } = [];
    public List<Role> Roles { get; set; } = [];
}
