# Email Notifications Integration Guide

## Overview
EduConnect now includes email notifications for key events like course enrollment, quiz completion, and announcements.

---

## ✅ Features Implemented

### 1. **Enrollment Confirmation Email**
- **Trigger**: When a student successfully enrolls in a course
- **Content**: Welcome message, course details, access information
- **Recipient**: Enrolled student

### 2. **Quiz Grade Notification Email**
- **Trigger**: Immediately after student submits a quiz
- **Content**: Score, percentage, letter grade (A-F)
- **Recipient**: Student who took the quiz

### 3. **Email Service Interface**
Extensible interface (`IEmailService`) supporting:
- Single email sending
- Bulk email sending
- Announcement distribution
- Welcome emails
- Password reset emails
- Certificate emails

---

## 🔧 Setup Instructions

### Step 1: Create SendGrid Account

1. Go to https://sendgrid.com
2. Sign up for a free account (30 credits/month)
3. Verify your email
4. Go to **Settings → API Keys**
5. Create a new API Key
6. Copy the API Key (save it securely!)

### Step 2: Configure API Key

#### Option A: Development (Local Testing)

Edit `appsettings.Development.json`:
```json
{
  "SendGrid": {
    "ApiKey": "SG.xxxxxxxxxxxx_YOUR_KEY_HERE",
    "FromEmail": "noreply@educonnect.com",
    "FromName": "EduConnect"
  }
}
```

#### Option B: Production (Render.com)

1. Go to your Render dashboard
2. Select your service
3. Go to **Environment** tab
4. Add these variables:
```
SENDGRID_APIKEY=SG.xxxxxxxxxxxx_YOUR_KEY_HERE
```

Then update `appsettings.Production.json` or use:
```csharp
var apiKey = Environment.GetEnvironmentVariable("SENDGRID_APIKEY");
```

### Step 3: Sender Email Configuration

You have two options:

**Option 1: Use SendGrid's Sandbox (Testing Only)**
- Emails go nowhere (for testing)
- Use: `noreply@sandbox.sendgrid.net`

**Option 2: Use Verified Domain (Production)**
1. In SendGrid Dashboard → **Settings → Sender Authentication**
2. Verify your domain or single sender
3. Update `FromEmail` in appsettings

---

## 📧 Email Triggers

### When Are Emails Sent?

| Event | Email Type | Recipient | When |
|-------|-----------|-----------|------|
| Student enrolls in course | Enrollment Confirmation | Student | Immediately after enrollment |
| Student submits quiz | Grade Notification | Student | Immediately after submission |
| Course marked complete | Certificate Email | Student | When progress = 100% |
| New announcement posted | Announcement | All enrolled students | When faculty creates announcement |
| User account created | Welcome Email | New user | Registration complete |
| Password reset requested | Reset Link | User | Password reset flow |

---

## 🧪 Testing Email Notifications

### Test Enrollment Email

```csharp
// In StudentController.cs
await _emailService.SendEnrollmentConfirmationAsync(
    "teststudent@example.com",
    "Test Student",
    "Introduction to C#"
);
```

### Test Grade Email

```csharp
// In StudentController.cs
await _emailService.SendGradeNotificationAsync(
    "teststudent@example.com",
    "Test Student",
    "Introduction to C#",
    "Quiz 1",
    85,  // score
    100  // total marks
);
```

### Local Testing (Without Real Emails)

Install Fake SendGrid CLI for testing:
```bash
npm install -g @sendgrid/mail-faker
```

Or use SendGrid's Sandbox mode (free tier).

---

## 📝 Available Email Methods

### IEmailService Interface

```csharp
// Send simple email
Task<bool> SendEmailAsync(
    string toEmail,
    string subject,
    string htmlContent);

// Send to multiple recipients
Task<bool> SendBulkEmailAsync(
    List<string> toEmails,
    string subject,
    string htmlContent);

// Send enrollment confirmation
Task SendEnrollmentConfirmationAsync(
    string studentEmail,
    string studentName,
    string courseName);

// Send grade notification
Task SendGradeNotificationAsync(
    string studentEmail,
    string studentName,
    string courseName,
    string quizName,
    int score,
    int totalPoints);

// Send announcement to class
Task SendAnnouncementAsync(
    List<string> studentEmails,
    string courseName,
    string announcementTitle,
    string announcementContent,
    string facultyName);

// Send welcome email to new user
Task SendWelcomeEmailAsync(
    string email,
    string fullName,
    string role);

// Send password reset link
Task SendPasswordResetEmailAsync(
    string email,
    string resetLink);

// Send certificate
Task SendCertificateEmailAsync(
    string studentEmail,
    string studentName,
    string courseName,
    DateTime completionDate);
```

