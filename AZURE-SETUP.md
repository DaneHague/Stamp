# Manual Azure Setup Guide for Stamp Application

This guide covers the essential configuration changes needed to deploy the Stamp application to Azure using your manually created resources.

## Prerequisites

You mentioned you have already created:
- Resource group
- Service plan
- 2 web apps (for API and Blazor)

## Database Recommendation

For your database, I recommend **Azure SQL Database**:

### Recommended Tiers:
- **Development/Staging**: Basic tier (5 DTU, 2GB) - Cost effective
- **Production**: Standard S1 (20 DTU, 250GB) - Good performance with room to scale

### Key Features:
- Automated backups with point-in-time restore
- Built-in security and threat detection
- Easy scaling as your application grows
- Integration with Azure services

## Application Configuration

The Stamp application has been updated to work with Azure:

### API Changes Made:
- ✅ **SQL Server Support**: Added Entity Framework SQL Server package
- ✅ **Environment Detection**: Uses SQLite for local dev, SQL Server for Azure
- ✅ **Key Vault Integration**: Automatic secret loading in production
- ✅ **Health Checks**: Added `/health` endpoint for monitoring
- ✅ **Auto-migrations**: Database updates applied automatically on startup

### Configuration Files:
- ✅ **Environment Templates**: Staging and Production appsettings with placeholders
- ✅ **Secure Defaults**: No actual secrets in committed files
- ✅ **Local Development**: Unchanged - still uses SQLite and local settings

## Required App Service Configuration

For your manually created web apps, you'll need to configure these settings:

### API Web App Settings:
```
ASPNETCORE_ENVIRONMENT = "Production" (or "Staging")
ConnectionStrings__DefaultConnection = "Your Azure SQL connection string"
Authentication__Jwt__Key = "Your JWT signing key (32+ characters)"
Authentication__Google__ClientId = "Your Google OAuth client ID"
Authentication__Google__ClientSecret = "Your Google OAuth client secret"
AllowedOrigins__0 = "https://your-blazor-app.azurewebsites.net"
```

### Blazor Web App:
- The Blazor app will automatically use the appropriate `appsettings.{Environment}.json` file
- You may need to update the API URL in the configuration after deployment

## Security Recommendations

### Option 1: App Service Configuration (Simple)
Store all settings directly in Azure App Service Configuration section.

### Option 2: Azure Key Vault (Recommended for Production)
1. Create an Azure Key Vault
2. Store sensitive values (connection strings, API keys) in Key Vault
3. Enable Managed Identity on your web apps
4. Grant Key Vault access to the web app identities
5. Reference Key Vault secrets in App Service configuration

## Local Development

No changes needed for local development:
- Still uses SQLite database
- Still uses `appsettings.json` for local settings
- For local secrets, use User Secrets or `appsettings.local.json`

## Database Connection String Format

For Azure SQL Database, use this connection string format:
```
Server=tcp:YOUR_SERVER.database.windows.net,1433;Initial Catalog=YOUR_DATABASE;Persist Security Info=False;User ID=YOUR_USERNAME;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

## Deployment Steps

1. **Deploy API**: Upload your API project to the API web app
2. **Deploy Blazor**: Upload your Blazor project to the Blazor web app  
3. **Configure Settings**: Add the required app settings to your API web app
4. **Test**: Verify the `/health` endpoint returns 200 OK
5. **Update CORS**: Ensure the API allows requests from your Blazor app URL

## Testing Your Deployment

### API Health Check:
Visit: `https://your-api-app.azurewebsites.net/health`
Should return: `Healthy`

### Blazor App:
Visit: `https://your-blazor-app.azurewebsites.net`
Should load the Stamp application interface

## Troubleshooting

### Common Issues:
- **500 errors**: Check connection string and ensure database exists
- **CORS errors**: Verify AllowedOrigins setting in API configuration
- **Authentication issues**: Check Google OAuth credentials and JWT key

### Useful Commands:
```bash
# Create database migration (if needed)
dotnet ef migrations add InitialMigration --project StampApi

# Apply migrations manually (if auto-migration fails)
dotnet ef database update --project StampApi --connection "YOUR_CONNECTION_STRING"
```

The application is now ready for manual Azure deployment with your existing resources!