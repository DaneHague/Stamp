# Stamp Blazor Application Documentation

## Overview

Stamp is a modern, collaborative API client built with Blazor WebAssembly that enables developers to create, test, and manage HTTP requests efficiently. The application provides an intuitive interface for organizing API requests into collections within workspaces, with full collaboration features including team sharing and member management.

## Key Features

### Core Functionality
- **HTTP Request Builder**: Create and execute HTTP requests with support for all common methods (GET, POST, PUT, DELETE, etc.)
- **Request Configuration**: Comprehensive setup for headers, query parameters, request body, and authentication
- **Response Viewer**: Detailed response display including status codes, headers, and formatted body content
- **Workspace Organization**: Organize your API work into logical workspaces
- **Collection Management**: Group related requests into collections for better organization
- **Request Persistence**: Save and manage your API requests for reuse

### Collaboration Features
- **Collection Sharing**: Invite team members to collaborate on API collections
- **Role-Based Access**: Owner, Admin, and Member roles with appropriate permissions
- **Real-time Collaboration**: Work together on API development and testing

### Authentication
- **Multiple Auth Methods**: Support for both email/password and Google authentication
- **Secure Token Management**: JWT-based authentication with secure token storage
- **Session Persistence**: Maintain authentication state across browser sessions

## User Interface Guide

### Main Application Layout

The Stamp application uses a clean, organized layout with the following key areas:

#### Navigation Bar
- **Logo and Title**: Stamp branding
- **User Profile**: Displays current user information and logout option
- **Authentication Status**: Shows login state and provides access to authentication

#### Workspace Selector
- **Current Workspace**: Displays the active workspace name and description
- **Workspace Dropdown**: Access to all user workspaces with quick switching
- **Workspace Management**: Create, edit, and delete workspaces
- **Workspace Actions**: Comprehensive workspace management tools

#### Collections Sidebar
- **Collections List**: Expandable tree view of collections and their requests
- **Collection Actions**: Create, share, and delete collections
- **Request Items**: Individual API requests within collections with method indicators
- **Quick Access**: Click any request to load it in the main editor

#### Request Builder (Main Area)
- **HTTP Method Selector**: Dropdown with support for GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS
- **URL Input**: Primary URL field with real-time validation
- **Send Button**: Execute the request with loading indicator
- **Save Button**: Persist requests to collections
- **Request Configuration Tabs**: Organized access to headers, parameters, body, and authentication

#### Response Viewer
- **Status Information**: HTTP status code and response time
- **Response Headers**: Key-value display of all response headers
- **Response Body**: Formatted display of response content with syntax highlighting
- **Response Statistics**: Additional metadata about the request/response cycle

### Workspace Management

#### Creating Workspaces
1. Click the workspace dropdown in the top navigation
2. Select "New Workspace" from the actions menu
3. Enter a workspace name (required) and optional description
4. Click "Create" to establish the new workspace

#### Switching Workspaces
1. Click the current workspace name to open the dropdown
2. Select any workspace from the list to switch to it
3. Collections will automatically update to show the selected workspace's content

#### Editing Workspaces
1. Open the workspace dropdown
2. Click "Edit" next to the current workspace
3. Modify the name or description as needed
4. Save changes to update the workspace

#### Deleting Workspaces
1. Open the workspace dropdown
2. Click "Delete" (only available if you have multiple workspaces)
3. Confirm the deletion
4. All collections will be moved to your remaining workspace

### Collections & Requests Management

#### Creating Collections
1. Ensure you have selected the desired workspace
2. Click "New" in the collections sidebar header
3. Enter a collection name (required) and optional description
4. Click "Create" to establish the collection

#### Adding Requests to Collections
1. Configure your HTTP request in the main request builder
2. Click the "Save" button next to the "Send" button
3. Enter a descriptive name for the request
4. Select the target collection from the dropdown
5. Click "Save" to persist the request

#### Organizing Requests
1. Click collection names in the sidebar to expand/collapse them
2. Individual requests show their HTTP method with color-coded badges
3. Click any request to load it into the request builder
4. Modify and resave requests to update them

