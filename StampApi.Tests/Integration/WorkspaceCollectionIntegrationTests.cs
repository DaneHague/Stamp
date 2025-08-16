using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StampApi.Controllers;
using StampApi.Models;
using StampApi.Tests.TestHelpers;

namespace StampApi.Tests.Integration;

public class WorkspaceCollectionIntegrationTests : ControllerTestBase, IAsyncLifetime
{
    private WorkspacesController _workspacesController = null!;
    private CollectionsController _collectionsController = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _workspacesController = new WorkspacesController(Context);
        _collectionsController = new CollectionsController(Context);
        SetupControllerContext(_workspacesController);
        SetupControllerContext(_collectionsController);
    }

    public override Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CompleteWorkflow_CreateWorkspaceAndCollections_ShouldWorkEndToEnd()
    {
        // 1. Create a workspace
        var newWorkspace = new Workspace
        {
            Name = "My API Project",
            Description = "A project for testing APIs"
        };

        var workspaceResult = await _workspacesController.PostWorkspace(newWorkspace);
        var createdWorkspace = GetActionResultValue(workspaceResult);
        createdWorkspace.Should().NotBeNull();

        // 2. Create collections in the workspace
        var collection1 = new Collection
        {
            Name = "User Management",
            Description = "APIs for user operations",
            WorkspaceId = createdWorkspace!.Id
        };

        var collection2 = new Collection
        {
            Name = "Product Catalog",
            Description = "APIs for product management",
            WorkspaceId = createdWorkspace.Id
        };

        var collection1Result = await _collectionsController.PostCollection(collection1);
        var collection2Result = await _collectionsController.PostCollection(collection2);

        var createdCollection1 = GetActionResultValue(collection1Result);
        var createdCollection2 = GetActionResultValue(collection2Result);

        createdCollection1.Should().NotBeNull();
        createdCollection2.Should().NotBeNull();

        // 3. Verify workspace contains both collections
        var workspaceWithCollections = await _workspacesController.GetWorkspace(createdWorkspace.Id);
        var workspace = GetActionResultValue(workspaceWithCollections);
        
        workspace.Should().NotBeNull();
        workspace!.Collections.Should().HaveCount(2);
        workspace.Collections.Should().Contain(c => c.Name == "User Management");
        workspace.Collections.Should().Contain(c => c.Name == "Product Catalog");

        // 4. Filter collections by workspace
        var filteredCollections = await _collectionsController.GetCollections(createdWorkspace.Id);
        var collections = GetActionResultValue(filteredCollections);
        
        collections.Should().NotBeNull();
        collections.Should().HaveCount(2);

        // 5. Update workspace name
        var updatedWorkspace = new Workspace
        {
            Id = createdWorkspace.Id,
            Name = "Updated API Project",
            Description = "Updated description"
        };

        var updateResult = await _workspacesController.PutWorkspace(createdWorkspace.Id, updatedWorkspace);
        updateResult.Should().BeOfType<NoContentResult>();

        // 6. Verify collections still belong to updated workspace
        var updatedWorkspaceData = await _workspacesController.GetWorkspace(createdWorkspace.Id);
        var finalWorkspace = GetActionResultValue(updatedWorkspaceData);
        
        finalWorkspace.Should().NotBeNull();
        finalWorkspace!.Name.Should().Be("Updated API Project");
        finalWorkspace.Collections.Should().HaveCount(2);
    }

    [Fact]
    public async Task WorkspaceDeletion_MovesCollectionsToAnotherWorkspace()
    {
        // 1. Create two workspaces
        var workspace1 = new Workspace { Name = "Workspace 1", Description = "First workspace" };
        var workspace2 = new Workspace { Name = "Workspace 2", Description = "Second workspace" };

        var workspace1Result = await _workspacesController.PostWorkspace(workspace1);
        var workspace2Result = await _workspacesController.PostWorkspace(workspace2);

        var createdWorkspace1 = GetActionResultValue(workspace1Result);
        var createdWorkspace2 = GetActionResultValue(workspace2Result);

        // 2. Create collections in first workspace
        var collection1 = new Collection { Name = "Collection 1", WorkspaceId = createdWorkspace1!.Id };
        var collection2 = new Collection { Name = "Collection 2", WorkspaceId = createdWorkspace1.Id };

        await _collectionsController.PostCollection(collection1);
        await _collectionsController.PostCollection(collection2);

        // 3. Add requests to collections
        var testCollection1 = await Context.Collections.FirstAsync(c => c.Name == "Collection 1");
        var testCollection2 = await Context.Collections.FirstAsync(c => c.Name == "Collection 2");
        
        await CreateTestApiRequestAsync(testCollection1, "Request 1");
        await CreateTestApiRequestAsync(testCollection1, "Request 2");
        await CreateTestApiRequestAsync(testCollection2, "Request 3");

        // 4. Delete first workspace
        var deleteResult = await _workspacesController.DeleteWorkspace(createdWorkspace1.Id);
        deleteResult.Should().BeOfType<NoContentResult>();

        // 5. Verify collections moved to second workspace
        var movedCollections = await Context.Collections
            .Include(c => c.Requests)
            .Where(c => c.WorkspaceId == createdWorkspace2!.Id)
            .ToListAsync();

        movedCollections.Should().HaveCount(2);
        movedCollections.Should().Contain(c => c.Name == "Collection 1");
        movedCollections.Should().Contain(c => c.Name == "Collection 2");

        // 6. Verify requests are still intact
        var collection1WithRequests = movedCollections.First(c => c.Name == "Collection 1");
        var collection2WithRequests = movedCollections.First(c => c.Name == "Collection 2");

        collection1WithRequests.Requests.Should().HaveCount(2);
        collection2WithRequests.Requests.Should().HaveCount(1);

        // 7. Verify first workspace is deleted
        var deletedWorkspace = await Context.Workspaces.FindAsync(createdWorkspace1.Id);
        deletedWorkspace.Should().BeNull();
    }

    [Fact]
    public async Task CollectionMembership_WorksAcrossWorkspaceOperations()
    {
        // 1. Create workspace and collection
        var workspace = await CreateTestWorkspaceAsync(TestUser, "Shared Workspace");
        var collection = await CreateTestCollectionAsync(TestUser, workspace, "Shared Collection");

        // 2. Add another user as member
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

        // 3. Switch to member user context
        SetupControllerContext(_collectionsController, memberUser.Id);

        // 4. Member should be able to read collection
        var collectionResult = await _collectionsController.GetCollection(collection.Id);
        GetActionResultType(collectionResult).Should().BeOfType<OkObjectResult>();

        // 5. But member should not be able to edit collection
        var updatedCollection = new Collection
        {
            Id = collection.Id,
            Name = "Updated by Member",
            WorkspaceId = workspace.Id
        };

        var editResult = await _collectionsController.PutCollection(collection.Id, updatedCollection);
        editResult.Should().BeOfType<ForbidResult>();

        // 6. And member should not be able to delete collection
        var deleteResult = await _collectionsController.DeleteCollection(collection.Id);
        deleteResult.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task WorkspaceIsolation_EnsuresUserCannotAccessOtherWorkspaces()
    {
        // 1. Create workspace and collection as User A
        var userA = TestUser;
        var workspaceA = await CreateTestWorkspaceAsync(userA, "User A Workspace");
        var collectionA = await CreateTestCollectionAsync(userA, workspaceA, "User A Collection");

        // 2. Create User B and their workspace
        var userB = await CreateAdditionalUserAsync("userb@example.com");
        SetupControllerContext(_workspacesController, userB.Id);
        SetupControllerContext(_collectionsController, userB.Id);

        var workspaceB = new Workspace { Name = "User B Workspace", Description = "User B's workspace" };
        var workspaceBResult = await _workspacesController.PostWorkspace(workspaceB);
        var createdWorkspaceB = GetActionResultValue(workspaceBResult);

        // 3. User B should not see User A's workspace
        var userBWorkspaces = await _workspacesController.GetWorkspaces();
        var workspaces = GetActionResultValue(userBWorkspaces);
        
        workspaces.Should().HaveCount(1);
        workspaces.First().Name.Should().Be("User B Workspace");

        // 4. User B should not be able to access User A's workspace directly
        var unauthorizedAccess = await _workspacesController.GetWorkspace(workspaceA.Id);
        GetActionResultType(unauthorizedAccess).Should().BeOfType<NotFoundResult>();

        // 5. User B should not be able to access User A's collection
        var unauthorizedCollectionAccess = await _collectionsController.GetCollection(collectionA.Id);
        GetActionResultType(unauthorizedCollectionAccess).Should().BeOfType<NotFoundResult>();

        // 6. User B should not be able to create collection in User A's workspace
        var unauthorizedCollectionCreation = new Collection
        {
            Name = "Unauthorized Collection",
            WorkspaceId = workspaceA.Id
        };

        var creationResult = await _collectionsController.PostCollection(unauthorizedCollectionCreation);
        GetActionResultType(creationResult).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CascadeDelete_CollectionDeletionRemovesRequests()
    {
        // 1. Create workspace, collection, and requests
        var workspace = await CreateTestWorkspaceAsync(TestUser, "Test Workspace");
        var collection = await CreateTestCollectionAsync(TestUser, workspace, "Test Collection");
        
        var request1 = await CreateTestApiRequestAsync(collection, "Request 1");
        var request2 = await CreateTestApiRequestAsync(collection, "Request 2");
        var request3 = await CreateTestApiRequestAsync(collection, "Request 3");

        // 2. Verify requests exist
        var existingRequests = await Context.ApiRequests.Where(r => r.CollectionId == collection.Id).ToListAsync();
        existingRequests.Should().HaveCount(3);

        // 3. Delete collection
        var deleteResult = await _collectionsController.DeleteCollection(collection.Id);
        deleteResult.Should().BeOfType<NoContentResult>();

        // 4. Verify collection is deleted
        var deletedCollection = await Context.Collections.FindAsync(collection.Id);
        deletedCollection.Should().BeNull();

        // 5. Verify all requests are also deleted due to cascade
        var remainingRequests = await Context.ApiRequests.Where(r => r.CollectionId == collection.Id).ToListAsync();
        remainingRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task MultiUserWorkflow_SharedCollectionManagement()
    {
        // 1. Owner creates workspace and collection
        var owner = TestUser;
        var workspace = await CreateTestWorkspaceAsync(owner, "Team Workspace");
        var collection = await CreateTestCollectionAsync(owner, workspace, "Team Collection");

        // 2. Add admin user
        var adminUser = await CreateAdditionalUserAsync("admin@example.com");
        var adminMembership = new CollectionMember
        {
            CollectionId = collection.Id,
            UserId = adminUser.Id,
            Role = CollectionRole.Admin,
            JoinedAt = DateTime.UtcNow
        };
        Context.CollectionMembers.Add(adminMembership);

        // 3. Add regular member
        var memberUser = await CreateAdditionalUserAsync("member@example.com");
        var memberMembership = new CollectionMember
        {
            CollectionId = collection.Id,
            UserId = memberUser.Id,
            Role = CollectionRole.Member,
            JoinedAt = DateTime.UtcNow
        };
        Context.CollectionMembers.Add(memberMembership);
        await Context.SaveChangesAsync();

        // 4. Test admin can edit collection
        SetupControllerContext(_collectionsController, adminUser.Id);
        var adminUpdate = new Collection
        {
            Id = collection.Id,
            Name = "Updated by Admin",
            WorkspaceId = workspace.Id
        };

        var adminUpdateResult = await _collectionsController.PutCollection(collection.Id, adminUpdate);
        adminUpdateResult.Should().BeOfType<NoContentResult>();

        // 5. Test member cannot edit collection
        SetupControllerContext(_collectionsController, memberUser.Id);
        var memberUpdate = new Collection
        {
            Id = collection.Id,
            Name = "Updated by Member",
            WorkspaceId = workspace.Id
        };

        var memberUpdateResult = await _collectionsController.PutCollection(collection.Id, memberUpdate);
        memberUpdateResult.Should().BeOfType<ForbidResult>();

        // 6. Test only owner can delete collection
        var adminDeleteResult = await _collectionsController.DeleteCollection(collection.Id);
        adminDeleteResult.Should().BeOfType<ForbidResult>();

        SetupControllerContext(_collectionsController, owner.Id);
        var ownerDeleteResult = await _collectionsController.DeleteCollection(collection.Id);
        ownerDeleteResult.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task WorkspaceCollectionFiltering_WorksCorrectlyWithMultipleWorkspaces()
    {
        // 1. Create multiple workspaces
        var workspace1 = await CreateTestWorkspaceAsync(TestUser, "Development");
        var workspace2 = await CreateTestWorkspaceAsync(TestUser, "Testing");
        var workspace3 = await CreateTestWorkspaceAsync(TestUser, "Production");

        // 2. Create collections in each workspace
        var devCollection1 = await CreateTestCollectionAsync(TestUser, workspace1, "Dev API");
        var devCollection2 = await CreateTestCollectionAsync(TestUser, workspace1, "Dev Utils");
        var testCollection = await CreateTestCollectionAsync(TestUser, workspace2, "Test API");
        var prodCollection1 = await CreateTestCollectionAsync(TestUser, workspace3, "Prod API");
        var prodCollection2 = await CreateTestCollectionAsync(TestUser, workspace3, "Prod Monitoring");

        // 3. Test filtering by workspace1
        var workspace1Collections = await _collectionsController.GetCollections(workspace1.Id);
        var collections1 = GetActionResultValue(workspace1Collections);
        
        collections1.Should().HaveCount(2);
        collections1.Should().Contain(c => c.Name == "Dev API");
        collections1.Should().Contain(c => c.Name == "Dev Utils");

        // 4. Test filtering by workspace2
        var workspace2Collections = await _collectionsController.GetCollections(workspace2.Id);
        var collections2 = GetActionResultValue(workspace2Collections);
        
        collections2.Should().HaveCount(1);
        collections2.First().Name.Should().Be("Test API");

        // 5. Test filtering by workspace3
        var workspace3Collections = await _collectionsController.GetCollections(workspace3.Id);
        var collections3 = GetActionResultValue(workspace3Collections);
        
        collections3.Should().HaveCount(2);
        collections3.Should().Contain(c => c.Name == "Prod API");
        collections3.Should().Contain(c => c.Name == "Prod Monitoring");

        // 6. Test getting all collections (no filter)
        var allCollections = await _collectionsController.GetCollections();
        var allCollectionsList = GetActionResultValue(allCollections);
        
        allCollectionsList.Should().HaveCount(5);
    }
}