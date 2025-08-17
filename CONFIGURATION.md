# Configuration Management Guide

This document explains how configuration is managed across different environments in the Stamp application.

## Configuration Strategy

The Stamp application uses a simple, secure configuration approach:

1. **Base Configuration**: `appsettings.json` - Default settings for local development
2. **Environment Templates**: `appsettings.{Environment}.json` - Environment-specific structure with placeholders
3. **Azure App Service Configuration**: Runtime settings that override template values
4. **Local Overrides**: `appsettings.local.json` or User Secrets (not committed)

## Environment-Specific Files

### ✅ Safe to Commit (Template Files)

These files contain **only structure and placeholders** - no actual secrets:

- `StampApi/appsettings.json` - Base development settings
- `StampApi/appsettings.Staging.json` - Staging template
- `StampApi/appsettings.Production.json` - Production template
- `StampBlazor/wwwroot/appsettings.json` - Base client settings
- `StampBlazor/wwwroot/appsettings.Staging.json` - Staging client template
- `StampBlazor/wwwroot/appsettings.Production.json` - Production client template

### ❌ Never Commit (Actual Secrets)

These file patterns are excluded via `.gitignore`:

- `**/appsettings.local.json` - Local developer overrides
- `**/appsettings.*.local.json` - Environment-specific local overrides
- `secrets.json` - User secrets file
- `deployment-outputs-*.json` - Azure deployment outputs

## How Configuration Works

### Development Environment
```json
// appsettings.json (committed)
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=stamp.db"
  },
  "Authentication": {
    "Google": {
      "ClientId": "dev-client-id",
      "ClientSecret": "dev-client-secret"
    }
  }
}
```

### Azure Environments (Staging/Production)
```json
// appsettings.Production.json (committed template)
{
  "ConnectionStrings": {
    "DefaultConnection": "PLACEHOLDER_WILL_BE_REPLACED_BY_KEYVAULT"
  },
  "Authentication": {
    "Google": {
      "ClientId": "PLACEHOLDER_WILL_BE_REPLACED_BY_KEYVAULT",
      "ClientSecret": "PLACEHOLDER_WILL_BE_REPLACED_BY_KEYVAULT"
    }
  }
}
```

At runtime in Azure:
1. **App Service Configuration** overrides placeholder values with actual settings

## Local Development Setup

### Option 1: User Secrets (Recommended)
```bash
# Navigate to API project
cd StampApi

# Initialize user secrets
dotnet user-secrets init

# Add your local secrets
dotnet user-secrets set "Authentication:Google:ClientId" "your-local-client-id"
dotnet user-secrets set "Authentication:Google:ClientSecret" "your-local-client-secret"
```

### Option 2: Local Override File
Create `StampApi/appsettings.local.json` (automatically ignored by git):
```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your-local-client-id",
      "ClientSecret": "your-local-client-secret"
    }
  }
}
```

## Azure Configuration Management

### App Service Configuration
Configure these settings in your Azure App Service:
- `ConnectionStrings__DefaultConnection` - Azure SQL Database connection string
- `Authentication__Jwt__Key` - JWT signing key (minimum 32 characters)
- `Authentication__Google__ClientId` - Google OAuth client ID
- `Authentication__Google__ClientSecret` - Google OAuth client secret
- `AllowedOrigins__0` - Your Blazor app URL for CORS

### How it works:
1. Azure App Service loads the appropriate `appsettings.{Environment}.json` file
2. App Service Configuration settings override the placeholder values
3. Your application gets the real values at runtime

## Security Best Practices

### ✅ Do This
- Commit template files with placeholders
- Use App Service Configuration for production secrets
- Use User Secrets for local development
- Rotate secrets regularly
- Use HTTPS for all communication
- Monitor application logs for security issues

### ❌ Don't Do This
- Commit files with actual secrets
- Share secrets via chat or email
- Use production secrets in development
- Hardcode secrets in application code
- Use HTTP in production

## Troubleshooting Configuration

### Local Development Issues
```bash
# Check what configuration is loaded
dotnet run --project StampApi --verbosity detailed

# List user secrets
dotnet user-secrets list --project StampApi

# Clear user secrets if needed
dotnet user-secrets clear --project StampApi
```

### Azure Environment Issues
1. **Check App Service Configuration**: Azure Portal > App Service > Configuration
2. **Review Application Logs**: Use App Service logs or Application Insights if configured
3. **Test Database Connection**: Verify connection string and firewall rules
4. **Check Health Endpoint**: Visit `/health` on your API app

### Common Error Messages
- **"Unable to decrypt the message"**: JWT key mismatch between environments
- **"Cannot open database"**: SQL connection string issue or firewall blocking
- **"CORS policy"**: AllowedOrigins configuration mismatch
- **500 Internal Server Error**: Check app logs for specific error details

## Configuration Checklist

### Before Committing Code
- [ ] No actual secrets in any committed files
- [ ] All sensitive values use placeholders
- [ ] .gitignore covers all secret file patterns
- [ ] Local development works with user secrets or local override

### Before Deploying to Azure
- [ ] Azure SQL Database created and accessible
- [ ] App Service Configuration has all required settings
- [ ] CORS origins match deployed URLs
- [ ] Environment-specific settings are correct

### After Deployment
- [ ] Application starts successfully (check `/health` endpoint)
- [ ] Database connectivity works
- [ ] Authentication flow completes
- [ ] API endpoints respond correctly
- [ ] Blazor client can communicate with API