using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StampApi.Controllers;
using StampApi.Models;
using StampApi.Tests.TestHelpers;

namespace StampApi.Tests.Controllers;

public class WorkspacesControllerTests : ControllerTestBase, IAsyncLifetime
{
    private WorkspacesController _controller = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _controller = new WorkspacesController(Context);
        SetupControllerContext(_controller);
    }

    public override Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetWorkspaces_ReturnsUserWorkspaces_WhenUserIsAuthenticated()
    {
        // Arrange
        var workspace1 = await CreateTestWorkspaceAsync(TestUser, "Workspace 1");
        var workspace2 = await CreateTestWorkspaceAsync(TestUser, "Workspace 2");
        var collection = await CreateTestCollectionAsync(TestUser, workspace1, "Test Collection");
        
        // Create another user's workspace to ensure isolation
        var otherUser = await CreateAdditionalUserAsync("other@example.com");
        await CreateTestWorkspaceAsync(otherUser, "Other User's Workspace");

        // Act
        var result = await _controller.GetWorkspaces();

        // Assert
        result.Should().NotBeNull();
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<OkObjectResult>();
        
        var workspaces = GetActionResultValue(result);
        workspaces.Should().NotBeNull();
        workspaces.Should().HaveCount(2);
        workspaces.Should().Contain(w => w.Name == "Workspace 1");
        workspaces.Should().Contain(w => w.Name == "Workspace 2");
        workspaces.Should().NotContain(w => w.Name == "Other User's Workspace");
        
        // Verify collections are included
        var workspaceWithCollection = workspaces.First(w => w.Name == "Workspace 1");
        workspaceWithCollection.Collections.Should().HaveCount(1);
        workspaceWithCollection.Collections.First().Name.Should().Be("Test Collection");
    }

    [Fact]
    public async Task GetWorkspaces_ReturnsUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _controller = new WorkspacesController(Context); // No authentication context

        // Act
        var result = await _controller.GetWorkspaces();

        // Assert
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetWorkspace_ReturnsWorkspace_WhenWorkspaceExistsAndBelongsToUser()
    {
        // Arrange
        var workspace = await CreateTestWorkspaceAsync(TestUser, "Test Workspace");
        var collection = await CreateTestCollectionAsync(TestUser, workspace, "Test Collection");
        await CreateTestApiRequestAsync(collection, "Test Request");

        // Act
        var result = await _controller.GetWorkspace(workspace.Id);

        // Assert
        result.Should().NotBeNull();
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<OkObjectResult>();
        
        var returnedWorkspace = GetActionResultValue(result);
        returnedWorkspace.Should().NotBeNull();
        returnedWorkspace!.Id.Should().Be(workspace.Id);
        returnedWorkspace.Name.Should().Be("Test Workspace");
        returnedWorkspace.Collections.Should().HaveCount(1);
        returnedWorkspace.Collections.First().Requests.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetWorkspace_ReturnsNotFound_WhenWorkspaceDoesNotExist()
    {
        // Act
        var result = await _controller.GetWorkspace(999);

        // Assert
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetWorkspace_ReturnsNotFound_WhenWorkspaceBelongsToAnotherUser()
    {
        // Arrange
        var otherUser = await CreateAdditionalUserAsync("other@example.com");
        var otherWorkspace = await CreateTestWorkspaceAsync(otherUser, "Other User's Workspace");

        // Act
        var result = await _controller.GetWorkspace(otherWorkspace.Id);

        // Assert
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task PostWorkspace_CreatesWorkspace_WhenDataIsValid()
    {
        // Arrange
        var newWorkspace = new Workspace
        {
            Name = "New Workspace",
            Description = "A new test workspace"
        };

        // Act
        var result = await _controller.PostWorkspace(newWorkspace);

        // Assert
        result.Should().NotBeNull();
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<CreatedAtActionResult>();
        
        var createdWorkspace = GetActionResultValue(result);
        createdWorkspace.Should().NotBeNull();
        createdWorkspace!.Name.Should().Be("New Workspace");
        createdWorkspace.Description.Should().Be("A new test workspace");
        createdWorkspace.UserId.Should().Be(TestUser.Id);
        createdWorkspace.Id.Should().BeGreaterThan(0);

        // Verify it was saved to the database
        var savedWorkspace = await Context.Workspaces.FindAsync(createdWorkspace.Id);
        savedWorkspace.Should().NotBeNull();
        savedWorkspace!.Name.Should().Be("New Workspace");
    }

    [Fact]
    public async Task PostWorkspace_ReturnsUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _controller = new WorkspacesController(Context); // No authentication context
        var newWorkspace = new Workspace { Name = "Test Workspace" };

        // Act
        var result = await _controller.PostWorkspace(newWorkspace);

        // Assert
        var actionResult = GetActionResultType(result);
        actionResult.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task PutWorkspace_UpdatesWorkspace_WhenDataIsValidAndUserOwnsWorkspace()
    {
        // Arrange
        var workspace = await CreateTestWorkspaceAsync(TestUser, "Original Name");
        var updatedWorkspace = new Workspace
        {
            Id = workspace.Id,
            Name = "Updated Name",
            Description = "Updated description"
        };

        // Act
        var result = await _controller.PutWorkspace(workspace.Id, updatedWorkspace);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify the workspace was updated
        var savedWorkspace = await Context.Workspaces.FindAsync(workspace.Id);
        savedWorkspace.Should().NotBeNull();
        savedWorkspace!.Name.Should().Be("Updated Name");
        savedWorkspace.Description.Should().Be("Updated description");
        savedWorkspace.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task PutWorkspace_ReturnsBadRequest_WhenIdMismatch()
    {
        // Arrange
        var workspace = await CreateTestWorkspaceAsync(TestUser, "Test Workspace");
        var updatedWorkspace = new Workspace
        {
            Id = workspace.Id + 1, // Different ID
            Name = "Updated Name"
        };

        // Act
        var result = await _controller.PutWorkspace(workspace.Id, updatedWorkspace);

        // Assert
        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task PutWorkspace_ReturnsNotFound_WhenWorkspaceDoesNotExist()
    {
        // Arrange
        var updatedWorkspace = new Workspace
        {
            Id = 999,
            Name = "Updated Name"
        };

        // Act
        var result = await _controller.PutWorkspace(999, updatedWorkspace);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task PutWorkspace_ReturnsNotFound_WhenWorkspaceBelongsToAnotherUser()
    {
        // Arrange
        var otherUser = await CreateAdditionalUserAsync("other@example.com");
        var otherWorkspace = await CreateTestWorkspaceAsync(otherUser, "Other User's Workspace");
        var updatedWorkspace = new Workspace
        {
            Id = otherWorkspace.Id,
            Name = "Updated Name"
        };

        // Act
        var result = await _controller.PutWorkspace(otherWorkspace.Id, updatedWorkspace);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteWorkspace_DeletesWorkspace_WhenUserOwnsWorkspaceAndHasMultipleWorkspaces()
    {
        // Arrange
        var workspace1 = await CreateTestWorkspaceAsync(TestUser, "Workspace 1");
        var workspace2 = await CreateTestWorkspaceAsync(TestUser, "Workspace 2");
        var collection = await CreateTestCollectionAsync(TestUser, workspace1, "Test Collection");

        // Act
        var result = await _controller.DeleteWorkspace(workspace1.Id);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify workspace was deleted
        var deletedWorkspace = await Context.Workspaces.FindAsync(workspace1.Id);
        deletedWorkspace.Should().BeNull();

        // Verify collection was moved to remaining workspace
        var movedCollection = await Context.Collections.FindAsync(collection.Id);
        movedCollection.Should().NotBeNull();
        movedCollection!.WorkspaceId.Should().Be(workspace2.Id);
    }

    [Fact]
    public async Task DeleteWorkspace_ReturnsBadRequest_WhenUserHasOnlyOneWorkspace()
    {
        // Arrange
        var workspace = await CreateTestWorkspaceAsync(TestUser, "Only Workspace");

        // Act
        var result = await _controller.DeleteWorkspace(workspace.Id);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("Cannot delete your only workspace. Create another workspace first.");

        // Verify workspace was not deleted
        var stillExistingWorkspace = await Context.Workspaces.FindAsync(workspace.Id);
        stillExistingWorkspace.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteWorkspace_ReturnsNotFound_WhenWorkspaceDoesNotExist()
    {
        // Act
        var result = await _controller.DeleteWorkspace(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteWorkspace_ReturnsNotFound_WhenWorkspaceBelongsToAnotherUser()
    {
        // Arrange
        var otherUser = await CreateAdditionalUserAsync("other@example.com");
        var otherWorkspace = await CreateTestWorkspaceAsync(otherUser, "Other User's Workspace");

        // Act
        var result = await _controller.DeleteWorkspace(otherWorkspace.Id);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteWorkspace_MovesCollectionsToTargetWorkspace_WhenDeletingWorkspaceWithCollections()
    {
        // Arrange
        var workspace1 = await CreateTestWorkspaceAsync(TestUser, "Workspace 1");
        var workspace2 = await CreateTestWorkspaceAsync(TestUser, "Workspace 2");
        var collection1 = await CreateTestCollectionAsync(TestUser, workspace1, "Collection 1");
        var collection2 = await CreateTestCollectionAsync(TestUser, workspace1, "Collection 2");
        var request = await CreateTestApiRequestAsync(collection1, "Test Request");

        // Act
        var result = await _controller.DeleteWorkspace(workspace1.Id);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify workspace was deleted
        var deletedWorkspace = await Context.Workspaces.FindAsync(workspace1.Id);
        deletedWorkspace.Should().BeNull();

        // Verify collections were moved
        var movedCollection1 = await Context.Collections.Include(c => c.Requests).FirstAsync(c => c.Id == collection1.Id);
        var movedCollection2 = await Context.Collections.FirstAsync(c => c.Id == collection2.Id);
        
        movedCollection1.WorkspaceId.Should().Be(workspace2.Id);
        movedCollection2.WorkspaceId.Should().Be(workspace2.Id);
        
        // Verify requests are still associated
        movedCollection1.Requests.Should().HaveCount(1);
        movedCollection1.Requests.First().Name.Should().Be("Test Request");
    }

    [Fact]
    public async Task WorkspaceExists_ReturnsTrue_WhenWorkspaceExistsAndBelongsToUser()
    {
        // Arrange
        var workspace = await CreateTestWorkspaceAsync(TestUser, "Test Workspace");

        // Act
        var exists = _controller.GetType()
            .GetMethod("WorkspaceExists", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(_controller, new object[] { workspace.Id, TestUser.Id });

        // Assert
        exists.Should().Be(true);
    }

    [Fact]
    public async Task WorkspaceExists_ReturnsFalse_WhenWorkspaceDoesNotExist()
    {
        // Act
        var exists = _controller.GetType()
            .GetMethod("WorkspaceExists", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(_controller, new object[] { 999, TestUser.Id });

        // Assert
        exists.Should().Be(false);
    }

    [Fact]
    public async Task WorkspaceExists_ReturnsFalse_WhenWorkspaceBelongsToAnotherUser()
    {
        // Arrange
        var otherUser = await CreateAdditionalUserAsync("other@example.com");
        var otherWorkspace = await CreateTestWorkspaceAsync(otherUser, "Other User's Workspace");

        // Act
        var exists = _controller.GetType()
            .GetMethod("WorkspaceExists", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(_controller, new object[] { otherWorkspace.Id, TestUser.Id });

        // Assert
        exists.Should().Be(false);
    }
}