# 🔧 SendGrid Setup - Fix Missing API Key

## ❌ Current Problem

```
ApiKey: "your-sendgrid-api-key-here"  ← This is a placeholder!
```

This is why you're getting the authorization error in SendGrid!

---

## ✅ Solution: Get & Configure Real API Key

### Step 1: Log In to SendGrid 🔐

```
1. Go to: https://sendgrid.com
2. Click "Sign In"
3. Enter your email & password
4. Log in
```

### Step 2: Navigate to API Keys Section 🗝️

```
1. In SendGrid Dashboard, go to: Settings
2. Look for: "API Keys"
3. Or direct link: https://app.sendgrid.com/settings/api_keys
```

### Step 3: Create New API Key 🆕

```
1. Click: "Create API Key" (or "New API Key")
2. Name it: "EduConnect"
3. Select permissions: "Mail Send" ✅
4. Click "Create"
5. IMPORTANT: Copy the key immediately! 
   (You can't see it again - write it down or save it)
```

Your key will look like:
```
SG.xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

### Step 4: Update appsettings.json 📝

Replace the placeholder with your REAL key:

**Before:**
```json
"SendGrid": {
  "ApiKey": "your-sendgrid-api-key-here",
  "FromEmail": "noreply@educonnect.com",
  "FromName": "EduConnect"
}
```

**After:**
```json
"SendGrid": {
  "ApiKey": "SG.your-actual-key-here-xxxxx",
  "FromEmail": "noreply@yourdomain.com",
  "FromName": "EduConnect"
}
```

---

## 🏗️ For Render (Production)

If you're deploying to Render:

### Step 1: Add Environment Variable

```
1. Go to Render Dashboard
2. Select your service
3. Go to "Environment" tab
4. Click "Add Environment Variable"
5. Name: SENDGRID_API_KEY
6. Value: SG.your-actual-key-here-xxxxx
7. Click "Save"
```

### Step 2: Update Code

Your `Program.cs` should read from environment:

```csharp
var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY") 
    ?? builder.Configuration["SendGrid:ApiKey"];
```

### Step 3: Never Commit API Key!

Add to `.gitignore`:
```
appsettings.Development.json
appsettings.*.json
```

---

## ✅ Verify It Works

### Test 1: Check if Key is Valid

Run this in terminal:
```bash
curl -X GET "https://api.sendgrid.com/v3/mail/validate" \
  -H "Authorization: Bearer YOUR_API_KEY"
```

Should return: `{"result":true}`

### Test 2: Test in Your App

1. Update `appsettings.json` with real key
2. Restart the application:
   ```bash
   dotnet run --urls "http://localhost:8000"
   ```
3. Go to your app
4. Enroll in a course
5. Check your email ✉️

### Test 3: Check SendGrid Dashboard

1. Log in to SendGrid
2. Go to "Mail Activity"
3. You should see your test email there!

---

## 🐛 Troubleshooting

### Problem: "Unauthorized" or "Invalid Key"
**Solution:**
- [ ] Copy key again (without extra spaces)
- [ ] Make sure it starts with "SG."
- [ ] Restart the application
- [ ] Check if key was revoked in SendGrid

### Problem: "Email not sent"
**Solution:**
- [ ] Check SendGrid Mail Activity for errors
- [ ] Verify "From" email is valid
- [ ] Check logs: `dotnet run > logs.txt 2>&1`

### Problem: "Too many requests"
**Solution:**
- Free tier: 100 emails/day
- Upgrade SendGrid plan if needed

---

## 📋 Checklist

- [ ] Created SendGrid account
- [ ] Logged in to SendGrid
- [ ] Found API Keys section
- [ ] Created new API Key named "EduConnect"
- [ ] Copied the key (starts with "SG.")
- [ ] Updated `appsettings.json` with real key
- [ ] Restarted application
- [ ] Tested enrollment email
- [ ] Verified in SendGrid Mail Activity

---

## 🎯 Quick Reference

| Item | Value |
|------|-------|
| SendGrid Dashboard | https://app.sendgrid.com |
| API Keys Section | https://app.sendgrid.com/settings/api_keys |
| Mail Activity | https://app.sendgrid.com/mail_activity |
| Key Format | `SG.xxxxxxxxxxxxxxxxxxxxxx` |
| Key Permission | "Mail Send" only |
| Expiry | Never (unless you delete it) |

---

## ✨ You're All Set!

Once you:
1. ✅ Get real SendGrid API key
2. ✅ Update appsettings.json
3. ✅ Restart app

**Emails will work!** 📧✅

