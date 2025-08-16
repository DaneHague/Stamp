# Stamp - Technical Architecture Documentation

## System Overview

Stamp is a modern, collaborative API client built using a clean client-server architecture with .NET 8 and Blazor WebAssembly. The system is designed to provide a robust, scalable, and user-friendly platform for API development, testing, and collaboration.

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Browser Environment                       │
├─────────────────────────────────────────────────────────────────┤
│  Blazor WebAssembly Client (StampBlazor)                      │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   Components    │  │    Services     │  │     Models      │ │
│  │  - RequestBuild │  │  - AuthService  │  │  - ApiRequest   │ │
│  │  - Workspace    │  │  - HttpService  │  │  - Collection   │ │
│  │  - Collections  │  │  - StateManage  │  │  - Workspace    │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────────┬───────────────────────────────────────┘
                          │ HTTPS/REST API
                          │ JWT Authentication
┌─────────────────────────▼───────────────────────────────────────┐
│                    Server Environment                           │
├─────────────────────────────────────────────────────────────────┤
│  ASP.NET Core Web API (StampApi)                               │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   Controllers   │  │     Models      │  │   Data Layer    │ │
│  │  - Workspaces   │  │  - ApplicationU │  │  - DbContext    │ │
│  │  - Collections  │  │  - Collection   │  │  - Migrations   │ │
│  │  - Requests     │  │  - ApiRequest   │  │  - Entities     │ │
│  │  - Auth         │  │  - User Models  │  │                 │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────────┬───────────────────────────────────────┘
                          │ Entity Framework Core
                          │ Code-First Migrations
┌─────────────────────────▼───────────────────────────────────────┐
│                    Database Layer                               │
├─────────────────────────────────────────────────────────────────┤
│  SQLite Database (Development) / SQL Server (Production)       │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │  Tables: ApplicationUsers, Workspaces, Collections,        │ │
│  │          ApiRequests, CollectionMembers, CollectionInvites │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## Client-Server Communication

### Data Flow Architecture

The application follows a unidirectional data flow pattern with clear separation of concerns:

```
┌──────────────┐    HTTP/REST     ┌──────────────┐    EF Core      ┌──────────────┐
│   Blazor     │◄────────────────►│   ASP.NET    │◄───────────────►│   Database   │
│  Components  │   JWT Auth       │ Controllers  │   Queries       │   (SQLite)   │
└──────────────┘                  └──────────────┘                 └──────────────┘
       ▲                                 ▲
       │                                 │
       ▼                                 ▼
┌──────────────┐                  ┌──────────────┐
│   Services   │                  │    Models    │
│  Layer       │                  │  & DTOs      │
└──────────────┘                  └──────────────┘
```

### Request/Response Patterns

#### Authentication Flow
```
1. User Input (Email/Password or Google)
   ↓
2. Frontend Auth Service
   ↓
3. API Authentication Controller
   ↓
4. Identity Verification
   ↓
5. JWT Token Generation
   ↓
6. Token Storage (localStorage)
   ↓
7. Authenticated API Calls
```

#### API Request Lifecycle
```
1. User configures HTTP request in RequestBuilder
   ↓
2. Request execution via HttpRequestService
   ↓
3. Direct HTTP call to target API (not through Stamp API)
   ↓
4. Response processing and display
   ↓
5. Optional: Save request to collection via Stamp API
```

#### Data Management Flow
```
1. Component requests data
   ↓
2. Service layer handles API communication
   ↓
3. Authenticated HTTP client adds JWT token
   ↓
4. API Controller validates token and permissions
   ↓
5. Entity Framework executes database operations
   ↓
6. Response serialization and return
   ↓
7. Component state updates and re-rendering
```

## Backend Architecture (StampApi)

### Technology Stack
- **Framework**: ASP.NET Core 8 Web API
- **Database**: SQLite (development) / SQL Server (production)
- **ORM**: Entity Framework Core 8
- **Authentication**: ASP.NET Core Identity + JWT Bearer
- **API Design**: RESTful principles

