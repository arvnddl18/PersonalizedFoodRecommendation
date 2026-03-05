# OAuth Integration Setup Guide

## Overview
Your Nomsaur application now supports OAuth authentication with Google and Facebook, in addition to the existing email/password authentication. This guide will help you configure the OAuth providers.

## What's Been Implemented

### 1. Database Changes
- Added OAuth fields to User model:
  - `Provider`: "local", "google", or "facebook"
  - `ProviderId`: OAuth provider's user ID
  - `ProviderData`: JSON data from OAuth provider

### 2. Authentication Flow
- **New Users**: Automatically created when they first login with OAuth
- **Existing Users**: OAuth info is linked to existing email accounts
- **Session Management**: Uses your existing session system
- **Profile Redirect**: Same flow as email/password (Preferences → Chat)

### 3. UI Updates
- Added functional OAuth buttons to both login and register pages
- Maintains existing design and mobile responsiveness
- Users can now register OR login using OAuth providers

## Setup Instructions

### Step 1: Google OAuth Setup

1. **Go to Google Cloud Console**
   - Visit: https://console.cloud.google.com/
   - Create a new project or select existing one

2. **Enable Google+ API**
   - Go to "APIs & Services" → "Library"
   - Search for "Google+ API" and enable it

3. **Create OAuth Credentials**
   - Go to "APIs & Services" → "Credentials"
   - Click "Create Credentials" → "OAuth 2.0 Client IDs"
   - Application type: "Web application"
   - Authorized redirect URIs: `https://yourdomain.com/auth/google-callback`
   - For local development: `https://localhost:7000/auth/google-callback`

4. **Update Configuration**
   ```json
   "Authentication": {
     "Google": {
       "ClientId": "YOUR_GOOGLE_CLIENT_ID",
       "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
     }
   }
   ```

### Step 2: Facebook OAuth Setup

1. **Go to Facebook Developers**
   - Visit: https://developers.facebook.com/
   - Create a new app or use existing one

2. **Add Facebook Login Product**
   - In your app dashboard, click "Add Product"
   - Find "Facebook Login" and click "Set Up"

3. **Configure OAuth Settings**
   - Go to "Facebook Login" → "Settings"
   - Valid OAuth Redirect URIs: `https://yourdomain.com/auth/facebook-callback`
   - For local development: `https://localhost:7000/auth/facebook-callback`

4. **Get App Credentials**
   - Go to "Settings" → "Basic"
   - Copy App ID and App Secret

5. **Update Configuration**
   ```json
   "Authentication": {
     "Facebook": {
       "AppId": "YOUR_FACEBOOK_APP_ID",
       "AppSecret": "YOUR_FACEBOOK_APP_SECRET"
     }
   }
   ```

### Step 3: Apply Database Migration

Run the migration to add OAuth fields to your database:

```bash
dotnet ef database update
```

### Step 4: Test the Integration

1. **Start your application**
   ```bash
   dotnet run
   ```

2. **Test OAuth Login/Registration**
   - Go to login page → Click Google or Facebook button → Complete OAuth flow
   - Go to register page → Click Google or Facebook button → Complete OAuth flow
   - Verify user is created/linked correctly in both cases

## Security Considerations

### 1. HTTPS Required
- OAuth providers require HTTPS in production
- Use ngrok for local testing: `ngrok http 7000`

### 2. Environment Variables
- Store OAuth secrets in environment variables or user secrets
- Never commit secrets to source control

### 3. User Data Privacy
- OAuth providers only share basic info (name, email)
- Additional data is stored in your `ProviderData` field

## Troubleshooting

### Common Issues

1. **"Invalid redirect URI"**
   - Check that redirect URIs match exactly in OAuth provider settings
   - Ensure HTTPS is used in production

2. **"App not verified"**
   - Facebook apps need verification for production use
   - Google apps may show warning for unverified apps

3. **"Email not provided"**
   - Some OAuth providers require additional permissions
   - Check OAuth scope settings

### Debug Steps

1. Check application logs for OAuth errors
2. Verify OAuth provider settings match your configuration
3. Test with different browsers/incognito mode
4. Check network tab for OAuth redirect issues

## Integration Benefits

### For Users
- **Faster Registration**: No need to create new passwords
- **Familiar Login**: Use existing Google/Facebook accounts
- **Security**: Leverage OAuth providers' security measures

### For Your System
- **Reduced Friction**: More users likely to register
- **Email Verification**: OAuth providers verify emails
- **User Data**: Access to verified user information
- **Compatibility**: Works alongside existing email/password system

## Next Steps

1. Configure OAuth providers with your credentials
2. Test the integration thoroughly
3. Consider adding more OAuth providers (Microsoft, Twitter, etc.)
4. Implement user account linking for users who want to add OAuth to existing accounts

## Support

If you encounter issues:
1. Check the troubleshooting section above
2. Review OAuth provider documentation
3. Check application logs for detailed error messages
4. Test with a fresh browser session

The OAuth integration is designed to be seamless with your existing authentication system while providing users with more convenient login options.
