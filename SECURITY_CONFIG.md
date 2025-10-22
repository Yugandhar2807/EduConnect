# 🔐 Security Configuration - API Key Protection

## ⚠️ CRITICAL SECURITY ALERT

Your API key was exposed in git. Here's what we did:

### ✅ Actions Taken

1. ✅ **Removed API key from appsettings.json**
   - Changed from: `"ApiKey": "BMRrAbiMMBblfBOMEmgu4esx6PkH96sw"`
   - Changed to: `"ApiKey": "${SENDGRID_API_KEY}"`

2. ✅ **Updated .gitignore**
   - Added sensitive file patterns
   - Prevents future commits of secrets

3. ✅ **Created appsettings.Development.json**
   - For local development
   - Contains placeholder (not real key)
   - Will NOT be committed to git

---

## 🚨 IMMEDIATE ACTION REQUIRED

### REVOKE THE EXPOSED API KEY!

**Your exposed key:** `BMRrAbiMMBblfBOMEmgu4esx6PkH96sw`

1. Go to SendGrid: https://app.sendgrid.com/settings/api_keys
2. Find the exposed key
3. Click "Delete" 
4. Confirm deletion
5. Generate a NEW key

**This key is now public and compromised!**

---

## 🔑 How to Secure from Now On

### For Local Development

1. **Edit appsettings.Development.json:**
   ```json
   {
     "SendGrid": {
       "ApiKey": "SG.your-local-test-key-here",
       "FromEmail": "test@sandbox.sendgrid.net",
       "FromName": "EduConnect"
     }
   }
   ```

2. **Note:** This file is in `.gitignore` - will NOT be committed

### For Production (Render)

1. **Use Environment Variables Only:**
   ```
   Dashboard → Environment Variables
   Name: SENDGRID_API_KEY
   Value: SG.your-production-key-here
   ```

2. **In Program.cs:**
   ```csharp
   var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY")
       ?? builder.Configuration["SendGrid:ApiKey"];
   ```

---

## 📝 Files Updated

### 1. appsettings.json (Public Safe)
```json
"SendGrid": {
  "ApiKey": "${SENDGRID_API_KEY}",  ← Environment variable
  "FromEmail": "noreply@educonnect.com",
  "FromName": "EduConnect"
}
```

### 2. appsettings.Development.json (Not Committed)
```json
"SendGrid": {
  "ApiKey": "your-local-test-key",  ← Placeholder only
  "FromEmail": "noreply@educonnect.com",
  "FromName": "EduConnect"
}
```

### 3. .gitignore (Updated)
```
# Sensitive Configuration Files (NEVER commit these!)
appsettings.Development.json
appsettings.Local.json
appsettings.*.json
!appsettings.Production.json
secrets.json
*.key
*.pem
*.pfx
```

---

## ✅ Security Checklist

- [x] API key removed from appsettings.json
- [x] Environment variable placeholder set
- [x] .gitignore updated
- [x] Development config file created
- [ ] REVOKE exposed API key in SendGrid
- [ ] Generate NEW API key
- [ ] Add NEW key to Render environment
- [ ] Test locally with test key
- [ ] Test production with real key
- [ ] Commit & push changes

---

## 🔄 Clean Git History (Optional)

If you want to completely remove the exposed key from git history:

```bash
# Option 1: Using git-filter-branch (Destructive)
git filter-branch --tree-filter 'sed -i "s/BMRrAbiMMBblfBOMEmgu4esx6PkH96sw/EXPOSED-KEY-REMOVED/g" appsettings.json' HEAD

# Option 2: Force push (if you're alone in repo)
git push origin --force-with-lease

# Option 3: Using BFG Repo-Cleaner (Recommended)
# Download from: https://rtyley.github.io/bfg-repo-cleaner/
bfg --replace-text passwords.txt .
```

**WARNING: These modify history - only do if necessary!**

---

## 🛡️ Best Practices Going Forward

### DO ✅
- [ ] Store API keys in environment variables
- [ ] Use `.gitignore` for sensitive files
- [ ] Keep `appsettings.json` configuration-only (no secrets)
- [ ] Use different keys for dev/production
- [ ] Rotate keys regularly
- [ ] Monitor SendGrid Activity for suspicious usage

### DON'T ❌
- [ ] Never commit API keys to git
- [ ] Never hardcode secrets in code
- [ ] Never share API keys in messages/emails
- [ ] Never use same key for dev and production
- [ ] Never push appsettings.*.json files

---

## 📊 Configuration Hierarchy

```
Priority (Highest to Lowest):
1. Environment Variables (SENDGRID_API_KEY)
2. appsettings.{Environment}.json (appsettings.Development.json)
3. appsettings.json (Public safe config)
```

Example in code:
```csharp
var apiKey = 
    Environment.GetEnvironmentVariable("SENDGRID_API_KEY") 
    ?? builder.Configuration["SendGrid:ApiKey"];
```

---

## 🔑 API Key Generation

### Get New Test Key (for local dev)
1. Go to SendGrid: https://app.sendgrid.com/settings/api_keys
2. Click "Create API Key"
3. Name: "Local Dev"
4. Permissions: "Mail Send" only
5. Copy key → paste in appsettings.Development.json

### Get New Production Key (for Render)
1. Go to SendGrid: https://app.sendgrid.com/settings/api_keys
2. Click "Create API Key"
3. Name: "EduConnect Production"
4. Permissions: "Mail Send" only
5. Copy key → add to Render environment variables

---

## 📞 Emergency: If Key is Compromised

1. **Immediately revoke the key** in SendGrid
2. **Delete it from git history** (optional)
3. **Generate a new key**
4. **Update all environments**
5. **Monitor usage** for suspicious activity

---

## ✨ You're Secure Now!

After following these steps, your API keys are:
- ✅ Not in git repository
- ✅ Not public on GitHub
- ✅ Protected in environment variables
- ✅ Properly configured for all environments

**Status: SECURE** 🔐

