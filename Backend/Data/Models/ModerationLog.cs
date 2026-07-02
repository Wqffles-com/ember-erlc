using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data.Models;

[PrimaryKey(nameof(Id))]
public class ModerationLog
{
    public Guid Id { get; set; }
    public Guid ServerId { get; set; }
    public Server Server { get; set; } = null!;
    public Guid ActorId { get; set; }
    public ApplicationUser Actor { get; set; } = null!;
    public required string TargetUserId { get; set; }
    public string? TargetUsername { get; set; }
    public ActionType ActionType { get; set; }
    public required string Reason { get; set; }
    public string? Evidence { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
