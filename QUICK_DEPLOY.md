# 🚀 EduConnect - Deploy in 5 Commands

## Copy & Paste These Commands

### Step 1: Configure Git
```powershell
git config --global user.name "Your Name"
git config --global user.email "your-email@gmail.com"
```

### Step 2: Initialize Repository
```powershell
cd C:\Users\Administrator\Downloads\EduNet
git init
```

### Step 3: Commit Your Code
```powershell
git add .
git commit -m "EduConnect ready for deployment"
```

### Step 4: Add GitHub Remote
Replace `YOUR_USERNAME` with your GitHub username:
```powershell
git remote add origin https://github.com/YOUR_USERNAME/educonnect.git
git branch -M main
```

### Step 5: Push to GitHub
```powershell
git push -u origin main
```

---

## Done! Now:

1. Go to https://render.com (or Azure/Railway)
2. Sign in with GitHub
3. Create new service
4. Select your `educonnect` repository
5. Click Deploy
6. Wait 5 minutes
7. Get your public URL!

---

## Your App Will Be Live At:
```
https://educonnect-xxxx.onrender.com
(or similar for your chosen platform)
```

### First Action After Deployment:
1. Visit the URL
2. Login: admin@educonnect.com / Admin@123456
3. Change password immediately!
4. Test all features
5. Share URL with users

---

## If Anything Goes Wrong:

```powershell
# Check git status
git status

# See your commits
git log --oneline

# Push again
git push origin main
```

---

**Questions?** Open DEPLOY_ONLINE_FINAL.md for complete guide!

**Good luck! 🎉**