### Controller Architecture

#### Controller Hierarchy
```
ControllerBase
├── AuthController (Currently disabled)
├── IdentityAuthController (Active authentication)
├── WorkspacesController
├── CollectionsController
├── RequestsController
├── CollectionMembersController
└── CollectionInvitesController
```

#### Authentication & Authorization
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize] // All controllers except auth require authentication
public class WorkspacesController : ControllerBase
{
    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
```

### Database Design

#### Entity Relationship Diagram
```
ApplicationUser (ASP.NET Identity)
├── Id (int, PK)
├── UserName (string)
├── Email (string)
├── GoogleId (string, nullable)
├── AvatarUrl (string, nullable)
├── CreatedAt (DateTime)
└── LastLoginAt (DateTime)
    │
    │ 1:N
    ▼
Workspace
├── Id (int, PK)
├── Name (string)
├── Description (string, nullable)
├── CreatedAt (DateTime)
├── UpdatedAt (DateTime)
└── UserId (int, FK)
    │
    │ 1:N
    ▼
Collection
├── Id (int, PK)
├── Name (string)
├── Description (string, nullable)
├── CreatedAt (DateTime)
├── UpdatedAt (DateTime)
├── UserId (int, FK, nullable)
└── WorkspaceId (int, FK, nullable)
    │
    ├── 1:N ──────────────────────┐
    ▼                            │
ApiRequest                      │ 1:N
├── Id (int, PK)                ▼
├── Name (string)           CollectionMember
├── Url (string)            ├── Id (int, PK)
├── Method (string)         ├── CollectionId (int, FK)
├── Headers (string, JSON)  ├── UserId (int, FK)
├── Body (string, nullable) ├── Role (enum: Owner/Admin/Member)
├── QueryParams (string)    └── JoinedAt (DateTime)
├── Authentication (string)     │
├── CreatedAt (DateTime)        │ 1:N
├── UpdatedAt (DateTime)        ▼
└── CollectionId (int, FK)  CollectionInvite
                           ├── Id (int, PK)
                           ├── CollectionId (int, FK)
                           ├── InvitedByUserId (int, FK)
                           ├── InvitedEmail (string)
                           ├── Role (enum)
                           ├── InviteToken (string, unique)
                           ├── Status (enum: Pending/Accepted/Declined/Expired/Cancelled)
                           ├── CreatedAt (DateTime)
                           ├── ExpiresAt (DateTime)
                           ├── AcceptedAt (DateTime, nullable)
                           └── AcceptedByUserId (int, FK, nullable)
```

#### Database Constraints and Indexes
```sql
-- Key constraints and indexes
UNIQUE INDEX IX_ApplicationUsers_GoogleId ON ApplicationUsers (GoogleId)
UNIQUE INDEX IX_CollectionMembers_CollectionId_UserId ON CollectionMembers (CollectionId, UserId)
UNIQUE INDEX IX_CollectionInvites_InviteToken ON CollectionInvites (InviteToken)

-- Foreign Key Relationships
Workspace.UserId → ApplicationUsers.Id (CASCADE DELETE)
Collection.UserId → ApplicationUsers.Id (SET NULL)
Collection.WorkspaceId → Workspace.Id (SET NULL)
ApiRequest.CollectionId → Collection.Id (CASCADE DELETE)
CollectionMember.CollectionId → Collection.Id (CASCADE DELETE)
CollectionMember.UserId → ApplicationUsers.Id (CASCADE DELETE)
CollectionInvite.CollectionId → Collection.Id (CASCADE DELETE)
CollectionInvite.InvitedByUserId → ApplicationUsers.Id (RESTRICT)
CollectionInvite.AcceptedByUserId → ApplicationUsers.Id (SET NULL)
```

### Security Architecture

#### JWT Token Structure
```json
{
  "sub": "user_id",
  "email": "user@example.com",
  "name": "User Name",
  "iat": 1692182400,
  "exp": 1692268800,
  "iss": "StampApi",
  "aud": "StampClient"
}
```

#### Authorization Patterns
```csharp
// User isolation pattern used throughout controllers
var userId = GetUserId();
if (userId == null) return Unauthorized();

var workspace = await _context.Workspaces
    .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

if (workspace == null) return NotFound();
```

#### Role-Based Access Control
```csharp
// Collection permissions
public enum CollectionRole
{
    Owner = 1,   // Full access including deletion
    Admin = 2,   // Can manage collection and members
    Member = 3   // Read-only access
}

// Permission checking
var userMembership = collection.Members.FirstOrDefault(m => m.UserId == userId);
if (collection.UserId != userId && 
    (userMembership == null || userMembership.Role != CollectionRole.Owner))
{
    return Forbid("Only collection owners can delete collections");
}
```

## Frontend Architecture (StampBlazor)

### Technology Stack
- **Framework**: Blazor WebAssembly (.NET 8)
- **UI Framework**: Bootstrap 5
- **Icons**: Bootstrap Icons
- **State Management**: Component-based state with service layer
- **HTTP Client**: Built-in .NET HttpClient

### Component Architecture

#### Component Hierarchy
```
App.razor (Root)
└── MainLayout.razor
    ├── NavMenu.razor
    │   ├── User Profile Display
    │   └── Authentication Controls
    └── Home.razor (Main Page)
        ├── IdentityLoginComponent.razor (if not authenticated)
        └── Authenticated Layout (if authenticated)
            ├── WorkspaceSelector.razor
            ├── CollectionsSidebar.razor
            ├── RequestBuilder.razor
            │   ├── RequestTabs.razor
            │   └── ResponseViewer.razor
            └── Modal Components
                ├── Collection Modals
                ├── Workspace Modals
                └── Member Management Modals
```

#### State Management Pattern
```csharp
// Service-based state management
public class WorkspaceService
{
    private readonly AuthenticatedHttpClient _httpClient;
    
