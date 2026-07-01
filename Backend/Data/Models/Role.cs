using Microsoft.EntityFrameworkCore;

namespace Backend.Data.Models;

[PrimaryKey(nameof(Id))]
public class Role
{
    public Guid Id { get; set; }
    public Guid ServerId { get; set; }
    public Server Server { get; set; } = null!;
    public required string Name { get; set; }
    public string? Color { get; set; }
    public long Permissions { get; set; }
    public int Position { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<MemberRole> MemberRoles { get; set; } = [];
}
