using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StampBlazor;
using StampBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient for API communication
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5024/") });

// Register authentication services
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<IdentityAuthenticationService>();
builder.Services.AddScoped<AuthenticatedHttpClient>();

// Register services with authenticated HttpClient injection
builder.Services.AddScoped<WorkspaceService>();
builder.Services.AddScoped<CollectionService>();
builder.Services.AddScoped<RequestService>();
builder.Services.AddScoped<CollectionInviteService>();
builder.Services.AddScoped<CollectionMemberService>();
builder.Services.AddScoped<HttpRequestService>(sp => 
    new HttpRequestService(new HttpClient())); // Separate HttpClient for external requests

await builder.Build().RunAsync();