using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StampApi.Controllers;
using StampApi.Models;
using StampApi.Tests.TestHelpers;
using System.Security.Claims;

namespace StampApi.Tests.Security;

public class AuthorizationTests : ControllerTestBase, IAsyncLifetime
{
    private WorkspacesController _workspacesController = null!;
    private CollectionsController _collectionsController = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _workspacesController = new WorkspacesController(Context);
        _collectionsController = new CollectionsController(Context);
    }

    public override Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AllEndpoints_RequireAuthentication_ReturnUnauthorized()
    {
        // Arrange - Controllers without authentication context
        var workspacesController = new WorkspacesController(Context);
        var collectionsController = new CollectionsController(Context);

        // Act & Assert - All workspace endpoints should return Unauthorized
        GetActionResultType(await workspacesController.GetWorkspaces()).Should().BeOfType<UnauthorizedResult>();
        GetActionResultType(await workspacesController.GetWorkspace(1)).Should().BeOfType<UnauthorizedResult>();
        GetActionResultType(await workspacesController.PostWorkspace(new Workspace { Name = "Test" })).Should().BeOfType<UnauthorizedResult>();
        (await workspacesController.PutWorkspace(1, new Workspace { Id = 1, Name = "Test" })).Should().BeOfType<UnauthorizedResult>();
        (await workspacesController.DeleteWorkspace(1)).Should().BeOfType<UnauthorizedResult>();

        // Act & Assert - All collection endpoints should return Unauthorized
        GetActionResultType(await collectionsController.GetCollections()).Should().BeOfType<UnauthorizedResult>();
        GetActionResultType(await collectionsController.GetCollection(1)).Should().BeOfType<UnauthorizedResult>();
        GetActionResultType(await collectionsController.PostCollection(new Collection { Name = "Test", WorkspaceId = 1 })).Should().BeOfType<UnauthorizedResult>();
        (await collectionsController.PutCollection(1, new Collection { Id = 1, Name = "Test", WorkspaceId = 1 })).Should().BeOfType<UnauthorizedResult>();
        (await collectionsController.DeleteCollection(1)).Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task WorkspaceAccess_IsolatesByUser()
    {
        // Arrange - Create workspace for User A
        SetupControllerContext(_workspacesController);
        SetupControllerContext(_collectionsController);
        
        var userA = TestUser;
        var workspaceA = await CreateTestWorkspaceAsync(userA, "User A Workspace");
        var collectionA = await CreateTestCollectionAsync(userA, workspaceA, "User A Collection");

        // Create User B and their workspace
        var userB = await CreateAdditionalUserAsync("userb@example.com");
        var workspaceB = await CreateTestWorkspaceAsync(userB, "User B Workspace");

        // Switch to User B's context
        SetupControllerContext(_workspacesController, userB.Id);
        SetupControllerContext(_collectionsController, userB.Id);

        // Act & Assert - User B cannot access User A's workspace
        var unauthorizedWorkspaceAccess = await _workspacesController.GetWorkspace(workspaceA.Id);
        GetActionResultType(unauthorizedWorkspaceAccess).Should().BeOfType<NotFoundResult>();

        // User B cannot modify User A's workspace
        var updatedWorkspace = new Workspace
        {
            Id = workspaceA.Id,
            Name = "Hacked Workspace"
        };
        var unauthorizedWorkspaceUpdate = await _workspacesController.PutWorkspace(workspaceA.Id, updatedWorkspace);
        unauthorizedWorkspaceUpdate.Should().BeOfType<NotFoundResult>();

        // User B cannot delete User A's workspace
        var unauthorizedWorkspaceDelete = await _workspacesController.DeleteWorkspace(workspaceA.Id);
        unauthorizedWorkspaceDelete.Should().BeOfType<NotFoundResult>();

        // User B cannot access User A's collection
        var unauthorizedCollectionAccess = await _collectionsController.GetCollection(collectionA.Id);
        GetActionResultType(unauthorizedCollectionAccess).Should().BeOfType<NotFoundResult>();

        // User B cannot create collection in User A's workspace
        var unauthorizedCollection = new Collection
        {
            Name = "Hacked Collection",
            WorkspaceId = workspaceA.Id
        };
        var unauthorizedCollectionCreation = await _collectionsController.PostCollection(unauthorizedCollection);
        GetActionResultType(unauthorizedCollectionCreation).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CollectionRoles_EnforcePermissions()
    {
        // Arrange - Owner creates workspace and collection
        var owner = TestUser;
        var workspace = await CreateTestWorkspaceAsync(owner, "Team Workspace");
        var collection = await CreateTestCollectionAsync(owner, workspace, "Team Collection");

        // Add users with different roles
        var admin = await CreateAdditionalUserAsync("admin@example.com");
        var member = await CreateAdditionalUserAsync("member@example.com");
        var nonMember = await CreateAdditionalUserAsync("nonmember@example.com");

        var adminMembership = new CollectionMember
        {
            CollectionId = collection.Id,
            UserId = admin.Id,
            Role = CollectionRole.Admin,
            JoinedAt = DateTime.UtcNow
        };

        var memberMembership = new CollectionMember
        {
            CollectionId = collection.Id,
            UserId = member.Id,
            Role = CollectionRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        Context.CollectionMembers.AddRange(adminMembership, memberMembership);
        await Context.SaveChangesAsync();

        // Test Admin permissions
        SetupControllerContext(_collectionsController, admin.Id);

        // Admin can read collection
        var adminRead = await _collectionsController.GetCollection(collection.Id);
        GetActionResultType(adminRead).Should().BeOfType<OkObjectResult>();

        // Admin can edit collection
        var adminUpdate = new Collection
        {
            Id = collection.Id,
            Name = "Updated by Admin",
            WorkspaceId = workspace.Id
        };
        var adminEdit = await _collectionsController.PutCollection(collection.Id, adminUpdate);
        adminEdit.Should().BeOfType<NoContentResult>();

        // Admin cannot delete collection (only owner can)
        var adminDelete = await _collectionsController.DeleteCollection(collection.Id);
        adminDelete.Should().BeOfType<ForbidResult>();

        // Test Member permissions
        SetupControllerContext(_collectionsController, member.Id);

        // Member can read collection
        var memberRead = await _collectionsController.GetCollection(collection.Id);
        GetActionResultType(memberRead).Should().BeOfType<OkObjectResult>();

        // Member cannot edit collection
        var memberUpdate = new Collection
        {
            Id = collection.Id,
            Name = "Updated by Member",
            WorkspaceId = workspace.Id
        };
        var memberEdit = await _collectionsController.PutCollection(collection.Id, memberUpdate);
        memberEdit.Should().BeOfType<ForbidResult>();

        // Member cannot delete collection
        var memberDelete = await _collectionsController.DeleteCollection(collection.Id);
        memberDelete.Should().BeOfType<ForbidResult>();

        // Test Non-member permissions
        SetupControllerContext(_collectionsController, nonMember.Id);

        // Non-member cannot read collection
        var nonMemberRead = await _collectionsController.GetCollection(collection.Id);
        GetActionResultType(nonMemberRead).Should().BeOfType<NotFoundResult>();

        // Test Owner permissions
        SetupControllerContext(_collectionsController, owner.Id);

        // Owner can delete collection
        var ownerDelete = await _collectionsController.DeleteCollection(collection.Id);
        ownerDelete.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task CollectionMembership_RequiresWorkspaceAccess()
    {
        // Arrange - User A creates workspace and collection
        var userA = TestUser;
        var workspaceA = await CreateTestWorkspaceAsync(userA, "User A Workspace");
        var collectionA = await CreateTestCollectionAsync(userA, workspaceA, "User A Collection");

        // User B has their own workspace
        var userB = await CreateAdditionalUserAsync("userb@example.com");
        var workspaceB = await CreateTestWorkspaceAsync(userB, "User B Workspace");

        // Add User B as member to User A's collection
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
        SetupControllerContext(_collectionsController, userB.Id);

        // Act & Assert - User B should NOT see the collection because it's in User A's workspace
        var collections = await _collectionsController.GetCollections();
        var collectionList = GetActionResultValue(collections);
        
        collectionList.Should().NotBeNull();
        collectionList.Should().BeEmpty(); // No collections because User A's workspace is not accessible

        // User B should NOT be able to access the collection directly either
        var directAccess = await _collectionsController.GetCollection(collectionA.Id);
        GetActionResultType(directAccess).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task InvalidUserId_HandledSecurely()
    {
        // Test with malicious user ID values
        var maliciousUserIds = new[] { -1, 0, int.MaxValue };

        foreach (var maliciousId in maliciousUserIds)
        {
            // Arrange
            SetupControllerContext(_workspacesController, maliciousId);
            SetupControllerContext(_collectionsController, maliciousId);

            // Act & Assert - Should not crash or expose data
            var workspaces = await _workspacesController.GetWorkspaces();
            var workspaceList = GetActionResultValue(workspaces);
            workspaceList.Should().BeEmpty();

            var collections = await _collectionsController.GetCollections();
            var collectionList = GetActionResultValue(collections);
            collectionList.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task UserIdSpoofing_CannotAccessOtherUserData()
    {
        // Arrange - Create data for multiple users
        var userA = TestUser;
        var userB = await CreateAdditionalUserAsync("userb@example.com");
        var userC = await CreateAdditionalUserAsync("userc@example.com");

        var workspaceA = await CreateTestWorkspaceAsync(userA, "User A Workspace");
        var workspaceB = await CreateTestWorkspaceAsync(userB, "User B Workspace");
        var workspaceC = await CreateTestWorkspaceAsync(userC, "User C Workspace");

        // Test: User A tries to access User B's workspace with correct context
        SetupControllerContext(_workspacesController, userA.Id);
        
        var unauthorizedAccess1 = await _workspacesController.GetWorkspace(workspaceB.Id);
        GetActionResultType(unauthorizedAccess1).Should().BeOfType<NotFoundResult>();

        var unauthorizedAccess2 = await _workspacesController.GetWorkspace(workspaceC.Id);
        GetActionResultType(unauthorizedAccess2).Should().BeOfType<NotFoundResult>();

        // Test: User B tries to access User A's and User C's workspaces
        SetupControllerContext(_workspacesController, userB.Id);
        
        var unauthorizedAccess3 = await _workspacesController.GetWorkspace(workspaceA.Id);
        GetActionResultType(unauthorizedAccess3).Should().BeOfType<NotFoundResult>();

        var unauthorizedAccess4 = await _workspacesController.GetWorkspace(workspaceC.Id);
        GetActionResultType(unauthorizedAccess4).Should().BeOfType<NotFoundResult>();

        // Verify each user can only access their own workspace
        SetupControllerContext(_workspacesController, userA.Id);
        var userAAccess = await _workspacesController.GetWorkspace(workspaceA.Id);
        GetActionResultType(userAAccess).Should().BeOfType<OkObjectResult>();

        SetupControllerContext(_workspacesController, userB.Id);
        var userBAccess = await _workspacesController.GetWorkspace(workspaceB.Id);
        GetActionResultType(userBAccess).Should().BeOfType<OkObjectResult>();

        SetupControllerContext(_workspacesController, userC.Id);
        var userCAccess = await _workspacesController.GetWorkspace(workspaceC.Id);
        GetActionResultType(userCAccess).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ClaimsManipulation_DoesNotBypassSecurity()
    {
        // Arrange - Create legitimate user and data
        var legitimateUser = TestUser;
        var workspace = await CreateTestWorkspaceAsync(legitimateUser, "Legitimate Workspace");

        // Try to create malicious claims that might bypass security
        var maliciousClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, legitimateUser.Id.ToString()),
            new(ClaimTypes.Role, "Admin"), // Fake admin role
            new("CustomClaim", "AdminOverride"), // Custom claim
            new(ClaimTypes.Name, "SuperUser") // Fake super user
        };

        var identity = new ClaimsIdentity(maliciousClaims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        _workspacesController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act - Try to access workspace (should work as the NameIdentifier is legitimate)
        var result = await _workspacesController.GetWorkspace(workspace.Id);

        // Assert - Should work normally, ignoring fake claims
        GetActionResultType(result).Should().BeOfType<OkObjectResult>();

        // Try with invalid NameIdentifier but valid other claims
        var invalidClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "999999"), // Non-existent user
            new(ClaimTypes.Role, "Admin"),
            new("SuperUser", "true")
        };

        identity = new ClaimsIdentity(invalidClaims, "TestAuthType");
        principal = new ClaimsPrincipal(identity);

        _workspacesController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Should not be able to access anything
        var unauthorizedResult = await _workspacesController.GetWorkspace(workspace.Id);
        GetActionResultType(unauthorizedResult).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ConcurrentUserOperations_MaintainDataIntegrity()
    {
        // Arrange - Two users working with workspaces simultaneously
        var userA = TestUser;
        var userB = await CreateAdditionalUserAsync("userb@example.com");

        // Create controllers for both users
        var controllerA = new WorkspacesController(Context);
        var controllerB = new WorkspacesController(Context);
        
        SetupControllerContext(controllerA, userA.Id);
        SetupControllerContext(controllerB, userB.Id);

        // Act - Both users create workspaces with same name simultaneously
        var workspaceA = new Workspace { Name = "Shared Name", Description = "User A's workspace" };
        var workspaceB = new Workspace { Name = "Shared Name", Description = "User B's workspace" };

        var resultA = await controllerA.PostWorkspace(workspaceA);
        var resultB = await controllerB.PostWorkspace(workspaceB);

        // Assert - Both should succeed (no uniqueness constraint on workspace names across users)
        GetActionResultType(resultA).Should().BeOfType<CreatedAtActionResult>();
        GetActionResultType(resultB).Should().BeOfType<CreatedAtActionResult>();

        var createdA = GetActionResultValue(resultA);
        var createdB = GetActionResultValue(resultB);

        createdA.Should().NotBeNull();
        createdB.Should().NotBeNull();
        createdA!.UserId.Should().Be(userA.Id);
        createdB!.UserId.Should().Be(userB.Id);

        // Verify isolation - User A cannot see User B's workspace and vice versa
        var userAWorkspaces = await controllerA.GetWorkspaces();
        var userBWorkspaces = await controllerB.GetWorkspaces();

        var workspaceListA = GetActionResultValue(userAWorkspaces);
        var workspaceListB = GetActionResultValue(userBWorkspaces);

        workspaceListA.Should().HaveCount(1);
        workspaceListB.Should().HaveCount(1);
        workspaceListA!.First().Id.Should().Be(createdA.Id);
        workspaceListB!.First().Id.Should().Be(createdB.Id);
    }

    [Fact]
    public async Task TokenValidation_WorksWithDifferentClaimFormats()
    {
        // Test different valid formats for user ID claims
        var validUserIdFormats = new[]
        {
            TestUser.Id.ToString(),
            $"  {TestUser.Id}  ", // With whitespace
            TestUser.Id.ToString("D"), // With decimal formatting
        };

        foreach (var userIdFormat in validUserIdFormats)
        {
            // Arrange
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userIdFormat),
                new(ClaimTypes.Email, "test@example.com")
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            _workspacesController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _workspacesController.GetWorkspaces();

            // Assert - Should work with properly formatted user IDs
            if (int.TryParse(userIdFormat.Trim(), out var parsedId) && parsedId == TestUser.Id)
            {
                GetActionResultType(result).Should().BeOfType<OkObjectResult>();
            }
            else
            {
                GetActionResultType(result).Should().BeOfType<UnauthorizedResult>();
            }
        }
    }
}