#### Collection Sharing
1. Click the "Share" button (people icon) next to any collection
2. Enter the email address of the person to invite
3. Select their role (Owner, Admin, or Member)
4. Send the invitation
5. Manage existing members and pending invitations from the same dialog

### Request Configuration

#### HTTP Methods
The application supports all standard HTTP methods:
- **GET**: Retrieve data (indicated by blue down arrow)
- **POST**: Create new resources (indicated by green plus)
- **PUT**: Update resources (indicated by orange up arrow)
- **PATCH**: Partial updates (indicated by yellow pencil)
- **DELETE**: Remove resources (indicated by red trash)
- **HEAD**: Retrieve headers only (indicated by info icon)
- **OPTIONS**: Check available methods (indicated by gear icon)

#### URL and Parameters
- **Base URL**: Enter the complete endpoint URL
- **Query Parameters**: Add parameters via the Params tab with key-value pairs
- **URL Auto-Update**: Parameters automatically update the displayed URL

#### Headers Configuration
- **Custom Headers**: Add any HTTP headers required by your API
- **Content-Type**: Automatically suggested based on request body
- **Authorization**: Can be set manually or via the Auth tab

#### Request Body
- **Raw JSON**: Primary body editor with JSON syntax highlighting
- **Content Types**: Support for JSON, XML, text, and other formats
- **Body Validation**: Real-time syntax checking for JSON content

#### Authentication
- **Bearer Token**: JWT or API token authentication
- **Basic Auth**: Username/password authentication
- **Custom Headers**: Manual authentication header setup
- **No Auth**: Requests without authentication requirements

### Response Analysis

#### Status Information
- **HTTP Status Code**: Color-coded display (green for 2xx, yellow for 3xx, red for 4xx/5xx)
- **Response Time**: Execution time in milliseconds
- **Response Size**: Content length information

#### Headers Display
- **Organized View**: All response headers in an easy-to-read format
- **Security Headers**: Special highlighting for important security headers
- **Cache Information**: Cache-related headers clearly displayed

#### Response Body
- **JSON Formatting**: Automatic pretty-printing and syntax highlighting for JSON
- **HTML Rendering**: Option to view HTML responses as rendered content
- **Raw View**: Access to unformatted response content
- **Search and Filter**: Find specific content within large responses

## Component Architecture

### Core Components

#### App.razor
- **Root Component**: Application entry point and global configuration
- **Routing**: Defines application routes and page navigation
- **Global Services**: Initializes application-wide services

#### MainLayout.razor
- **Application Shell**: Provides the main application structure
- **Navigation Integration**: Houses the navigation menu and user interface
- **Responsive Design**: Adapts to different screen sizes

#### Home.razor (Main Page)
- **Authentication Gate**: Displays login or main application based on auth state
- **Component Orchestration**: Coordinates workspace, collection, and request components
- **State Management**: Manages global application state

### Feature Components

#### WorkspaceSelector.razor
- **Workspace Management**: Complete workspace lifecycle management
- **Dropdown Interface**: Intuitive workspace selection and switching
- **Modal Dialogs**: Create, edit, and delete workspace functionality
- **Event Handling**: Workspace change notifications

#### CollectionsSidebar.razor
- **Collection Display**: Hierarchical view of collections and requests
- **Interaction Handling**: Expand/collapse, selection, and navigation
- **CRUD Operations**: Create, delete, and manage collections
- **Member Management**: Integration with collection sharing features

#### RequestBuilder.razor
- **Request Configuration**: Complete HTTP request setup interface
- **Method Selection**: Dropdown with visual method indicators
- **URL Handling**: Real-time URL validation and parameter integration
- **Save Functionality**: Request persistence with collection assignment

#### RequestTabs.razor
- **Tabbed Interface**: Organized access to request configuration options
- **Dynamic Content**: Context-aware tab content based on HTTP method
- **Data Binding**: Real-time synchronization with request model

