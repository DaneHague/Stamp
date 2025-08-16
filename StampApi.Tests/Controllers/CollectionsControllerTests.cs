using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StampApi.Controllers;
using StampApi.Models;
using StampApi.Tests.TestHelpers;

namespace StampApi.Tests.Controllers;

public class CollectionsControllerTests : ControllerTestBase, IAsyncLifetime
{
    private CollectionsController _controller = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _controller = new CollectionsController(Context);
        SetupControllerContext(_controller);
    }

    public override Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetCollections_ReturnsAllUserCollections_WhenNoWorkspaceIdProvided()
    {
        // Arrange
        var workspace1 = await CreateTestWorkspaceAsync(TestUser, "Workspace 1");
        var workspace2 = await CreateTestWorkspaceAsync(TestUser, "Workspace 2");
        var collection1 = await CreateTestCollectionAsync(TestUser, workspace1, "Collection 1");
        var collection2 = await CreateTestCollectionAsync(TestUser, workspace2, "Collection 2");
        await CreateTestApiRequestAsync(collection1, "Request 1");
        
        // Create another user's collection to ensure isolation
        var otherUser = await CreateAdditionalUserAsync("other@example.com");
        var otherWorkspace = await CreateTestWorkspaceAsync(otherUser, "Other Workspace");
        await CreateTestCollectionAsync(otherUser, otherWorkspace, "Other Collection");

        // Act
        var result = await _controller.GetCollections();

        // Assert
        result.Should().NotBeNull();
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<OkObjectResult>();
        
        var collections = GetActionResultValue(result);
        collections.Should().NotBeNull();
        collections.Should().HaveCount(2);
        collections.Should().Contain(c => c.Name == "Collection 1");
        collections.Should().Contain(c => c.Name == "Collection 2");
        collections.Should().NotContain(c => c.Name == "Other Collection");
        
        // Verify requests and members are included
        var collectionWithRequest = collections.First(c => c.Name == "Collection 1");
        collectionWithRequest.Requests.Should().HaveCount(1);
        collectionWithRequest.Members.Should().HaveCount(1);
        collectionWithRequest.Members.First().Role.Should().Be(CollectionRole.Owner);
    }

    [Fact]
    public async Task GetCollections_ReturnsFilteredCollections_WhenWorkspaceIdProvided()
    {
        // Arrange
        var workspace1 = await CreateTestWorkspaceAsync(TestUser, "Workspace 1");
        var workspace2 = await CreateTestWorkspaceAsync(TestUser, "Workspace 2");
        var collection1 = await CreateTestCollectionAsync(TestUser, workspace1, "Collection 1");
        var collection2 = await CreateTestCollectionAsync(TestUser, workspace2, "Collection 2");

        // Act
        var result = await _controller.GetCollections(workspace1.Id);

        // Assert
        result.Should().NotBeNull();
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<OkObjectResult>();
        
        var collections = GetActionResultValue(result);
        collections.Should().NotBeNull();
        collections.Should().HaveCount(1);
        collections.Should().Contain(c => c.Name == "Collection 1");
        collections.Should().NotContain(c => c.Name == "Collection 2");
    }

    [Fact]
    public async Task GetCollections_ReturnsSharedCollections_WhenUserIsMember()
    {
        // Arrange
        var workspace = await CreateTestWorkspaceAsync(TestUser, "Test Workspace");
        var collection = await CreateTestCollectionAsync(TestUser, workspace, "Shared Collection");
        
        // Add another user as a member
        var otherUser = await CreateAdditionalUserAsync("other@example.com");
        var membership = new CollectionMember
        {
            CollectionId = collection.Id,
            UserId = otherUser.Id,
            Role = CollectionRole.Member,
            JoinedAt = DateTime.UtcNow
        };
        Context.CollectionMembers.Add(membership);
        await Context.SaveChangesAsync();
        
        // Switch to other user's context
        SetupControllerContext(_controller, otherUser.Id);

        // Act
        var result = await _controller.GetCollections();

        // Assert
        result.Should().NotBeNull();
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<OkObjectResult>();
        
        var collections = GetActionResultValue(result);
        collections.Should().NotBeNull();
        collections.Should().HaveCount(1);
        collections.First().Name.Should().Be("Shared Collection");
    }

    [Fact]
    public async Task GetCollections_ReturnsUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _controller = new CollectionsController(Context); // No authentication context

        // Act
        var result = await _controller.GetCollections();

        // Assert
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetCollection_ReturnsCollection_WhenCollectionExistsAndUserHasAccess()
    {
        // Arrange
        var workspace = await CreateTestWorkspaceAsync(TestUser, "Test Workspace");
        var collection = await CreateTestCollectionAsync(TestUser, workspace, "Test Collection");
        await CreateTestApiRequestAsync(collection, "Test Request");

        // Act
        var result = await _controller.GetCollection(collection.Id);

        // Assert
        result.Should().NotBeNull();
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<OkObjectResult>();
        
        var returnedCollection = GetActionResultValue(result);
        returnedCollection.Should().NotBeNull();
        returnedCollection!.Id.Should().Be(collection.Id);
        returnedCollection.Name.Should().Be("Test Collection");
        returnedCollection.Requests.Should().HaveCount(1);
        returnedCollection.Members.Should().HaveCount(1);
        returnedCollection.Workspace.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCollection_ReturnsNotFound_WhenCollectionDoesNotExist()
    {
        // Act
        var result = await _controller.GetCollection(999);

        // Assert
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetCollection_ReturnsNotFound_WhenUserDoesNotHaveAccess()
    {
        // Arrange
        var otherUser = await CreateAdditionalUserAsync("other@example.com");
        var otherWorkspace = await CreateTestWorkspaceAsync(otherUser, "Other Workspace");
        var otherCollection = await CreateTestCollectionAsync(otherUser, otherWorkspace, "Other Collection");

        // Act
        var result = await _controller.GetCollection(otherCollection.Id);

        // Assert
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task PostCollection_CreatesCollection_WhenDataIsValidAndWorkspaceExists()
    {
        // Arrange
        var workspace = await CreateTestWorkspaceAsync(TestUser, "Test Workspace");
        var newCollection = new Collection
        {
            Name = "New Collection",
            Description = "A new test collection",
            WorkspaceId = workspace.Id
        };

        // Act
        var result = await _controller.PostCollection(newCollection);

        // Assert
        result.Should().NotBeNull();
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<CreatedAtActionResult>();
        
        var createdCollection = GetActionResultValue(result);
        createdCollection.Should().NotBeNull();
        createdCollection!.Name.Should().Be("New Collection");
        createdCollection.Description.Should().Be("A new test collection");
        createdCollection.UserId.Should().Be(TestUser.Id);
        createdCollection.WorkspaceId.Should().Be(workspace.Id);
        createdCollection.Id.Should().BeGreaterThan(0);

        // Verify owner membership was created
        var membership = await Context.CollectionMembers.FirstOrDefaultAsync(m => m.CollectionId == createdCollection.Id && m.UserId == TestUser.Id);
        membership.Should().NotBeNull();
        membership!.Role.Should().Be(CollectionRole.Owner);
    }

    [Fact]
    public async Task PostCollection_ReturnsBadRequest_WhenWorkspaceIdIsInvalid()
    {
        // Arrange
        var newCollection = new Collection
        {
            Name = "New Collection",
            WorkspaceId = 0 // Invalid workspace ID
        };

        // Act
        var result = await _controller.PostCollection(newCollection);

        // Assert
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = actionResult as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("WorkspaceId is required");
    }

    [Fact]
    public async Task PostCollection_ReturnsBadRequest_WhenWorkspaceDoesNotExist()
    {
        // Arrange
        var newCollection = new Collection
        {
            Name = "New Collection",
            WorkspaceId = 999 // Non-existent workspace
        };

        // Act
        var result = await _controller.PostCollection(newCollection);

        // Assert
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = actionResult as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("Invalid workspace or workspace does not belong to user");
    }

    [Fact]
    public async Task PostCollection_ReturnsBadRequest_WhenWorkspaceBelongsToAnotherUser()
    {
        // Arrange
        var otherUser = await CreateAdditionalUserAsync("other@example.com");
        var otherWorkspace = await CreateTestWorkspaceAsync(otherUser, "Other Workspace");
        var newCollection = new Collection
        {
            Name = "New Collection",
            WorkspaceId = otherWorkspace.Id
        };

        // Act
        var result = await _controller.PostCollection(newCollection);

        // Assert
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = actionResult as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("Invalid workspace or workspace does not belong to user");
    }

    [Fact]
    public async Task PutCollection_UpdatesCollection_WhenUserIsOwner()
    {
        // Arrange
        var workspace = await CreateTestWorkspaceAsync(TestUser, "Test Workspace");
        var collection = await CreateTestCollectionAsync(TestUser, workspace, "Original Name");
        var updatedCollection = new Collection
        {
            Id = collection.Id,
            Name = "Updated Name",
            Description = "Updated description",
            WorkspaceId = workspace.Id
        };

        // Act
        var result = await _controller.PutCollection(collection.Id, updatedCollection);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify the collection was updated
        var savedCollection = await Context.Collections.FindAsync(collection.Id);
        savedCollection.Should().NotBeNull();
        savedCollection!.Name.Should().Be("Updated Name");
        savedCollection.Description.Should().Be("Updated description");
        savedCollection.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task PutCollection_UpdatesCollection_WhenUserIsAdmin()
    {
        // Arrange
        var workspace = await CreateTestWorkspaceAsync(TestUser, "Test Workspace");
        var collection = await CreateTestCollectionAsync(TestUser, workspace, "Test Collection");
        
        // Add another user as admin
        var adminUser = await CreateAdditionalUserAsync("admin@example.com");
        var adminMembership = new CollectionMember
        {
            CollectionId = collection.Id,
            UserId = adminUser.Id,
            Role = CollectionRole.Admin,
            JoinedAt = DateTime.UtcNow
        };
        Context.CollectionMembers.Add(adminMembership);
        await Context.SaveChangesAsync();
        
        // Switch to admin user's context
        SetupControllerContext(_controller, adminUser.Id);
        
        var updatedCollection = new Collection
        {
            Id = collection.Id,
            Name = "Updated by Admin",
            WorkspaceId = workspace.Id
        };

        // Act
        var result = await _controller.PutCollection(collection.Id, updatedCollection);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task PutCollection_ReturnsForbid_WhenUserIsRegularMember()
    {
        // Arrange
        var workspace = await CreateTestWorkspaceAsync(TestUser, "Test Workspace");
        var collection = await CreateTestCollectionAsync(TestUser, workspace, "Test Collection");
        
        // Add another user as regular member
        var memberUser = await CreateAdditionalUserAsync("member@example.com");
        var membership = new CollectionMember
        {
            CollectionId = collection.Id,
            UserId = memberUser.Id,
            Role = CollectionRole.Member,
            JoinedAt = DateTime.UtcNow
        };
        Context.CollectionMembers.Add(membership);
        await Context.SaveChangesAsync();
        
        // Switch to member user's context
        SetupControllerContext(_controller, memberUser.Id);
        
        var updatedCollection = new Collection
        {
            Id = collection.Id,
            Name = "Updated by Member",
            WorkspaceId = workspace.Id
        };

        // Act
        var result = await _controller.PutCollection(collection.Id, updatedCollection);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task PutCollection_ReturnsBadRequest_WhenIdMismatch()
    {
        // Arrange
        var workspace = await CreateTestWorkspaceAsync(TestUser, "Test Workspace");
        var collection = await CreateTestCollectionAsync(TestUser, workspace, "Test Collection");
        var updatedCollection = new Collection
        {
            Id = collection.Id + 1, // Different ID
            Name = "Updated Name",
            WorkspaceId = workspace.Id
        };

        // Act
        var result = await _controller.PutCollection(collection.Id, updatedCollection);

        // Assert
        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task DeleteCollection_DeletesCollection_WhenUserIsOwner()
    {
        // Arrange
        var workspace = await CreateTestWorkspaceAsync(TestUser, "Test Workspace");
        var collection = await CreateTestCollectionAsync(TestUser, workspace, "Test Collection");
        var request = await CreateTestApiRequestAsync(collection, "Test Request");

        // Act
        var result = await _controller.DeleteCollection(collection.Id);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify collection was deleted (cascade should delete requests too)
        var deletedCollection = await Context.Collections.FindAsync(collection.Id);
        deletedCollection.Should().BeNull();
        
        var deletedRequest = await Context.ApiRequests.FindAsync(request.Id);
        deletedRequest.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCollection_ReturnsForbid_WhenUserIsAdmin()
    {
        // Arrange
        var workspace = await CreateTestWorkspaceAsync(TestUser, "Test Workspace");
        var collection = await CreateTestCollectionAsync(TestUser, workspace, "Test Collection");
        
        // Add another user as admin
        var adminUser = await CreateAdditionalUserAsync("admin@example.com");
        var adminMembership = new CollectionMember
        {
            CollectionId = collection.Id,
            UserId = adminUser.Id,
            Role = CollectionRole.Admin,
            JoinedAt = DateTime.UtcNow
        };
        Context.CollectionMembers.Add(adminMembership);
        await Context.SaveChangesAsync();
        
        // Switch to admin user's context
        SetupControllerContext(_controller, adminUser.Id);

        // Act
        var result = await _controller.DeleteCollection(collection.Id);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeleteCollection_ReturnsForbid_WhenUserIsRegularMember()
    {
        // Arrange
        var workspace = await CreateTestWorkspaceAsync(TestUser, "Test Workspace");
        var collection = await CreateTestCollectionAsync(TestUser, workspace, "Test Collection");
        
        // Add another user as regular member
        var memberUser = await CreateAdditionalUserAsync("member@example.com");
        var membership = new CollectionMember
        {
            CollectionId = collection.Id,
            UserId = memberUser.Id,
            Role = CollectionRole.Member,
            JoinedAt = DateTime.UtcNow
        };
        Context.CollectionMembers.Add(membership);
        await Context.SaveChangesAsync();
        
        // Switch to member user's context
        SetupControllerContext(_controller, memberUser.Id);

        // Act
        var result = await _controller.DeleteCollection(collection.Id);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeleteCollection_ReturnsNotFound_WhenCollectionDoesNotExist()
    {
        // Act
        var result = await _controller.DeleteCollection(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteCollection_ReturnsForbid_WhenWorkspaceBelongsToAnotherUser()
    {
        // Arrange
        var otherUser = await CreateAdditionalUserAsync("other@example.com");
        var otherWorkspace = await CreateTestWorkspaceAsync(otherUser, "Other Workspace");
        var otherCollection = await CreateTestCollectionAsync(otherUser, otherWorkspace, "Other Collection");

        // Act
        var result = await _controller.DeleteCollection(otherCollection.Id);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task CollectionExists_ReturnsTrue_WhenCollectionExistsAndUserOwns()
    {
        // Arrange
        var workspace = await CreateTestWorkspaceAsync(TestUser, "Test Workspace");
        var collection = await CreateTestCollectionAsync(TestUser, workspace, "Test Collection");

        // Act
        var exists = _controller.GetType()
            .GetMethod("CollectionExists", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(_controller, new object[] { collection.Id, TestUser.Id });

        // Assert
        exists.Should().Be(true);
    }

    [Fact]
    public async Task CollectionExists_ReturnsFalse_WhenCollectionDoesNotExist()
    {
        // Act
        var exists = _controller.GetType()
            .GetMethod("CollectionExists", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(_controller, new object[] { 999, TestUser.Id });

        // Assert
        exists.Should().Be(false);
    }

    [Fact]
    public async Task GetCollections_ExcludesCollectionsFromOtherUserWorkspaces_EvenIfUserIsMember()
    {
        // Arrange - User A creates workspace and collection
        var userA = TestUser;
        var workspaceA = await CreateTestWorkspaceAsync(userA, "User A Workspace");
        var collectionA = await CreateTestCollectionAsync(userA, workspaceA, "Collection A");
        
        // User B creates their own workspace
        var userB = await CreateAdditionalUserAsync("userb@example.com");
        var workspaceB = await CreateTestWorkspaceAsync(userB, "User B Workspace");
        
        // Add User B as member to Collection A (in User A's workspace)
        var membership = new CollectionMember
        {
            CollectionId = collectionA.Id,
            UserId = userB.Id,
            Role = CollectionRole.Member,
            JoinedAt = DateTime.UtcNow
        };
        Context.CollectionMembers.Add(membership);
        await Context.SaveChangesAsync();
        
        // Switch to User B's context
        SetupControllerContext(_controller, userB.Id);

        // Act - User B requests collections
        var result = await _controller.GetCollections();

        // Assert - User B should NOT see Collection A because it's in User A's workspace
        result.Should().NotBeNull();
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<OkObjectResult>();
        
        var collections = GetActionResultValue(result);
        collections.Should().NotBeNull();
        collections.Should().BeEmpty(); // User B has no collections in their own workspace
    }
}