    // Centralized data access
    public async Task<List<Workspace>> GetWorkspacesAsync()
    {
        // API communication with automatic authentication
    }
    
    // State change notifications through component parameters and events
}

// Component communication via EventCallbacks
[Parameter] public EventCallback<Workspace> OnWorkspaceChanged { get; set; }

// Parent-child data flow
@if (selectedWorkspace != null)
{
    <CollectionsSidebar CurrentWorkspace="selectedWorkspace" 
                       OnRequestSelected="HandleRequestSelected" />
}
```

### Service Layer Architecture

#### Service Dependencies
```
AuthenticatedHttpClient (Wrapper around HttpClient)
├── Automatic JWT token injection
├── Token refresh handling
└── Base HTTP client configuration

Data Services (Dependent on AuthenticatedHttpClient)
├── WorkspaceService
├── CollectionService
├── RequestService
├── CollectionMemberService
└── CollectionInviteService

Authentication Services (Independent)
├── AuthenticationService (Google Auth)
├── IdentityAuthenticationService (Email/Password)
└── State management for authentication

HttpRequestService (Independent)
└── Direct API calls to external services (uses separate HttpClient)
```

#### Service Communication Pattern
```csharp
// Dependency injection in Program.cs
builder.Services.AddScoped<AuthenticatedHttpClient>();
builder.Services.AddScoped<WorkspaceService>();

// Service usage in components
@inject WorkspaceService WorkspaceService

// Async data loading pattern
protected override async Task OnInitializedAsync()
{
    workspaces = await WorkspaceService.GetWorkspacesAsync();
    StateHasChanged();
}
```

### Client-Side State Management

#### Authentication State
```csharp
public class IdentityAuthenticationService
{
    private User? _currentUser;
    private string? _jwtToken;
    
    public event Action? AuthenticationStateChanged;
    
