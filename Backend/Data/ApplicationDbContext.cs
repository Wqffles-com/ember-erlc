using Backend.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Server> Servers => Set<Server>();
    public DbSet<ServerMember> ServerMembers => Set<ServerMember>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<MemberRole> MemberRoles => Set<MemberRole>();
    public DbSet<ModerationLog> ModerationLogs => Set<ModerationLog>();
    public DbSet<JoinRequest> JoinRequests => Set<JoinRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>()
            .HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApplicationUser>()
            .HasMany(u => u.OwnedServers)
            .WithOne(s => s.Owner)
            .HasForeignKey(s => s.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApplicationUser>()
            .HasMany(u => u.ServerMemberships)
            .WithOne(sm => sm.User)
            .HasForeignKey(sm => sm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Server>()
            .HasMany(s => s.Members)
            .WithOne(sm => sm.Server)
            .HasForeignKey(sm => sm.ServerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Server>()
            .HasMany(s => s.Roles)
            .WithOne(r => r.Server)
            .HasForeignKey(r => r.ServerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ServerMember>()
            .HasMany(sm => sm.MemberRoles)
            .WithOne(mr => mr.Member)
            .HasForeignKey(mr => mr.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Role>()
            .HasMany(r => r.MemberRoles)
            .WithOne(mr => mr.Role)
            .HasForeignKey(mr => mr.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MemberRole>()
            .HasIndex(mr => new { mr.MemberId, mr.RoleId })
            .IsUnique();

        modelBuilder.Entity<JoinRequest>()
            .HasOne(jr => jr.Server)
            .WithMany()
            .HasForeignKey(jr => jr.ServerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ModerationLog>()
            .HasOne(m => m.Server)
            .WithMany()
            .HasForeignKey(m => m.ServerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ModerationLog>()
            .HasOne(m => m.Actor)
            .WithMany()
            .HasForeignKey(m => m.ActorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JoinRequest>()
            .HasOne(jr => jr.Server)
            .WithMany()
            .HasForeignKey(jr => jr.ServerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JoinRequest>()
            .HasOne(jr => jr.User)
            .WithMany()
            .HasForeignKey(jr => jr.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
