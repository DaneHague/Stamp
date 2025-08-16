using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StampApi.Data;
using StampApi.Models;

namespace StampApi.Tests.TestHelpers;

public class ControllerTestBase : IAsyncLifetime
{
    protected StampDbContext Context { get; private set; }
    protected ApplicationUser TestUser { get; private set; } = null!;

    public virtual async Task InitializeAsync()
    {
        Context = TestDbContextFactory.CreateInMemoryContext();
        TestUser = await TestDbContextFactory.CreateTestUserAsync(Context);
    }

    protected void SetupControllerContext(ControllerBase controller, int? userId = null)
    {
        var userIdToUse = userId ?? TestUser.Id;
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userIdToUse.ToString()),
            new(ClaimTypes.Email, $"user{userIdToUse}@example.com")
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    protected async Task<ApplicationUser> CreateAdditionalUserAsync(string email = "additional@example.com")
    {
        return await TestDbContextFactory.CreateTestUserAsync(Context, email);
    }

    protected async Task<Workspace> CreateTestWorkspaceAsync(ApplicationUser? user = null, string name = "Test Workspace")
    {
        var userToUse = user ?? TestUser;
        return await TestDbContextFactory.CreateTestWorkspaceAsync(Context, userToUse, name);
    }

    protected async Task<Collection> CreateTestCollectionAsync(ApplicationUser? user = null, Workspace? workspace = null, string name = "Test Collection")
    {
        var userToUse = user ?? TestUser;
        var workspaceToUse = workspace ?? await CreateTestWorkspaceAsync(userToUse);
        return await TestDbContextFactory.CreateTestCollectionAsync(Context, userToUse, workspaceToUse, name);
    }

    protected async Task<ApiRequest> CreateTestApiRequestAsync(Collection? collection = null, string name = "Test Request")
    {
        var collectionToUse = collection ?? await CreateTestCollectionAsync();
        return await TestDbContextFactory.CreateTestApiRequestAsync(Context, collectionToUse, name);
    }

    protected static T GetActionResultValue<T>(ActionResult<T> result)
    {
        if (result.Result is OkObjectResult okResult)
        {
            return (T)okResult.Value!;
        }
        return result.Value!;
    }

    protected static IActionResult GetActionResultType<T>(ActionResult<T> result)
    {
        return result.Result ?? new OkObjectResult(result.Value);
    }

    public virtual Task DisposeAsync()
    {
        Context?.Dispose();
        return Task.CompletedTask;
    }
}