# GitHub Setup - Step by Step

## Step 1: Create GitHub Account
1. Go to https://github.com
2. Click "Sign up"
3. Enter username, email, password
4. Verify email
5. Done!

## Step 2: Create New Repository
1. Click "+" → "New repository"
2. Repository name: `educonnect`
3. Description: "EduConnect Learning Portal"
4. **Private** or **Public** (your choice)
5. Click "Create repository"
6. Copy the HTTPS URL (you'll need it)

## Step 3: Install Git
- **Windows**: Download from https://git-scm.com/download/win
- **Mac**: `brew install git`
- **Linux**: `sudo apt-get install git`

## Step 4: Configure Git

Open PowerShell/Terminal and run:
```bash
git config --global user.name "Your Name"
git config --global user.email "your-email@gmail.com"
```

## Step 5: Push Your Code to GitHub

Navigate to your project folder:
```bash
cd C:\Users\Administrator\Downloads\EduNet
```

Initialize Git:
```bash
git init
```

Add all files:
```bash
git add .
```

Create first commit:
```bash
git commit -m "Initial commit - EduConnect application"
```

Add GitHub remote (replace with YOUR URL):
```bash
git remote add origin https://github.com/YOUR_USERNAME/educonnect.git
```

Rename main branch:
```bash
git branch -M main
```

Push to GitHub:
```bash
git push -u origin main
```

Enter your GitHub credentials when prompted.

## Step 6: Verify on GitHub
1. Go to https://github.com/YOUR_USERNAME/educonnect
2. You should see all your files there
3. Done! Code is backed up

---

# Deploy to Render (Easiest)

## Step 1: Create Render Account
1. Go to https://render.com
2. Click "Sign up"
3. Sign up with GitHub (easiest method)
4. Authorize Render to access GitHub

## Step 2: Create New Web Service
1. Click "New" button
2. Select "Web Service"
3. Select your "educonnect" repository
4. Click "Connect"

## Step 3: Configure Service
Fill in the form:

| Field | Value |
|-------|-------|
| Name | educonnect |
| Environment | .NET |
| Build Command | `dotnet build` |
| Start Command | `dotnet EduConnect.dll` |
| Plan | Free |

## Step 4: Click "Create Web Service"
- Render will start building
- Takes 2-5 minutes
- You'll see logs in dashboard
- When done, you get a public URL

## Step 5: Access Your App
- Render gives you URL like: `https://educonnect-xxxx.onrender.com`
- Share this with users!
- First load might be slow (free tier)

---

# Deploy to Azure (More Professional)

## Prerequisites
1. Azure Account (free tier): https://azure.microsoft.com/free
2. Create account and login

## Step 1: Create Resource Group
1. Click "Resource Groups"
2. Click "Create"
3. Name: `educonnect-rg`
4. Region: Choose closest to you
5. Click "Create"

## Step 2: Create App Service
1. Click "Create a resource"
2. Search "App Service"
3. Click "Create"
4. Fill in:
   - **Name**: `educonnect-app` (must be unique)
   - **Publish**: Code
   - **Runtime**: .NET 9
   - **Operating System**: Linux
   - **Plan**: Free tier (F1)
5. Click "Create"

## Step 3: Deploy from GitHub
1. Go to your App Service
2. Click "Deployment Center"
3. Select "GitHub"
4. Click "Authorize"
5. Select your `educonnect` repository
6. Select `main` branch
7. Click "Save"
8. Azure deploys automatically!

## Step 4: Get URL
- Go to App Service overview
- Find "Default domain"
- URL is: `https://educonnect-app.azurewebsites.net`

---

# After Deployment

## Important: Change Default Admin Password

1. Login with: admin@educonnect.com / Admin@123456
2. Click your profile
3. Change password to something secure
4. Share new credentials only with admins

## Share Application with Users

Tell users:
```
Go to: https://your-app-url.com
Click "Register"
Create account
Start using!
```

## Monitor Your Application

**Render Dashboard**: See logs and status
**Azure Portal**: See metrics and logs
**Check regularly**: Make sure it's running

## Scale if Needed

- More users? Upgrade plan
- Need database? Add managed database
- Need custom domain? Configure DNS

---

# Troubleshooting

## Application won't start
- Check logs in hosting dashboard
- Verify connection string
- Check for build errors

## Can't access URL
- Wait 5 minutes for DNS to propagate
- Refresh page (Ctrl+Shift+R)
- Check if service is running

## Database issues
- SQLite database deploys with app
- Make sure file permissions are correct
- Or use managed database service

## Performance is slow
- Free tier has limited resources
- Upgrade to paid plan for better performance
- Use CDN for static files

---

# Support

**For Render**: https://render.com/docs
**For Azure**: https://docs.microsoft.com/azure
**For Git/GitHub**: https://github.com/git-tips
**General Help**: Contact platform support

---

**Next Steps:**
1. Create GitHub account
2. Push code to GitHub
3. Create hosting account (Render/Azure)
4. Deploy!
5. Share URL with users
6. Monitor and maintain

Questions? I'm here to help! 🚀
