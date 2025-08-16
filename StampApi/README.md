# Stamp API Documentation

## Overview

The Stamp API is a robust RESTful web service built with .NET 8 that powers the Stamp collaborative API client application. It provides comprehensive endpoints for managing workspaces, collections, API requests, user authentication, and collaborative features including collection sharing and member management.

## Architecture

### Technology Stack
- **Framework**: ASP.NET Core 8 Web API
- **Database**: SQLite with Entity Framework Core 8
- **Authentication**: ASP.NET Core Identity with JWT Bearer tokens
- **ORM**: Entity Framework Core with Code-First migrations

### Core Components
- **Controllers**: Handle HTTP requests and responses
- **Models**: Define data structures and entity relationships
- **DbContext**: Manages database operations and entity configurations
- **Services**: Business logic and data access layer (handled by controllers in current implementation)

## Database Schema

### Entity Relationships

```
ApplicationUser (Identity)
├── Workspaces (1:N)
│   └── Collections (1:N)
│       ├── ApiRequests (1:N)
│       ├── CollectionMembers (1:N)
│       └── CollectionInvites (1:N)
└── CollectionMembers (1:N)
```

### Core Entities

#### ApplicationUser
- Extends `IdentityUser<int>` for authentication
- **Properties**: Id, UserName, Email, GoogleId, AvatarUrl, CreatedAt, LastLoginAt
- **Relationships**: One-to-many with Workspaces and Collections

#### Workspace
- **Properties**: Id, Name, Description, CreatedAt, UpdatedAt, UserId
- **Relationships**: 
  - Belongs to ApplicationUser
  - One-to-many with Collections

#### Collection
- **Properties**: Id, Name, Description, CreatedAt, UpdatedAt, UserId, WorkspaceId
- **Relationships**:
  - Belongs to ApplicationUser and Workspace
  - One-to-many with ApiRequests, CollectionMembers, CollectionInvites

#### ApiRequest
- **Properties**: Id, Name, Url, Method, Headers, Body, QueryParams, Authentication, CreatedAt, UpdatedAt, CollectionId
- **Relationships**: Belongs to Collection

#### CollectionMember
- **Properties**: Id, CollectionId, UserId, Role, JoinedAt
- **Roles**: Owner, Admin, Member
- **Relationships**: Belongs to Collection and ApplicationUser

#### CollectionInvite
- **Properties**: Id, CollectionId, InvitedByUserId, InvitedEmail, Role, InviteToken, Status, CreatedAt, ExpiresAt, AcceptedAt, AcceptedByUserId
- **Status**: Pending, Accepted, Declined, Expired, Cancelled
- **Relationships**: Belongs to Collection and ApplicationUser

## Authentication & Security

### JWT Authentication
The API uses JWT Bearer tokens for authentication with the following configuration:
- **Issuer**: Configurable in appsettings.json
- **Audience**: Configurable in appsettings.json
- **Expiration**: Configurable hours (default settings in appsettings.json)
- **Algorithm**: HMAC-SHA256

### Password Requirements
- Minimum length: 6 characters
- No special character requirements (simplified for development)
- Unique email addresses required

### Authorization
- All API endpoints require authentication except authentication endpoints
- User isolation: Users can only access their own workspaces and associated data
- Role-based access for collections:
  - **Owner**: Full access (create, read, update, delete)
  - **Admin**: Can manage collection and members
  - **Member**: Read-only access

## API Endpoints Reference

### Authentication Endpoints

#### POST /api/IdentityAuth/register
Register a new user account.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "password123"
}
```

**Response:**
```json
{
  "token": "jwt_token_here",
  "user": {
    "id": 1,
    "email": "user@example.com",
    "name": "user@example.com",
    "avatarUrl": null
  }
}
```

#### POST /api/IdentityAuth/login
Authenticate an existing user.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "password123"
}
```

**Response:**
```json
{
  "token": "jwt_token_here",
  "user": {
    "id": 1,
    "email": "user@example.com",
    "name": "user@example.com",
    "avatarUrl": null
  }
}
```

### Workspace Management

#### GET /api/Workspaces
Get all workspaces for the authenticated user.

**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Response:**
```json
[
  {
    "id": 1,
    "name": "My Workspace",
    "description": "Default workspace",
    "createdAt": "2024-08-16T12:00:00Z",
    "updatedAt": "2024-08-16T12:00:00Z",
    "userId": 1,
    "collections": [...]
  }
]
```

#### GET /api/Workspaces/{id}
Get a specific workspace by ID.

**Response:**
```json
{
  "id": 1,
  "name": "My Workspace",
  "description": "Default workspace",
  "createdAt": "2024-08-16T12:00:00Z",
  "updatedAt": "2024-08-16T12:00:00Z",
  "userId": 1,
  "collections": [
    {
      "id": 1,
      "name": "Sample Collection",
      "requests": [...]
    }
  ]
}
```

#### POST /api/Workspaces
Create a new workspace.

**Request Body:**
```json
{
  "name": "New Workspace",
  "description": "Description of the workspace"
}
```

**Response:** Returns the created workspace with generated ID.

#### PUT /api/Workspaces/{id}
Update an existing workspace.

**Request Body:**
```json
{
  "id": 1,
  "name": "Updated Workspace Name",
  "description": "Updated description"
}
```

#### DELETE /api/Workspaces/{id}
Delete a workspace. Collections will be moved to the user's first remaining workspace.

**Business Rules:**
- Cannot delete the user's only workspace
- Collections are automatically migrated to another workspace

### Collection Management

#### GET /api/Collections
Get all collections for the authenticated user, optionally filtered by workspace.