---

## 🚀 Deploying to Production

### Option 1: Deploy to Render (Recommended)

1. **Add SendGrid API Key to Render Environment:**
   ```bash
   SENDGRID_APIKEY=SG.xxxxxxxxxxxx_YOUR_KEY_HERE
   ```

2. **Update appsettings.Production.json:**
   ```json
   {
     "SendGrid": {
       "ApiKey": "${SENDGRID_APIKEY}",
       "FromEmail": "noreply@yourdomain.com",
       "FromName": "EduConnect"
     }
   }
   ```

3. **Push to GitHub:**
   ```bash
   git add -A
   git commit -m "Add email notifications integration"
   git push origin main
   ```

4. **Render auto-deploys** - emails now live!

### Option 2: Deploy to Azure

1. Go to Azure Key Vault
2. Add secret: `SendGridApiKey`
3. Reference in appsettings:
   ```json
   {
     "SendGrid": {
       "ApiKey": "@Microsoft.KeyVault(SecretUri=...)",
       "FromEmail": "noreply@yourdomain.com"
     }
   }
   ```

---

## 🐛 Troubleshooting

### Issue: "SendGrid API key is not configured"

**Solution:** Add SendGrid settings to appsettings.json:
```json
"SendGrid": {
  "ApiKey": "YOUR_KEY",
  "FromEmail": "noreply@educonnect.com",
  "FromName": "EduConnect"
}
```

### Issue: "Email was not sent"

**Check:**
1. Is the API key valid?
2. Is the "From" email verified in SendGrid?
3. Check SendGrid Activity Feed for errors
4. Look at application logs for exceptions

### Issue: Emails going to spam

**Solutions:**
1. Setup SPF and DKIM records in SendGrid
2. Use a verified domain instead of sandbox
3. Add unsubscribe links (already included)
4. Include clear branding in emails

---

## 📊 Monitoring Email Delivery

### SendGrid Dashboard

1. Log in to SendGrid
2. Go to **Mail Activity**
3. View:
   - Delivered emails ✅
   - Bounced emails ❌
   - Opened emails 👁️
   - Clicked links 🔗
   - Spam reports 🚫

### Application Logs

Enable detailed logging:
```csharp
// In appsettings.json
"Logging": {
  "LogLevel": {
    "EduConnect.Services.EmailService": "Debug"
  }
}
```

---

## 🔐 Security Best Practices

1. **Never commit API keys**
   ```bash
   # .gitignore
   appsettings.Development.json
   appsettings.*.json  # except Production
   ```

2. **Use Environment Variables**
   ```csharp
   var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
   ```

3. **Validate Email Addresses**
   ```csharp
   if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
       throw new InvalidOperationException("Invalid email");
   ```

4. **Rate Limiting**
   - SendGrid free tier: 100 emails/day
   - Paid: Up to 100k emails/day
   - Monitor usage in dashboard

---

## 🎯 Next Steps

### Phase 2 Enhancements:
1. **Announcement Emails** - Send when faculty creates announcements
2. **Progress Reminders** - Weekly progress emails
3. **Certificate Emails** - Auto-send on course completion
4. **Assignment Deadlines** - Reminder emails 24h before
5. **Discussion Notifications** - New reply notifications

### Integration with Other Features:
1. Combine with **SignalR** for real-time + email
2. Add **SMS notifications** (Twilio)
3. Create **Email Templates** in SendGrid dashboard
4. Add **Unsubscribe** preferences for students

---

## 📞 Support

For SendGrid issues:
- **Documentation**: https://sendgrid.com/docs
- **Help Center**: https://support.sendgrid.com
- **API Reference**: https://docs.sendgrid.com/api-reference

For EduConnect:
- Check `Services/EmailService.cs` for implementation
- Check `appsettings.json` for configuration
- Review logs in `Logs/` directory

---

## ✅ Checklist

- [x] SendGrid account created
- [x] API key obtained
- [x] Email service implemented
- [x] StudentController integration
- [ ] Test email locally
- [ ] Add API key to appsettings
- [ ] Push to GitHub
- [ ] Deploy to Render
- [ ] Verify emails in production
- [ ] Monitor SendGrid dashboard

