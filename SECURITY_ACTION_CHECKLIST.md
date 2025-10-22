# 🔐 API KEY SECURITY - Action Checklist

## ✅ What We've Done (Completed)

- [x] Removed API key from `appsettings.json`
- [x] Updated `.gitignore` with sensitive files
- [x] Created `appsettings.Development.json` (local config)
- [x] Created security documentation
- [x] Pushed secure configuration to GitHub

---

## 🚨 WHAT YOU MUST DO IMMEDIATELY

### Task 1: Revoke Compromised Key (URGENT!)

```
⏱️ Time: 2 minutes
🔗 Link: https://app.sendgrid.com/settings/api_keys
```

**Steps:**
1. Log in to SendGrid
2. Go to Settings → API Keys
3. Find key: `BMRrAbiMMBblfBOMEmgu4esx6PkH96sw`
4. Click the **Delete** button
5. Confirm deletion

**Why:** This key is now PUBLIC on GitHub!

---

### Task 2: Create NEW Test Key (for local development)

```
⏱️ Time: 2 minutes
🔗 Link: https://app.sendgrid.com/settings/api_keys
```

**Steps:**
1. Click **Create API Key**
2. Name: `Local Development`
3. Permissions: Select `Mail Send` only
4. Click **Create**
5. **COPY the key immediately** (you won't see it again)

**Result:** Key looks like `SG.abc123...`

**Next:** Add to `appsettings.Development.json` locally (don't commit!)

---

### Task 3: Create NEW Production Key (for Render)

```
⏱️ Time: 2 minutes
🔗 Link: https://app.sendgrid.com/settings/api_keys
```

**Steps:**
1. Click **Create API Key**
2. Name: `EduConnect Production`
3. Permissions: Select `Mail Send` only
4. Click **Create**
5. **COPY the key immediately**

**Result:** Key looks like `SG.xyz789...`

**Next:** Add to Render environment variables

---

### Task 4: Add New Production Key to Render

```
⏱️ Time: 2 minutes
🔗 Link: https://dashboard.render.com
```

**Steps:**
1. Go to Render Dashboard
2. Select your EduConnect service
3. Go to **Environment** tab
4. Click **Add Environment Variable**
5. Name: `SENDGRID_API_KEY`
6. Value: `SG.xyz789...` (your new production key)
7. Click **Save**
8. Service auto-redeploys

---

### Task 5: Add New Test Key Locally

```
⏱️ Time: 1 minute
🔗 File: appsettings.Development.json (local only)
```

**Steps:**
1. Open: `appsettings.Development.json`
2. Find: `"ApiKey": "your-sendgrid-api-key-here-for-local-dev"`
3. Replace with: `"ApiKey": "SG.abc123..."` (your test key)
4. Save file
5. **DO NOT COMMIT THIS FILE** (it's in .gitignore)

---

### Task 6: Test Locally

```
⏱️ Time: 3 minutes
```

**Steps:**
1. Stop current application (Ctrl+C)
2. Restart application:
   ```bash
   dotnet run --urls "http://localhost:8000"
   ```
3. Log in as student
4. Enroll in a course
5. Check your email ✉️

**Expected:** Email received with enrollment confirmation

---

### Task 7: Test on Production (Render)

```
⏱️ Time: 5 minutes
```

**Steps:**
1. Go to your Render deployed URL
2. Log in as student
3. Enroll in a course
4. Check your email ✉️

**Expected:** Email received on your production instance

---

## 📋 Complete Checklist

### Security Fixes (DONE ✅)
- [x] API key removed from git
- [x] .gitignore updated
- [x] Development config created
- [x] Security docs created
- [x] Changes pushed to GitHub

### Required Actions (TODO 🔄)
- [ ] Revoke compromised key in SendGrid
- [ ] Create new test key
- [ ] Create new production key
- [ ] Add production key to Render
- [ ] Add test key locally
- [ ] Test locally
- [ ] Test on production

### Verification (VERIFY ✓)
- [ ] Local emails working
- [ ] Production emails working
- [ ] No API key in git
- [ ] No API key in .gitignore files
- [ ] Render environment variable set

---

## 🔍 Verification Commands

### Check git history (should NOT see API key)
```bash
git log --oneline -20
```

### Check if API key is in committed files
```bash
git grep "SG\..*[0-9a-zA-Z]"
```
Should return: `(nothing)` - no API keys found

### Check .gitignore is working
```bash
git status
```
Should NOT show `appsettings.Development.json`

---

## ⏰ Timeline

| Task | Time | Status |
|------|------|--------|
| Revoke Key | 2 min | ⏳ TODO |
| New Test Key | 2 min | ⏳ TODO |
| New Prod Key | 2 min | ⏳ TODO |
| Add to Render | 2 min | ⏳ TODO |
| Add Locally | 1 min | ⏳ TODO |
| Test Local | 3 min | ⏳ TODO |
| Test Prod | 5 min | ⏳ TODO |
| **TOTAL** | **~20 min** | ⏳ TODO |

---

## 🚨 Important Reminders

### DO ✅
- [ ] Revoke the old key immediately
- [ ] Keep new keys SECRET
- [ ] Use different keys for dev & production
- [ ] Check .gitignore is protecting files
- [ ] Monitor SendGrid for suspicious activity

### DON'T ❌
- [ ] Don't share API keys via email/chat
- [ ] Don't commit appsettings.Development.json
- [ ] Don't use same key for local & production
- [ ] Don't ignore git warnings about commits
- [ ] Don't reuse revoked keys

---

## 📞 If Something Goes Wrong

### Issue: "Unauthorized" error
**Solution:**
- Check key is correct (no spaces)
- Check key starts with "SG."
- Verify SendGrid account access
- Generate new key if needed

### Issue: "Email not sending"
**Solution:**
- Check Render environment variable is set
- Check local appsettings.Development.json is updated
- Restart application
- Check SendGrid Mail Activity for errors

### Issue: "API key still public"
**Solution:**
- Can't remove from git history (too late)
- Must revoke key immediately
- GitHub notifies if secrets detected
- Check GitHub security alerts

---

## ✨ After Completing All Tasks

Your application will be:
- ✅ Fully secured
- ✅ API key not exposed
- ✅ Proper configuration per environment
- ✅ Following best practices
- ✅ Ready for production

**Estimated Time to Complete: 20 minutes** ⏱️

---

## 📚 Documentation

See these files for more info:
- `SECURITY_CONFIG.md` - Complete guide
- `SECURITY_FIXES_APPLIED.md` - What we did
- `.gitignore` - Protected files

---

## 🎯 Start Now!

**Your immediate action:** Revoke the compromised key! 🔐

Link: https://app.sendgrid.com/settings/api_keys