    public bool IsAuthenticated => !string.IsNullOrEmpty(_jwtToken);
    
    // State persistence in localStorage
    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", token);
}
```

#### Component State Synchronization
```csharp
// Parent component manages shared state
private Workspace? currentWorkspace;
private List<Collection>? collections;

// Child components receive state via parameters
<WorkspaceSelector CurrentWorkspace="currentWorkspace" 
                  OnWorkspaceChanged="HandleWorkspaceChanged" />

// State updates propagate through event callbacks
private async Task HandleWorkspaceChanged(Workspace workspace)
{
    currentWorkspace = workspace;
    await LoadCollections(); // Refresh dependent data
    StateHasChanged();       // Trigger re-render
}
```

## Data Flow & State Management

### Request Execution Flow
```
1. User configures request in RequestBuilder component
2. RequestBuilder calls HttpRequestService.SendRequestAsync()
3. HttpRequestService creates new HttpClient (separate from API client)
4. Direct HTTP request to target API endpoint
5. Response returned to RequestBuilder
6. ResponseViewer component displays formatted response
7. Optional: User saves request to collection via RequestService
```

### Collection Sharing Flow
```
1. Collection owner clicks "Share" button
2. CollectionMembers component loads current members
3. Owner sends invitation via CollectionInviteService
4. API creates CollectionInvite record with unique token
5. Invited user receives email with invitation link
6. User clicks link, navigating to /invite/{token} page
7. InviteAcceptance component validates token and shows collection details
8. User accepts invitation
9. API creates CollectionMember record and updates invite status
10. User gains access to shared collection
```

### Workspace Management Flow
```
1. User selects workspace in WorkspaceSelector
2. OnWorkspaceChanged event fires
3. Parent component (Home.razor) updates currentWorkspace
4. CollectionsSidebar receives new workspace parameter
5. Collections are filtered and reloaded for new workspace
6. UI updates to show workspace-specific collections
7. Request context automatically switches to new workspace
```

## Security Implementation

### Client-Side Security

#### Token Management
```javascript
// Secure token storage
localStorage.setItem('authToken', jwtToken);

// Automatic token inclusion
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);

