# Configuration Setup Instructions

## ⚠️ Important: Sensitive Configuration

This project requires sensitive API keys and credentials that are **NOT included** in the repository for security reasons.

## Required Configuration Files

### 1. appsettings.Development.json

Create this file in `Ecoomerce.Web/` directory (copy from `appsettings.Development.json.example`):

```json
{
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-gmail-app-password"
  },
  "Stripe": {
    "PublishableKey": "pk_test_your_key_here",
    "SecretKey": "sk_test_your_key_here",
    "WebhookSecret": "whsec_your_webhook_secret"
  },
  "Authentication": {
    "Google": {
      "ClientId": "your-client-id.apps.googleusercontent.com",
      "ClientSecret": "your-client-secret"
    }
  }
}
```

### 2. Get Your API Keys

#### Stripe (Payment Processing)
1. Create account at https://stripe.com
2. Get test keys from Dashboard → Developers → API keys
3. For webhooks: Use Stripe CLI or Dashboard → Developers → Webhooks
4. **IMPORTANT**: Use TEST keys (pk_test_*, sk_test_*) for development

#### Google OAuth (Social Login)
1. Go to https://console.cloud.google.com
2. Create project → Enable Google+ API
3. Credentials → Create OAuth 2.0 Client ID
4. Add authorized redirect URI: `https://localhost:5001/signin-google`

#### Gmail SMTP (Email Sending)
1. Go to Google Account → Security
2. Enable 2-Step Verification
3. Generate App Password for "Mail"
4. Use the 16-character password (not your Gmail password)

### 3. .gitignore Configuration

The following files are already in `.gitignore` and should NEVER be committed:

```
appsettings.Development.json
appsettings.Production.json
*.user
*.suo
```

### 4. Production Deployment

For production, use environment variables or Azure Key Vault instead of appsettings.json:

**Azure App Service**:
- Configuration → Application settings
- Add each key as environment variable

**Environment Variables** (format: `SectionName__SubKey`):
```
Stripe__SecretKey=sk_live_your_production_key
Authentication__Google__ClientSecret=your_secret
```

## Security Best Practices

1. ✅ **Never commit real API keys** to Git
2. ✅ **Use test keys** for development (pk_test_*, sk_test_*)
3. ✅ **Use environment variables** for production
4. ✅ **Rotate keys** if accidentally exposed
5. ✅ **Use different keys** for dev/staging/production

## What's Safe to Commit?

✅ **SAFE**:
- appsettings.json (with placeholder values)
- appsettings.Development.json.example
- This README

❌ **NEVER COMMIT**:
- appsettings.Development.json (real values)
- appsettings.Production.json (real values)
- Any file containing real API keys or passwords

## If You Accidentally Committed Secrets

1. **Immediately rotate/revoke** the exposed keys:
   - Stripe: Dashboard → Developers → API keys → Roll key
   - Google: Delete OAuth client, create new one

2. **Remove from Git history**:
   ```bash
   git filter-branch --force --index-filter \
     "git rm --cached --ignore-unmatch Ecoomerce.Web/appsettings.json" \
     --prune-empty --tag-name-filter cat -- --all
   
   git push origin --force --all
   ```

3. **Notify your team** if it's a shared repository

## Need Help?

- Stripe docs: https://stripe.com/docs/keys
- Google OAuth: https://developers.google.com/identity/protocols/oauth2
- ASP.NET Core configuration: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/
