using Microsoft.EntityFrameworkCore;

namespace Backend.Data.Models;

[PrimaryKey(nameof(Id))]
[Index(nameof(UserName))]
public class ApplicationUser
{
    public Guid Id { get; set; }
    public required string UserName { get; set; }
    public required string PasswordHash { get; set; }
    public List<RefreshToken> RefreshTokens { get; set; } = [];
    public List<Server> OwnedServers { get; set; } = [];
    public List<ServerMember> ServerMemberships { get; set; } = [];
}