// Token validation on page load
const storedToken = localStorage.getItem('authToken');
if (storedToken && !isTokenExpired(storedToken)) {
    // Restore authentication state
}
```

#### CORS Configuration
```csharp
// API CORS policy for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDevelopment", policy =>
    {
        policy.WithOrigins("http://localhost:5175", "https://localhost:5175")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

### Server-Side Security

#### Input Validation
```csharp
// Model validation attributes
public class Collection
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
}

// Controller validation
if (!ModelState.IsValid)
{
    return BadRequest(ModelState);
}
```

#### SQL Injection Prevention
```csharp
// Entity Framework parameterized queries
var collections = await _context.Collections
    .Where(c => c.UserId == userId && c.WorkspaceId == workspaceId)
    .Include(c => c.Requests)
    .ToListAsync();
```

#### Authorization Checks
```csharp
// Consistent authorization pattern
private async Task<bool> HasCollectionAccess(int collectionId, int userId, CollectionRole? requiredRole = null)
{
    var membership = await _context.CollectionMembers
        .FirstOrDefaultAsync(m => m.CollectionId == collectionId && m.UserId == userId);
    
    return membership != null && (requiredRole == null || membership.Role <= requiredRole);
}
```

## Performance Optimization

### Backend Optimizations

#### Database Query Optimization
```csharp
// Efficient includes to prevent N+1 queries
var workspaces = await _context.Workspaces
    .Where(w => w.UserId == userId)
    .Include(w => w.Collections)
        .ThenInclude(c => c.Requests)
    .Include(w => w.Collections)
        .ThenInclude(c => c.Members)
            .ThenInclude(m => m.User)
    .OrderBy(w => w.CreatedAt)
    .ToListAsync();
```

#### Response Caching
```csharp
// Potential caching implementation
[ResponseCache(Duration = 300, VaryByHeader = "Authorization")]
public async Task<ActionResult<IEnumerable<Workspace>>> GetWorkspaces()
```

### Frontend Optimizations

#### Component Rendering Optimization
```csharp
// Selective re-rendering
protected override bool ShouldRender()
{
    return _hasStateChanged;
}

// Efficient event handling
private readonly Dictionary<int, bool> _expandedCollections = new();

// Cached computed values
private string _cachedMethodClass = "method-get";
private string _lastMethod = "GET";
```

#### Data Loading Strategies
```csharp
// Lazy loading of non-critical data
if (availableCollections == null)
{
    availableCollections = await CollectionService.GetCollectionsAsync();
}

// Progressive loading
protected override async Task OnInitializedAsync()
{
    // Load critical data first
    await LoadWorkspaces();
    
    // Defer non-critical data
    _ = Task.Run(async () => await LoadUserPreferences());
}
```

## Deployment Architecture

### Development Environment
```
Developer Machine
├── Visual Studio/VS Code
├── .NET 8 SDK
├── Local SQLite Database
└── Local Development Servers
    ├── StampApi (localhost:5024)
    └── StampBlazor (localhost:5175)
```

### Production Deployment Options

#### Option 1: Separate Hosting
```
┌─────────────────────────────────────┐
│          Static Site Host           │
│  (Azure Static Web Apps, Netlify)  │
│                                     │
│     Blazor WASM Application         │
└─────────────┬───────────────────────┘
              │ HTTPS API Calls
              ▼
┌─────────────────────────────────────┐
│           API Host                  │
│  (Azure App Service, AWS EC2)      │
│                                     │
│      ASP.NET Core Web API           │
└─────────────┬───────────────────────┘
              │ SQL Connection
              ▼
┌─────────────────────────────────────┐
│         Database Host               │
│  (Azure SQL, AWS RDS)              │
│                                     │
│       SQL Server Database          │
└─────────────────────────────────────┘
```

#### Option 2: Unified Hosting
```
┌─────────────────────────────────────┐
│        Application Server           │
│   (IIS, Azure App Service)         │
│                                     │
│  ┌─────────────────────────────────┐ │
│  │    ASP.NET Core Host            │ │
│  │  ├── Web API Endpoints          │ │
│  │  └── Static File Serving        │ │
│  │      (Blazor WASM files)        │ │
│  └─────────────────────────────────┘ │
└─────────────┬───────────────────────┘
              │ Database Connection
              ▼
┌─────────────────────────────────────┐
│         Database Server             │
│       SQL Server Database          │
└─────────────────────────────────────┘
```

### Deployment Considerations

#### Environment Configuration
```csharp
// appsettings.json structure
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-server;Database=StampDb;..."
  },
  "Authentication": {
    "Jwt": {
      "Key": "production-secret-key",
      "Issuer": "https://api.stamp.app",
      "Audience": "https://stamp.app",
      "ExpireHours": "24"
    }
  },
  "Cors": {
    "AllowedOrigins": ["https://stamp.app"]
  }
}
```

#### Security Hardening
```csharp
// Production security configuration
if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
    app.UseHsts();
    app.UseSecurityHeaders();
}

// Rate limiting
app.UseRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});
```

## Monitoring & Observability

### Logging Strategy
```csharp
// Structured logging with Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/stamp-api-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.ApplicationInsights(telemetryConfiguration)
    .CreateLogger();

// Application logging
public class WorkspacesController : ControllerBase
{
    private readonly ILogger<WorkspacesController> _logger;
    
    public async Task<ActionResult<Workspace>> PostWorkspace(Workspace workspace)
    {
        _logger.LogInformation("Creating workspace {WorkspaceName} for user {UserId}", 
            workspace.Name, userId);
    }
}
```

### Performance Monitoring
```csharp
// Application Insights integration
builder.Services.AddApplicationInsightsTelemetry();

// Custom metrics
public class ApiMetrics
{
    private readonly IMetrics _metrics;
    
    public void RecordRequestDuration(string endpoint, TimeSpan duration)
    {
        _metrics.CreateHistogram<double>("api_request_duration")
            .Record(duration.TotalMilliseconds, new("endpoint", endpoint));
    }
}
```

### Health Checks
```csharp
// Health check configuration
builder.Services.AddHealthChecks()
    .AddDbContextCheck<StampDbContext>()
    .AddUrlGroup(new Uri("https://external-api.com/health"), "external-api");

app.MapHealthChecks("/health");
```

## Scalability Considerations

### Database Scaling
```sql
-- Indexing strategy for performance
CREATE INDEX IX_Collections_WorkspaceId_UserId ON Collections (WorkspaceId, UserId);
CREATE INDEX IX_ApiRequests_CollectionId ON ApiRequests (CollectionId);
CREATE INDEX IX_CollectionMembers_UserId ON CollectionMembers (UserId);

-- Partitioning strategy for large datasets
-- Partition by UserId or date ranges for historical data
```

### Caching Strategy
```csharp
// Distributed caching with Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = connectionString;
});

// Memory caching for frequently accessed data
builder.Services.AddMemoryCache();

public class WorkspaceService
{
    private readonly IMemoryCache _cache;
    
    public async Task<List<Workspace>> GetWorkspacesAsync(int userId)
    {
        var cacheKey = $"workspaces_{userId}";
        if (!_cache.TryGetValue(cacheKey, out List<Workspace> workspaces))
        {
            workspaces = await _context.Workspaces
                .Where(w => w.UserId == userId)
                .ToListAsync();
                
            _cache.Set(cacheKey, workspaces, TimeSpan.FromMinutes(5));
        }
        return workspaces;
    }
}
```

### Load Balancing
```
           Load Balancer
                │
        ┌───────┼───────┐
        ▼       ▼       ▼
   API Server  API Server  API Server
   Instance 1  Instance 2  Instance 3
        │       │       │
        └───────┼───────┘
                ▼
        Shared Database
```

## Future Architecture Enhancements

### Microservices Evolution
```
Current Monolith → Future Microservices

┌─────────────────────────────────────┐
│           API Gateway               │
│     (Authentication, Routing)       │
└─────────────┬───────────────────────┘
              │
    ┌─────────┼─────────┐
    ▼         ▼         ▼
┌─────────┐ ┌─────────┐ ┌─────────┐
│ Auth    │ │Workspace│ │Request  │
│Service  │ │Service  │ │Service  │
└─────────┘ └─────────┘ └─────────┘
    │         │         │
    ▼         ▼         ▼
┌─────────┐ ┌─────────┐ ┌─────────┐
│Auth DB  │ │Core DB  │ │Request  │
│         │ │         │ │History  │
└─────────┘ └─────────┘ └─────────┘
```

### Real-time Collaboration
```csharp
// SignalR integration for real-time features
builder.Services.AddSignalR();

public class CollaborationHub : Hub
{
    public async Task JoinCollection(string collectionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"collection_{collectionId}");
    }
    
    public async Task RequestUpdated(string collectionId, ApiRequest request)
    {
        await Clients.Group($"collection_{collectionId}")
            .SendAsync("RequestUpdated", request);
    }
}
```

### Event-Driven Architecture
```csharp
// Domain events for complex workflows
public class CollectionSharedEvent : IDomainEvent
{
    public int CollectionId { get; set; }
    public int SharedByUserId { get; set; }
    public string InvitedEmail { get; set; }
}

public class CollectionSharedEventHandler : INotificationHandler<CollectionSharedEvent>
{
    public async Task Handle(CollectionSharedEvent notification, CancellationToken cancellationToken)
    {
        // Send invitation email
        // Log audit event
        // Update analytics
    }
}
```

This architecture provides a solid foundation for a scalable, maintainable, and secure API client application while allowing for future enhancements and growth.