#### ResponseViewer.razor
- **Response Display**: Comprehensive response analysis and display
- **Format Detection**: Automatic content type detection and formatting
- **Status Visualization**: Color-coded status and timing information

#### CollectionMembers.razor
- **Member Management**: Complete collection collaboration interface
- **Invitation System**: Send and manage collection invitations
- **Role Management**: Owner, Admin, Member role assignment and modification
- **Real-time Updates**: Dynamic member list updates

### Authentication Components

#### IdentityLoginComponent.razor
- **Login Interface**: Email/password authentication form
- **Registration**: New user account creation
- **Validation**: Client-side form validation and error handling
- **State Management**: Authentication state updates

#### LoginComponent.razor (Google Auth)
- **Google Integration**: Google Sign-In implementation
- **OAuth Flow**: Secure Google authentication workflow
- **Profile Management**: Google profile information integration

## Services & State Management

### Core Services

#### AuthenticationService.cs
- **Google Authentication**: Google Sign-In integration and token management
- **Token Storage**: Secure JWT token persistence in localStorage
- **State Events**: Authentication state change notifications
- **Session Management**: Automatic session restoration

#### IdentityAuthenticationService.cs
- **Email/Password Auth**: Traditional authentication implementation
- **Registration**: New user account creation
- **Token Management**: JWT token handling and refresh
- **User Profile**: Current user information management

#### AuthenticatedHttpClient.cs
- **HTTP Client Wrapper**: Automatic authorization header injection
- **Token Refresh**: Automatic token refresh on expiration
- **Request Intercepting**: Global request/response intercepting

### Data Services

#### WorkspaceService.cs
- **CRUD Operations**: Complete workspace management
- **API Communication**: RESTful API integration
- **Data Caching**: Local workspace data caching
- **Error Handling**: Comprehensive error management

#### CollectionService.cs
- **Collection Management**: Full collection lifecycle operations
- **Workspace Integration**: Workspace-scoped collection operations
- **Member Filtering**: Access control and permission-based filtering

#### RequestService.cs
- **Request CRUD**: API request creation, update, and deletion
- **Collection Integration**: Request-to-collection association
- **Data Validation**: Request data validation before persistence

#### HttpRequestService.cs
- **External API Calls**: Direct HTTP requests to external APIs
- **Response Processing**: Response parsing and error handling
- **Configuration Support**: Headers, authentication, and parameter handling

### Collaboration Services

#### CollectionMemberService.cs
- **Member Management**: Collection member CRUD operations
- **Role Management**: Permission-based role assignment
- **Invitation Processing**: Member invitation workflow

#### CollectionInviteService.cs
- **Invitation System**: Complete invitation lifecycle management
- **Token Handling**: Secure invitation token generation and validation
- **Email Integration**: Invitation delivery system

### Data Models

#### Core Models
- **Workspace**: Workspace entity with collections relationship
- **Collection**: Collection entity with requests and members
- **ApiRequest**: HTTP request configuration and metadata
- **User**: User profile and authentication information

#### Authentication Models
- **RequestAuthentication**: Authentication configuration for requests
- **AuthenticationType**: Supported authentication methods enum

#### Collaboration Models
- **CollectionMember**: Member relationship with roles
- **CollectionInvite**: Invitation entity with status tracking
- **RequestTab**: Tab management for request builder interface

#### Response Models
- **HttpResponseInfo**: Complete HTTP response representation
- **ResponseHeaders**: Structured response header information

## Styling & Theming

### CSS Architecture

#### Global Styles (app.css)
- **Bootstrap Integration**: Bootstrap 5 framework for base styling
- **Custom Variables**: CSS custom properties for consistent theming
- **Responsive Design**: Mobile-first responsive design principles
- **Component Overrides**: Global style overrides for better UX

#### Component Styles
Each major component has its own CSS file for encapsulated styling:
- **MainLayout.razor.css**: Application shell and navigation styling
- **RequestBuilder.razor.css**: Request builder interface styling
- **ResponseViewer.razor.css**: Response display formatting
- **CollectionsSidebar.razor.css**: Sidebar and collection tree styling
- **WorkspaceSelector.razor.css**: Workspace dropdown and modal styling

