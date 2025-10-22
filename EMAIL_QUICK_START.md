# 🚀 Email Integration - Quick Start (5 Minutes)

## Step 1: Create SendGrid Account (1 min)

```bash
1. Go to: https://sendgrid.com
2. Click "Sign Up"
3. Create account (FREE tier)
4. Verify email
```

## Step 2: Get API Key (2 min)

```bash
1. Log in to SendGrid Dashboard
2. Go to: Settings → API Keys
3. Click: "Create API Key"
4. Name it: "EduConnect"
5. Copy the key (it starts with "SG.")
6. Save it somewhere safe
```

## Step 3: Configure in Render (1 min)

```bash
1. Go to: https://dashboard.render.com
2. Select your EduConnect service
3. Go to: "Environment" tab
4. Add new environment variable:
   Key: SENDGRID_API_KEY
   Value: SG.xxxxxxxxxxx (paste your key)
5. Click "Save"
6. Service auto-redeploys
```

## Step 4: Update appsettings.Production.json (1 min)

File: `appsettings.Production.json`

Add this:
```json
{
  "SendGrid": {
    "ApiKey": "${SENDGRID_API_KEY}",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "EduConnect"
  }
}
```

## ✅ Done! Emails Now Work

**Test it:**
1. Go to http://your-render-url
2. Login as student
3. Enroll in a course
4. **Check your email!** 📬

---

## 🧪 Test Locally (Optional)

Edit `appsettings.Development.json`:

```json
{
  "SendGrid": {
    "ApiKey": "SG.xxxxxxxxxxx",
    "FromEmail": "test@sandbox.sendgrid.net",
    "FromName": "EduConnect"
  }
}
```

Run locally:
```bash
dotnet run --urls "http://localhost:8000"
```

---

## 📊 Monitor Emails

Go to: **SendGrid Dashboard → Mail Activity**

See:
- ✅ Delivered emails
- 👁️ Opens
- 🔗 Clicks
- ❌ Bounces

---

## 🎯 What Emails Send Automatically

| When | Email |
|------|-------|
| Student enrolls | ✅ Enrollment confirmation |
| Student takes quiz | ✅ Grade notification |
| Admin posts announcement | 🔄 Ready (see guide) |
| New user registers | 🔄 Ready (see guide) |

---

## 📚 Documentation

More details:
- Read: `EMAIL_NOTIFICATIONS_GUIDE.md`
- See: `EMAIL_INTEGRATION_SUMMARY.md`

---

## ⚠️ Common Issues

### "Emails not sending"
→ Check SendGrid dashboard for errors

### "Invalid API key"
→ Copy key again (without spaces)

### "Emails going to spam"
→ Need to verify your domain in SendGrid (advanced)

---

## 🎉 That's It!

Your EduConnect now sends professional emails automatically! 

**What's Next:**
- Announcements notifications
- Password reset emails
- Course completion certificates
- Progress reminders

See `ENHANCEMENT_ROADMAP.md` for more features!