**Query Parameters:**
- `workspaceId` (optional): Filter collections by workspace ID

**Response:**
```json
[
  {
    "id": 1,
    "name": "API Collection",
    "description": "Collection of API requests",
    "createdAt": "2024-08-16T12:00:00Z",
    "updatedAt": "2024-08-16T12:00:00Z",
    "userId": 1,
    "workspaceId": 1,
    "requests": [...],
    "members": [...]
  }
]
```

#### GET /api/Collections/{id}
Get a specific collection by ID.

#### POST /api/Collections
Create a new collection.

**Request Body:**
```json
{
  "name": "New Collection",
  "description": "Description of the collection",
  "workspaceId": 1
}
```

**Business Rules:**
- WorkspaceId is required
- Workspace must belong to the authenticated user
- Owner membership is automatically created

#### PUT /api/Collections/{id}
Update an existing collection.

**Authorization:**
- User must be collection owner or admin
- Workspace must belong to the user

#### DELETE /api/Collections/{id}
Delete a collection.

**Authorization:**
- Only collection owners can delete collections

### Request Management

#### GET /api/Requests
Get all API requests for the authenticated user.

#### GET /api/Requests/{id}
Get a specific API request by ID.

#### GET /api/Requests/collection/{collectionId}
Get all requests in a specific collection.

#### POST /api/Requests
Create a new API request.

**Request Body:**
```json
{
  "name": "Sample Request",
  "url": "https://api.example.com/users",
  "method": "GET",
  "headers": "{\"Content-Type\": \"application/json\"}",
  "body": "{\"key\": \"value\"}",
  "queryParams": "param1=value1&param2=value2",
  "authentication": "{\"type\": \"bearer\", \"token\": \"...\"}",
  "collectionId": 1
}
```

#### PUT /api/Requests/{id}
Update an existing API request.

#### DELETE /api/Requests/{id}
Delete an API request.

### Collection Member Management

#### GET /api/CollectionMembers/{collectionId}
Get all members of a specific collection.

#### POST /api/CollectionMembers/{collectionId}/invite
Invite a user to join a collection.

**Request Body:**
```json
{
  "email": "user@example.com",
  "role": "Member"
}
```

#### DELETE /api/CollectionMembers/{collectionId}/{userId}
Remove a user from a collection.

#### PUT /api/CollectionMembers/{collectionId}/{userId}
Update a member's role in a collection.

### Collection Invite Management

#### GET /api/CollectionInvites/{collectionId}
Get all pending invites for a collection.

#### GET /api/CollectionInvites/token/{token}
Get invite details by token.

#### POST /api/CollectionInvites/{token}/accept
Accept a collection invitation.

#### POST /api/CollectionInvites/{token}/decline
Decline a collection invitation.

#### DELETE /api/CollectionInvites/{inviteId}
Cancel a pending invitation.

## Configuration

### Database Connection
The API uses SQLite for development with the connection string configured in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=stamp.db"
  }
}
```

### JWT Settings
Configure JWT authentication in `appsettings.json`:

```json
{
  "Authentication": {
    "Jwt": {
      "Key": "your-secret-key-here",
      "Issuer": "StampApi",
      "Audience": "StampClient",
      "ExpireHours": "24"
    }
  }
}
```

### CORS Policy
The API includes CORS configuration for local development:
- Allowed origins: `http://localhost:5175`, `https://localhost:5175`
- Allows credentials, all methods, and all headers

## Deployment Guide

### Prerequisites
- .NET 8 SDK
- SQL Server or SQLite (for development)

### Setup Steps

1. **Clone the repository and navigate to the API directory**
   ```bash
   git clone <repository-url>
   cd StampApi
   ```

2. **Configure the database connection**
   - Update `appsettings.json` with your database connection string
   - For production, consider using SQL Server instead of SQLite

3. **Configure JWT settings**
   - Generate a secure secret key
   - Update JWT settings in `appsettings.json`

4. **Run database migrations**
   ```bash
   dotnet ef database update
   ```

5. **Build and run the application**
   ```bash
   dotnet build
   dotnet run
   ```

### Production Considerations

1. **Security**
   - Use strong JWT secret keys
   - Enable HTTPS in production
   - Configure proper CORS origins
   - Implement rate limiting
   - Add input validation and sanitization

2. **Database**
   - Use SQL Server or PostgreSQL for production
   - Configure connection pooling
   - Implement database backup strategies

3. **Monitoring**
   - Add application logging with Serilog or similar
   - Implement health checks
   - Configure application insights or monitoring tools

4. **Performance**
   - Enable response caching where appropriate
   - Optimize database queries
   - Consider implementing pagination for large datasets

## Error Handling

The API returns standard HTTP status codes:
- **200 OK**: Successful GET requests
- **201 Created**: Successful POST requests
- **204 No Content**: Successful PUT/DELETE requests
- **400 Bad Request**: Invalid request data
- **401 Unauthorized**: Authentication required
- **403 Forbidden**: Insufficient permissions
- **404 Not Found**: Resource not found
- **500 Internal Server Error**: Server errors

Error responses include descriptive messages when possible.

## API Versioning

Currently, the API does not implement versioning. For future versions, consider:
- URL path versioning (`/api/v1/collections`)
- Header-based versioning
- Query parameter versioning

## Testing

### Manual Testing
Use the included `StampApi.http` file with REST Client extensions in VS Code or similar tools.

### Automated Testing
Consider implementing:
- Unit tests for business logic
- Integration tests for API endpoints
- Database tests with in-memory providers

## Support

For issues and feature requests, please refer to the project's issue tracker or documentation.