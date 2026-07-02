using Microsoft.EntityFrameworkCore;

namespace Backend.Data.Models;

public enum JoinRequestStatus
{
    Pending,
    Accepted,
    Denied
}

[PrimaryKey(nameof(Id))]
public class JoinRequest
{
    public Guid Id { get; set; }
    public Guid ServerId { get; set; }
    public Server Server { get; set; } = null!;
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public JoinRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
