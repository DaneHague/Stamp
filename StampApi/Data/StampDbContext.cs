using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StampApi.Models;

namespace StampApi.Data;

public class StampDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public StampDbContext(DbContextOptions<StampDbContext> options) : base(options) { }

    public DbSet<Workspace> Workspaces { get; set; }
    public DbSet<Collection> Collections { get; set; }
    public DbSet<ApiRequest> ApiRequests { get; set; }
    public DbSet<CollectionMember> CollectionMembers { get; set; }
    public DbSet<CollectionInvite> CollectionInvites { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.GoogleId).HasMaxLength(255);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastLoginAt).IsRequired();
            
            entity.HasIndex(e => e.GoogleId).IsUnique();
        });

        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Workspaces)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Collections)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
                  
            entity.HasOne(e => e.Workspace)
                  .WithMany(w => w.Collections)
                  .HasForeignKey(e => e.WorkspaceId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ApiRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Method).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Headers);
            entity.Property(e => e.Body);
            entity.Property(e => e.QueryParams);
            entity.Property(e => e.Authentication);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasOne(e => e.Collection)
                  .WithMany(c => c.Requests)
                  .HasForeignKey(e => e.CollectionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CollectionMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.JoinedAt).IsRequired();
            
            entity.HasOne(e => e.Collection)
                  .WithMany(c => c.Members)
                  .HasForeignKey(e => e.CollectionId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            // Ensure unique collection-user pairs
            entity.HasIndex(e => new { e.CollectionId, e.UserId }).IsUnique();
        });

        modelBuilder.Entity<CollectionInvite>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvitedEmail).IsRequired().HasMaxLength(255);
            entity.Property(e => e.InviteToken).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            
            entity.HasOne(e => e.Collection)
                  .WithMany(c => c.Invites)
                  .HasForeignKey(e => e.CollectionId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.InvitedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.InvitedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            entity.HasOne(e => e.AcceptedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.AcceptedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
                  
            entity.HasIndex(e => e.InviteToken).IsUnique();
        });
    }
}