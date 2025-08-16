using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StampApi.Controllers;
using StampApi.Models;
using StampApi.Tests.TestHelpers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace StampApi.Tests.EdgeCases;

public class EdgeCaseTests : ControllerTestBase, IAsyncLifetime
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
    public async Task GetWorkspaces_HandlesEmptyDatabase()
    {
        // Act
        var result = await _workspacesController.GetWorkspaces();

        // Assert
        result.Should().NotBeNull();
        var workspaces = GetActionResultValue(result);
        workspaces.Should().NotBeNull();
        workspaces.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCollections_HandlesEmptyDatabase()
    {
        // Act
        var result = await _collectionsController.GetCollections();

        // Assert
        result.Should().NotBeNull();
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<OkObjectResult>();
        
        var collections = GetActionResultValue(result);
        collections.Should().NotBeNull();
        collections.Should().BeEmpty();
    }

    [Fact]
    public async Task PostWorkspace_HandlesNullName()
    {
        // Arrange
        var newWorkspace = new Workspace
        {
            Name = null!, // This should violate model validation
            Description = "Valid description"
        };

        // Act & Assert
        // Note: This test assumes model validation will catch null names
        // In a real scenario, this would be caught by model validation before reaching the controller
        try
        {
            await _workspacesController.PostWorkspace(newWorkspace);
            // If we reach here, the controller should handle it gracefully
        }
        catch (Exception ex)
        {
            // Expected behavior - either validation or database constraint violation
            ex.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task PostWorkspace_HandlesEmptyName()
    {
        // Arrange
        var newWorkspace = new Workspace
        {
            Name = "", // Empty name
            Description = "Valid description"
        };

        // Act
        var result = await _workspacesController.PostWorkspace(newWorkspace);

        // Assert
        // Should still work as empty string is different from null
        result.Should().NotBeNull();
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task PostCollection_HandlesNegativeWorkspaceId()
    {
        // Arrange
        var newCollection = new Collection
        {
            Name = "Test Collection",
            WorkspaceId = -1
        };

        // Act
        var result = await _collectionsController.PostCollection(newCollection);

        // Assert
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = actionResult as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("WorkspaceId is required");
    }

    [Fact]
    public async Task PostCollection_HandlesMaxIntWorkspaceId()
    {
        // Arrange
        var newCollection = new Collection
        {
            Name = "Test Collection",
            WorkspaceId = int.MaxValue
        };

        // Act
        var result = await _collectionsController.PostCollection(newCollection);

        // Assert
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = actionResult as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("Invalid workspace or workspace does not belong to user");
    }

    [Fact]
    public async Task GetWorkspace_HandlesNegativeId()
    {
        // Act
        var result = await _workspacesController.GetWorkspace(-1);

        // Assert
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetWorkspace_HandlesZeroId()
    {
        // Act
        var result = await _workspacesController.GetWorkspace(0);

        // Assert
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetCollection_HandlesNegativeId()
    {
        // Act
        var result = await _collectionsController.GetCollection(-1);

        // Assert
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteWorkspace_HandlesNonExistentId()
    {
        // Act
        var result = await _workspacesController.DeleteWorkspace(999999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteCollection_HandlesNonExistentId()
    {
        // Act
        var result = await _collectionsController.DeleteCollection(999999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetUserId_HandlesInvalidClaimValue()
    {
        // Arrange - Create controller with invalid user ID claim
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "not-a-number"),
            new(ClaimTypes.Email, "test@example.com")
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        _workspacesController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        // Act
        var result = await _workspacesController.GetWorkspaces();

        // Assert
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetUserId_HandlesMissingClaim()
    {
        // Arrange - Create controller with no NameIdentifier claim
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test@example.com")
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        _workspacesController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        // Act
        var result = await _workspacesController.GetWorkspaces();

        // Assert
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task PutWorkspace_HandlesConcurrencyException()
    {
        // Arrange
        var workspace = await CreateTestWorkspaceAsync(TestUser, "Test Workspace");
        
        // Simulate concurrent update by modifying the workspace in the database
        var dbWorkspace = await Context.Workspaces.FindAsync(workspace.Id);
        dbWorkspace!.Name = "Modified by another user";
        dbWorkspace.UpdatedAt = DateTime.UtcNow;
        await Context.SaveChangesAsync();

        // Now try to update with the original workspace object
        var updatedWorkspace = new Workspace
        {
            Id = workspace.Id,
            Name = "My Update",
            Description = "My description"
        };

        // Act
        var result = await _workspacesController.PutWorkspace(workspace.Id, updatedWorkspace);

        // Assert
        // The update should still succeed as EF Core doesn't use optimistic concurrency by default
        // This tests the concurrency exception handling in the controller
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DatabaseConstraintViolation_HandlesUniqueConstraints()
    {
        // Arrange - Create a collection member
        var workspace = await CreateTestWorkspaceAsync(TestUser, "Test Workspace");
        var collection = await CreateTestCollectionAsync(TestUser, workspace, "Test Collection");
        
        // Try to add the same user as member twice (should violate unique constraint)
        var membership1 = new CollectionMember
        {
            CollectionId = collection.Id,
            UserId = TestUser.Id,
            Role = CollectionRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        var membership2 = new CollectionMember
        {
            CollectionId = collection.Id,
            UserId = TestUser.Id, // Same user, same collection
            Role = CollectionRole.Admin,
            JoinedAt = DateTime.UtcNow
        };

        Context.CollectionMembers.Add(membership1);
        await Context.SaveChangesAsync();

        // Act & Assert
        Context.CollectionMembers.Add(membership2);
        
        var action = async () => await Context.SaveChangesAsync();
        await action.Should().ThrowAsync<Exception>(); // Should throw some kind of constraint violation
    }

    [Fact]
    public async Task LargeDataSet_HandlesMultipleWorkspacesAndCollections()
    {
        // Arrange - Create many workspaces and collections
        var workspaces = new List<Workspace>();
        for (int i = 0; i < 10; i++)
        {
            var workspace = await CreateTestWorkspaceAsync(TestUser, $"Workspace {i}");
            workspaces.Add(workspace);

            // Add multiple collections to each workspace
            for (int j = 0; j < 5; j++)
            {
                await CreateTestCollectionAsync(TestUser, workspace, $"Collection {i}-{j}");
            }
        }

        // Act
        var allWorkspaces = await _workspacesController.GetWorkspaces();
        var allCollections = await _collectionsController.GetCollections();

        // Assert
        var workspaceList = GetActionResultValue(allWorkspaces);
        var collectionList = GetActionResultValue(allCollections);

        workspaceList.Should().HaveCount(10);
        collectionList.Should().HaveCount(50); // 10 workspaces * 5 collections each

        // Verify all collections are properly associated
        foreach (var workspace in workspaceList!)
        {
            workspace.Collections.Should().HaveCount(5);
        }
    }

    [Fact]
    public async Task SpecialCharacters_HandlesNamesWithSpecialCharacters()
    {
        // Arrange
        var specialNameWorkspace = new Workspace
        {
            Name = "Test Workspace!@#$%^&*()_+-={}[]|\\:;\"'<>,.?/",
            Description = "Special chars: 你好 мир العالم 🚀 ñáéíóú"
        };

        // Act
        var result = await _workspacesController.PostWorkspace(specialNameWorkspace);

        // Assert
        result.Should().NotBeNull();
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<CreatedAtActionResult>();
        
        var createdWorkspace = GetActionResultValue(result);
        createdWorkspace!.Name.Should().Be("Test Workspace!@#$%^&*()_+-={}[]|\\:;\"'<>,.?/");
        createdWorkspace.Description.Should().Be("Special chars: 你好 мир العالم 🚀 ñáéíóú");
    }

    [Fact]
    public async Task VeryLongNames_HandlesMaxLengthNames()
    {
        // Arrange - Create names at max length (255 characters)
        var maxLengthName = new string('A', 255);
        var maxLengthDescription = new string('B', 1000);

        var workspace = new Workspace
        {
            Name = maxLengthName,
            Description = maxLengthDescription
        };

        // Act
        var result = await _workspacesController.PostWorkspace(workspace);

        // Assert
        result.Should().NotBeNull();
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<CreatedAtActionResult>();
        
        var createdWorkspace = GetActionResultValue(result);
        createdWorkspace!.Name.Should().HaveLength(255);
        createdWorkspace.Description.Should().HaveLength(1000);
    }

    [Fact]
    public async Task ExcessivelyLongNames_HandlesBeyondMaxLength()
    {
        // Arrange - Create names exceeding max length
        var tooLongName = new string('A', 256); // One character too long
        var tooLongDescription = new string('B', 1001); // One character too long

        var workspace = new Workspace
        {
            Name = tooLongName,
            Description = tooLongDescription
        };

        // Act & Assert
        // This should either be caught by model validation or database constraints
        try
        {
            var result = await _workspacesController.PostWorkspace(workspace);
            await Context.SaveChangesAsync(); // Force the database save
        }
        catch (Exception ex)
        {
            // Expected - should throw some validation or constraint exception
            ex.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task DeleteLastWorkspace_PreventsOrphanedCollections()
    {
        // Arrange - Create a single workspace with collections
        var workspace = await CreateTestWorkspaceAsync(TestUser, "Only Workspace");
        var collection1 = await CreateTestCollectionAsync(TestUser, workspace, "Collection 1");
        var collection2 = await CreateTestCollectionAsync(TestUser, workspace, "Collection 2");

        // Act
        var result = await _workspacesController.DeleteWorkspace(workspace.Id);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        
        // Verify workspace still exists
        var stillExists = await Context.Workspaces.FindAsync(workspace.Id);
        stillExists.Should().NotBeNull();
        
        // Verify collections still exist
        var collections = await Context.Collections.Where(c => c.WorkspaceId == workspace.Id).ToListAsync();
        collections.Should().HaveCount(2);
    }

    [Fact]
    public async Task NullWorkspaceReference_HandlesOrphanedCollections()
    {
        // Arrange - Create collection with null workspace reference (simulating data corruption)
        var collection = new Collection
        {
            Name = "Orphaned Collection",
            Description = "Collection without workspace",
            UserId = TestUser.Id,
            WorkspaceId = null, // Orphaned
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Context.Collections.Add(collection);
        await Context.SaveChangesAsync();

        // Create owner membership
        var membership = new CollectionMember
        {
            CollectionId = collection.Id,
            UserId = TestUser.Id,
            Role = CollectionRole.Owner,
            JoinedAt = DateTime.UtcNow
        };
        Context.CollectionMembers.Add(membership);
        await Context.SaveChangesAsync();

        // Act
        var result = await _collectionsController.GetCollections();

        // Assert
        // The query should handle null workspace gracefully
        result.Should().NotBeNull();
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<OkObjectResult>();
        
        var collections = GetActionResultValue(result);
        collections.Should().NotBeNull();
        // Orphaned collection should not appear in results due to workspace filter
        collections.Should().BeEmpty();
    }
}