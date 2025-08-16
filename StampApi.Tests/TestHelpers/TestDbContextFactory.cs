using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StampApi.Data;
using StampApi.Models;

namespace StampApi.Tests.TestHelpers;

public static class TestDbContextFactory
{
    public static StampDbContext CreateInMemoryContext(string databaseName = "")
    {
        if (string.IsNullOrEmpty(databaseName))
        {
            databaseName = Guid.NewGuid().ToString();
        }

        var options = new DbContextOptionsBuilder<StampDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var context = new StampDbContext(options);
        
        // Ensure the database is created
        context.Database.EnsureCreated();
        
        return context;
    }

    public static async Task<ApplicationUser> CreateTestUserAsync(StampDbContext context, string email = "test@example.com")
    {
        var user = new ApplicationUser
        {
            Id = new Random().Next(1, 10000),
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpper(),
            NormalizedUserName = email.ToUpper(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    public static async Task<Workspace> CreateTestWorkspaceAsync(StampDbContext context, ApplicationUser user, string name = "Test Workspace")
    {
        var workspace = new Workspace
        {
            Name = name,
            Description = "Test workspace description",
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();
        return workspace;
    }

    public static async Task<Collection> CreateTestCollectionAsync(StampDbContext context, ApplicationUser user, Workspace workspace, string name = "Test Collection")
    {
        var collection = new Collection
        {
            Name = name,
            Description = "Test collection description",
            UserId = user.Id,
            WorkspaceId = workspace.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Collections.Add(collection);
        await context.SaveChangesAsync();

        // Create owner membership
        var ownerMembership = new CollectionMember
        {
            CollectionId = collection.Id,
            UserId = user.Id,
            Role = CollectionRole.Owner,
            JoinedAt = DateTime.UtcNow
        };
        
        context.CollectionMembers.Add(ownerMembership);
        await context.SaveChangesAsync();
        
        return collection;
    }

    public static async Task<ApiRequest> CreateTestApiRequestAsync(StampDbContext context, Collection collection, string name = "Test Request")
    {
        var request = new ApiRequest
        {
            Name = name,
            Url = "https://api.example.com/test",
            Method = "GET",
            CollectionId = collection.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.ApiRequests.Add(request);
        await context.SaveChangesAsync();
        return request;
    }
}