### Design System

#### Color Palette
- **Primary Colors**: Brand colors for primary actions and emphasis
- **Method Colors**: Distinct colors for each HTTP method
- **Status Colors**: Semantic colors for success, warning, and error states
- **Neutral Colors**: Grays for text, borders, and background elements

#### Typography
- **Font Family**: System font stack for optimal readability
- **Heading Hierarchy**: Consistent heading sizes and weights
- **Body Text**: Readable body text with appropriate line height
- **Code Display**: Monospace fonts for URLs, code, and technical content

#### Iconography
- **Bootstrap Icons**: Comprehensive icon set for UI elements
- **Method Icons**: Specific icons for each HTTP method
- **Action Icons**: Consistent icons for common actions (save, delete, edit)
- **Status Icons**: Visual indicators for various states

#### Spacing & Layout
- **Grid System**: Bootstrap grid for responsive layouts
- **Component Spacing**: Consistent padding and margins
- **Modal Layouts**: Centered modal dialogs with proper spacing
- **Form Layouts**: Well-organized form fields with appropriate spacing

## Development Setup

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022, VS Code, or JetBrains Rider
- Node.js (for potential future npm packages)

### Getting Started

1. **Clone the Repository**
   ```bash
   git clone <repository-url>
   cd StampBlazor
   ```

2. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure API Endpoint**
   Update the API base address in `Program.cs`:
   ```csharp
   builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5024/") });
   ```

4. **Run the Application**
   ```bash
   dotnet run
   ```
   The application will be available at `http://localhost:5175`

### Project Structure

```
StampBlazor/
├── Components/           # Reusable UI components
│   ├── CollectionMembers.razor
│   ├── CollectionsSidebar.razor
│   ├── RequestBuilder.razor
│   ├── RequestTabs.razor
│   ├── ResponseViewer.razor
│   ├── WorkspaceSelector.razor
│   └── [Component].razor.css
├── Layout/              # Application layout components
│   ├── MainLayout.razor
│   └── NavMenu.razor
├── Models/              # Data models and DTOs
├── Pages/               # Page components and routes
├── Services/            # Business logic and API services
├── wwwroot/            # Static assets
│   ├── css/            # Global stylesheets
│   ├── js/             # JavaScript files
│   └── index.html      # Application host page
├── Program.cs          # Application entry point
└── _Imports.razor      # Global using statements
```

### Development Guidelines

#### Component Development
1. **Single Responsibility**: Each component should have a focused purpose
2. **Reusability**: Design components for reuse across the application
3. **State Management**: Use appropriate state management patterns
4. **Event Handling**: Implement proper event callbacks for component communication

#### Service Development
1. **Dependency Injection**: Register all services in Program.cs
2. **Error Handling**: Implement comprehensive error handling
3. **Async Patterns**: Use async/await for all API calls
4. **Interface Segregation**: Consider interfaces for testability

#### Styling Guidelines
1. **Component Scoping**: Use component-specific CSS files
2. **Bootstrap Integration**: Leverage Bootstrap classes where appropriate
3. **Responsive Design**: Ensure all components work on mobile devices
4. **Accessibility**: Follow accessibility best practices

## Configuration

### Application Settings

#### API Configuration
The application is configured to communicate with the Stamp API:
- **Base URL**: Configurable in Program.cs
- **Default Endpoint**: `http://localhost:5024/`
- **Authentication**: JWT Bearer token authentication

#### Service Registration
All services are registered in Program.cs with appropriate lifetimes:
- **Scoped Services**: Authentication, data services, and HTTP clients
- **Service Dependencies**: Proper dependency injection configuration

### Authentication Configuration

#### JWT Token Storage
- **Storage Method**: Browser localStorage
- **Token Key**: "authToken"
- **User Data Key**: "currentUser"
- **Automatic Restoration**: Token restoration on application start

#### Google Authentication
- **Google Client Setup**: Configured in wwwroot/js/google-auth.js
- **OAuth Flow**: Standard Google OAuth 2.0 implementation
- **Profile Integration**: User profile data synchronization

