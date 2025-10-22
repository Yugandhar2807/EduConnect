# Email Configuration Debug Guide

## Issue: Emails Not Sending

### Current Configuration
- **API Key:** `Xosfvn7S7BPtLrO7Xa3B64FMrSQo37LO`
- **Status:** ❌ INVALID FORMAT
- **Expected Format:** `SG.xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx...`

### Why It's Not Working

The API key format is WRONG. Valid SendGrid API keys:
1. Always start with `SG.`
2. Are typically 80-100+ characters long
3. Can be found at: https://app.sendgrid.com/settings/api_keys

### Steps to Fix

#### Step 1: Get Your Correct API Key
```
1. Go to: https://app.sendgrid.com/settings/api_keys
2. Find: "Local Development" key (the one you just created)
3. Click on it to view/copy the full key
4. Make sure it STARTS with "SG."
5. Make sure you copy the ENTIRE key (it's long!)
```

#### Step 2: Update appsettings.Development.json
```json
{
  "SendGrid": {
    "ApiKey": "SG.YOUR_FULL_KEY_HERE_THAT_YOU_JUST_COPIED",
    "FromEmail": "noreply@educonnect.com",
    "FromName": "EduConnect"
  }
}
```

#### Step 3: Restart App
```bash
# Stop current app (Ctrl+C)
# Start new app
dotnet run
```

#### Step 4: Test Email
```
1. Go to http://localhost:8000
2. Login as student
3. Enroll in a course
4. Check your email for confirmation
```

### Example of Valid Key Format
```
SG.WS6gA_F_SDkdqCT1_i2XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
```

Notice:
- ✅ Starts with `SG.`
- ✅ Very long (80+ characters)
- ✅ Contains mix of letters, numbers, underscores

### Current Problem
```
Xosfvn7S7BPtLrO7Xa3B64FMrSQo37LO
```
- ❌ Doesn't start with `SG.`
- ❌ Too short (32 characters vs expected 80+)
- ❌ Wrong format entirely

### Possible Solutions

**Option A: Copy Full Key Correctly**
- Maybe you only copied part of the key
- Go back to SendGrid and copy the COMPLETE key

**Option B: Regenerate Key**
- Delete the current "Local Development" key
- Create a new one
- Copy the FULL key immediately (it won't be shown again)

**Option C: Check SendGrid Account**
- Make sure your SendGrid account is active
- Check if keys are being generated properly
- Test key in SendGrid interface first

### How to Verify Key is Correct

After getting the right key, it should:
1. Start with `SG.`
2. Be 85+ characters long
3. Work in SendGrid's API interface

### Contact SendGrid Support
If you're having issues getting valid keys:
- Visit: https://support.sendgrid.com
- Check API key documentation: https://docs.sendgrid.com/ui/account-and-settings/api-keys

---

## Next Action Required

**URGENT:** Get your correct API key from SendGrid and provide it in this format:

```
SG.xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

Once you have the correct key, I'll update the configuration and test emails!
