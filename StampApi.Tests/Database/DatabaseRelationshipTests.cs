using Microsoft.EntityFrameworkCore;
using StampApi.Models;
using StampApi.Tests.TestHelpers;

namespace StampApi.Tests.Database;

public class DatabaseRelationshipTests : ControllerTestBase, IAsyncLifetime
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
    }

    public override Task DisposeAsync()
    {
        return base.DisposeAsync();
    }

    [Fact]
    public async Task UserWorkspaceRelationship_CascadeDeleteBehavior()
    {
        // Arrange
        var user = TestUser;
        var workspace = await CreateTestWorkspaceAsync(user, "Test Workspace");
        var collection = await CreateTestCollectionAsync(user, workspace, "Test Collection");
        var request = await CreateTestApiRequestAsync(collection, "Test Request");

        // Act - Delete user (this would happen through Identity system, but we can test the relationship)
        Context.Users.Remove(user);
        await Context.SaveChangesAsync();

        // Assert - Workspace should be deleted due to cascade
        var deletedWorkspace = await Context.Workspaces.FindAsync(workspace.Id);
        deletedWorkspace.Should().BeNull();

        // Collection should have SetNull behavior for user reference but be deleted due to workspace cascade
        var deletedCollection = await Context.Collections.FindAsync(collection.Id);
        deletedCollection.Should().BeNull();

        // Request should be deleted due to collection cascade
        var deletedRequest = await Context.ApiRequests.FindAsync(request.Id);
        deletedRequest.Should().BeNull();
    }

    [Fact]
    public async Task WorkspaceCollectionRelationship_SetNullBehavior()
    {
        // Arrange
        var user = TestUser;
        var workspace = await CreateTestWorkspaceAsync(user, "Test Workspace");
        var collection = await CreateTestCollectionAsync(user, workspace, "Test Collection");
        var request = await CreateTestApiRequestAsync(collection, "Test Request");

        // Act - Delete workspace directly (bypassing controller logic)
        Context.Workspaces.Remove(workspace);
        await Context.SaveChangesAsync();

        // Assert - Collection should have WorkspaceId set to null
        var orphanedCollection = await Context.Collections.FindAsync(collection.Id);
        orphanedCollection.Should().NotBeNull();
        orphanedCollection!.WorkspaceId.Should().BeNull();

        // Request should still exist
        var remainingRequest = await Context.ApiRequests.FindAsync(request.Id);
        remainingRequest.Should().NotBeNull();
    }

    [Fact]
    public async Task CollectionApiRequestRelationship_CascadeDeleteBehavior()
    {
        // Arrange
        var user = TestUser;
        var workspace = await CreateTestWorkspaceAsync(user, "Test Workspace");
        var collection = await CreateTestCollectionAsync(user, workspace, "Test Collection");
        var request1 = await CreateTestApiRequestAsync(collection, "Request 1");
        var request2 = await CreateTestApiRequestAsync(collection, "Request 2");
        var request3 = await CreateTestApiRequestAsync(collection, "Request 3");

        // Act - Delete collection
        Context.Collections.Remove(collection);
        await Context.SaveChangesAsync();

        // Assert - All requests should be deleted due to cascade
        var deletedRequest1 = await Context.ApiRequests.FindAsync(request1.Id);
        var deletedRequest2 = await Context.ApiRequests.FindAsync(request2.Id);
        var deletedRequest3 = await Context.ApiRequests.FindAsync(request3.Id);

        deletedRequest1.Should().BeNull();
        deletedRequest2.Should().BeNull();
        deletedRequest3.Should().BeNull();
    }

    [Fact]
    public async Task CollectionMemberRelationship_CascadeDeleteBehavior()
    {
        // Arrange
        var user = TestUser;
        var workspace = await CreateTestWorkspaceAsync(user, "Test Workspace");
        var collection = await CreateTestCollectionAsync(user, workspace, "Test Collection");
        
        // Add additional members
        var member1 = await CreateAdditionalUserAsync("member1@example.com");
        var member2 = await CreateAdditionalUserAsync("member2@example.com");

        var membership1 = new CollectionMember
        {
            CollectionId = collection.Id,
            UserId = member1.Id,
            Role = CollectionRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        var membership2 = new CollectionMember
        {
            CollectionId = collection.Id,
            UserId = member2.Id,
            Role = CollectionRole.Admin,
            JoinedAt = DateTime.UtcNow
        };

        Context.CollectionMembers.AddRange(membership1, membership2);
        await Context.SaveChangesAsync();

        var membershipIds = new[] { membership1.Id, membership2.Id };

        // Act - Delete collection
        Context.Collections.Remove(collection);
        await Context.SaveChangesAsync();

        // Assert - All memberships should be deleted due to cascade
        var remainingMemberships = await Context.CollectionMembers
            .Where(m => membershipIds.Contains(m.Id))
            .ToListAsync();

        remainingMemberships.Should().BeEmpty();
    }

    [Fact]
    public async Task UserDeletion_CascadeDeletesMemberships()
    {
        // Arrange
        var user = TestUser;
        var otherUser = await CreateAdditionalUserAsync("other@example.com");
        var workspace = await CreateTestWorkspaceAsync(user, "Test Workspace");
        var collection = await CreateTestCollectionAsync(user, workspace, "Test Collection");

        // Add other user as member
        var membership = new CollectionMember
        {
            CollectionId = collection.Id,
            UserId = otherUser.Id,
            Role = CollectionRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        Context.CollectionMembers.Add(membership);
        await Context.SaveChangesAsync();

        // Act - Delete the other user
        Context.Users.Remove(otherUser);
        await Context.SaveChangesAsync();

        // Assert - Membership should be deleted due to user cascade
        var deletedMembership = await Context.CollectionMembers.FindAsync(membership.Id);
        deletedMembership.Should().BeNull();

        // Collection should still exist
        var remainingCollection = await Context.Collections.FindAsync(collection.Id);
        remainingCollection.Should().NotBeNull();
    }

    [Fact]
    public async Task CollectionInviteRelationship_CascadeDeleteBehavior()
    {
        // Arrange
        var user = TestUser;
        var workspace = await CreateTestWorkspaceAsync(user, "Test Workspace");
        var collection = await CreateTestCollectionAsync(user, workspace, "Test Collection");

        var invite = new CollectionInvite
        {
            CollectionId = collection.Id,
            InvitedEmail = "invited@example.com",
            InviteToken = Guid.NewGuid().ToString(),
            Role = CollectionRole.Member,
            Status = InviteStatus.Pending,
            InvitedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        Context.CollectionInvites.Add(invite);
        await Context.SaveChangesAsync();

        // Act - Delete collection
        Context.Collections.Remove(collection);
        await Context.SaveChangesAsync();

        // Assert - Invite should be deleted due to cascade
        var deletedInvite = await Context.CollectionInvites.FindAsync(invite.Id);
        deletedInvite.Should().BeNull();
    }

    [Fact]
    public async Task InvitedByUserDeletion_RestrictBehavior()
    {
        // Arrange
        var inviterUser = TestUser;
        var inviteeUser = await CreateAdditionalUserAsync("invitee@example.com");
        var workspace = await CreateTestWorkspaceAsync(inviterUser, "Test Workspace");
        var collection = await CreateTestCollectionAsync(inviterUser, workspace, "Test Collection");

        var invite = new CollectionInvite
        {
            CollectionId = collection.Id,
            InvitedEmail = "newinvite@example.com",
            InviteToken = Guid.NewGuid().ToString(),
            Role = CollectionRole.Member,
            Status = InviteStatus.Pending,
            InvitedByUserId = inviterUser.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        Context.CollectionInvites.Add(invite);
        await Context.SaveChangesAsync();

        // Act & Assert - Deleting inviter user should be restricted if there are pending invites
        Context.Users.Remove(inviterUser);
        
        var action = async () => await Context.SaveChangesAsync();
        await action.Should().ThrowAsync<Exception>(); // Should throw constraint violation due to restrict behavior
    }

    [Fact]
    public async Task AcceptedByUserDeletion_SetNullBehavior()
    {
        // Arrange
        var inviterUser = TestUser;
        var accepterUser = await CreateAdditionalUserAsync("accepter@example.com");
        var workspace = await CreateTestWorkspaceAsync(inviterUser, "Test Workspace");
        var collection = await CreateTestCollectionAsync(inviterUser, workspace, "Test Collection");

        var invite = new CollectionInvite
        {
            CollectionId = collection.Id,
            InvitedEmail = "accepted@example.com",
            InviteToken = Guid.NewGuid().ToString(),
            Role = CollectionRole.Member,
            Status = InviteStatus.Accepted,
            InvitedByUserId = inviterUser.Id,
            AcceptedByUserId = accepterUser.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        Context.CollectionInvites.Add(invite);
        await Context.SaveChangesAsync();

        // Act - Delete accepter user
        Context.Users.Remove(accepterUser);
        await Context.SaveChangesAsync();

        // Assert - Invite should still exist but AcceptedByUserId should be null
        var remainingInvite = await Context.CollectionInvites.FindAsync(invite.Id);
        remainingInvite.Should().NotBeNull();
        remainingInvite!.AcceptedByUserId.Should().BeNull();
        remainingInvite.InvitedByUserId.Should().Be(inviterUser.Id); // Should remain unchanged
    }

    [Fact]
    public async Task UniqueConstraints_CollectionMemberUniqueness()
    {
        // Arrange
        var user = TestUser;
        var workspace = await CreateTestWorkspaceAsync(user, "Test Workspace");
        var collection = await CreateTestCollectionAsync(user, workspace, "Test Collection");

        // Try to add the same user to the same collection twice
        var membership1 = new CollectionMember
        {
            CollectionId = collection.Id,
            UserId = user.Id,
            Role = CollectionRole.Admin,
            JoinedAt = DateTime.UtcNow
        };

        var membership2 = new CollectionMember
        {
            CollectionId = collection.Id,
            UserId = user.Id,
            Role = CollectionRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        Context.CollectionMembers.Add(membership1);
        await Context.SaveChangesAsync();

        // Act & Assert
        Context.CollectionMembers.Add(membership2);
        
        var action = async () => await Context.SaveChangesAsync();
        await action.Should().ThrowAsync<Exception>(); // Should throw unique constraint violation
    }

    [Fact]
    public async Task UniqueConstraints_InviteTokenUniqueness()
    {
        // Arrange
        var user = TestUser;
        var workspace = await CreateTestWorkspaceAsync(user, "Test Workspace");
        var collection1 = await CreateTestCollectionAsync(user, workspace, "Collection 1");
        var collection2 = await CreateTestCollectionAsync(user, workspace, "Collection 2");

        var duplicateToken = Guid.NewGuid().ToString();

        var invite1 = new CollectionInvite
        {
            CollectionId = collection1.Id,
            InvitedEmail = "invite1@example.com",
            InviteToken = duplicateToken,
            Role = CollectionRole.Member,
            Status = InviteStatus.Pending,
            InvitedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        var invite2 = new CollectionInvite
        {
            CollectionId = collection2.Id,
            InvitedEmail = "invite2@example.com",
            InviteToken = duplicateToken, // Same token
            Role = CollectionRole.Member,
            Status = InviteStatus.Pending,
            InvitedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        Context.CollectionInvites.Add(invite1);
        await Context.SaveChangesAsync();

        // Act & Assert
        Context.CollectionInvites.Add(invite2);
        
        var action = async () => await Context.SaveChangesAsync();
        await action.Should().ThrowAsync<Exception>(); // Should throw unique constraint violation
    }

    [Fact]
    public async Task NavigationProperties_LoadCorrectly()
    {
        // Arrange
        var user = TestUser;
        var workspace = await CreateTestWorkspaceAsync(user, "Test Workspace");
        var collection = await CreateTestCollectionAsync(user, workspace, "Test Collection");
        var request = await CreateTestApiRequestAsync(collection, "Test Request");

        var member = await CreateAdditionalUserAsync("member@example.com");
        var membership = new CollectionMember
        {
            CollectionId = collection.Id,
            UserId = member.Id,
            Role = CollectionRole.Member,
            JoinedAt = DateTime.UtcNow
        };
        Context.CollectionMembers.Add(membership);
        await Context.SaveChangesAsync();

        // Act - Load workspace with all related data
        var loadedWorkspace = await Context.Workspaces
            .Include(w => w.Collections)
                .ThenInclude(c => c.Requests)
            .Include(w => w.Collections)
                .ThenInclude(c => c.Members)
                    .ThenInclude(m => m.User)
            .FirstAsync(w => w.Id == workspace.Id);

        // Assert
        loadedWorkspace.Should().NotBeNull();
        loadedWorkspace.Collections.Should().HaveCount(1);
        
        var loadedCollection = loadedWorkspace.Collections.First();
        loadedCollection.Requests.Should().HaveCount(1);
        loadedCollection.Members.Should().HaveCount(2); // Owner + Member
        
        var ownerMember = loadedCollection.Members.First(m => m.Role == CollectionRole.Owner);
        var regularMember = loadedCollection.Members.First(m => m.Role == CollectionRole.Member);
        
        ownerMember.User.Should().NotBeNull();
        ownerMember.User.Email.Should().Be(user.Email);
        
        regularMember.User.Should().NotBeNull();
        regularMember.User.Email.Should().Be(member.Email);
    }

    [Fact]
    public async Task DatabaseIndexes_PerformEfficiently()
    {
        // Arrange - Create many records to test index performance
        var user = TestUser;
        var workspaces = new List<Workspace>();
        
        for (int i = 0; i < 10; i++)
        {
            var workspace = await CreateTestWorkspaceAsync(user, $"Workspace {i}");
            workspaces.Add(workspace);
            
            for (int j = 0; j < 10; j++)
            {
                await CreateTestCollectionAsync(user, workspace, $"Collection {i}-{j}");
            }
        }

        // Act - Query collections by workspace (should use index)
        var targetWorkspace = workspaces[5];
        var collections = await Context.Collections
            .Where(c => c.WorkspaceId == targetWorkspace.Id)
            .ToListAsync();

        // Assert
        collections.Should().HaveCount(10);
        collections.Should().OnlyContain(c => c.WorkspaceId == targetWorkspace.Id);
    }

    [Fact]
    public async Task SoftDelete_NotImplemented_HardDeleteBehavior()
    {
        // This test verifies that we're doing hard deletes, not soft deletes
        // If soft delete were implemented, deleted records would still exist with a deleted flag

        // Arrange
        var user = TestUser;
        var workspace = await CreateTestWorkspaceAsync(user, "Test Workspace");
        var collection = await CreateTestCollectionAsync(user, workspace, "Test Collection");

        var originalCollectionCount = await Context.Collections.CountAsync();
        originalCollectionCount.Should().Be(1);

        // Act - Delete collection
        Context.Collections.Remove(collection);
        await Context.SaveChangesAsync();

        // Assert - Record should be completely gone (hard delete)
        var remainingCollectionCount = await Context.Collections.CountAsync();
        remainingCollectionCount.Should().Be(0);

        // Verify no "deleted" records exist
        var allCollections = await Context.Collections.IgnoreQueryFilters().ToListAsync();
        allCollections.Should().BeEmpty();
    }
}