## Performance Considerations

### Optimization Strategies

#### Component Optimization
- **Rendering Optimization**: Use `StateHasChanged()` judiciously
- **Event Handling**: Properly dispose of event subscriptions
- **Component Lifecycle**: Optimize component initialization and cleanup

#### Data Management
- **Caching**: Local caching of workspace and collection data
- **Lazy Loading**: Load data only when needed
- **State Synchronization**: Efficient state updates across components

#### Network Optimization
- **Request Batching**: Group related API calls where possible
- **Error Retry**: Implement retry logic for failed requests
- **Response Caching**: Cache appropriate API responses

### Memory Management
- **Component Disposal**: Proper disposal of components and subscriptions
- **Event Cleanup**: Remove event handlers to prevent memory leaks
- **Service Lifetime**: Appropriate service lifetime management

## Testing Strategy

### Unit Testing
- **Component Testing**: Test individual component behavior
- **Service Testing**: Test business logic and API communication
- **Model Testing**: Validate data model behavior

### Integration Testing
- **API Integration**: Test API service integration
- **Authentication Flow**: Test authentication workflows
- **Component Integration**: Test component interaction

### End-to-End Testing
- **User Workflows**: Test complete user scenarios
- **Cross-Browser Testing**: Ensure compatibility across browsers
- **Mobile Testing**: Verify mobile device functionality

## Deployment

### Build Configuration

#### Production Build
```bash
dotnet publish -c Release
```

#### Build Optimization
- **Trimming**: Reduce application size with IL trimming
- **Compression**: Enable response compression
- **Caching**: Configure proper caching headers

### Hosting Options

#### Static Site Hosting
- **GitHub Pages**: Host on GitHub Pages with proper routing
- **Azure Static Web Apps**: Deploy to Azure with CI/CD
- **Netlify**: Deploy with Netlify's Blazor WASM support

#### Traditional Web Hosting
- **IIS**: Host on Internet Information Services
- **Apache**: Configure Apache for Blazor WASM
- **Nginx**: Set up Nginx reverse proxy

### Configuration Management
- **Environment-Specific Settings**: Configure different API endpoints
- **Feature Flags**: Implement feature toggling
- **Security Configuration**: Secure authentication and API keys

## Troubleshooting

### Common Issues

#### Authentication Problems
- **Token Expiration**: Implement automatic token refresh
- **CORS Issues**: Ensure proper CORS configuration on API
- **Storage Problems**: Handle localStorage limitations

#### API Communication
- **Network Errors**: Implement proper error handling and retry logic
- **Serialization Issues**: Ensure proper JSON serialization/deserialization
- **Timeout Handling**: Configure appropriate request timeouts

#### Performance Issues
- **Slow Loading**: Optimize component rendering and data loading
- **Memory Leaks**: Ensure proper component disposal
- **Large Response Handling**: Implement pagination or streaming

### Debugging Tools
- **Browser DevTools**: Use browser debugging tools effectively
- **Blazor DevTools**: Leverage Blazor-specific debugging features
- **Network Monitoring**: Monitor API calls and responses

## Future Enhancements

### Planned Features
- **Advanced Authentication**: Additional authentication providers
- **Request History**: Track and replay previous requests
- **Environment Management**: Support for different API environments
- **Export/Import**: Backup and restore functionality
- **Advanced Collaboration**: Real-time collaborative editing

### Technical Improvements
- **Offline Support**: Progressive Web App capabilities
- **Performance Optimization**: Additional caching and optimization
- **Accessibility Enhancements**: Improved screen reader support
- **Mobile App**: Native mobile application development

## Support & Contributing

### Getting Help
- **Documentation**: Comprehensive documentation and examples
- **Issue Tracking**: GitHub issues for bugs and feature requests
- **Community**: Developer community and discussions

### Contributing Guidelines
- **Code Standards**: Follow established coding conventions
- **Testing Requirements**: Include tests with new features
- **Documentation**: Update documentation with changes
- **Review Process**: Participate in code review process