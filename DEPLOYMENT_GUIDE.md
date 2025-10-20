# EduConnect Deployment Guide

## Deployment Options for Online Access

### Option 1: **Microsoft Azure (Recommended for ASP.NET Core)**

#### Prerequisites:
- Azure Account (create free account at azure.microsoft.com)
- Azure CLI installed
- Visual Studio or VS Code with Azure extension

#### Steps:

1. **Create Resource Group**
   ```bash
   az group create --name educonnect-rg --location eastus
   ```

2. **Create SQL Database** (optional, if moving from SQLite)
   ```bash
   az sql server create --resource-group educonnect-rg --name educonnect-server --admin-user adminuser --admin-password "YourPassword@123"
   az sql db create --resource-group educonnect-rg --server educonnect-server --name educonnect-db
   ```

3. **Create App Service Plan**
   ```bash
   az appservice plan create --name educonnect-plan --resource-group educonnect-rg --sku F1 --is-linux
   ```

4. **Create Web App**
   ```bash
   az webapp create --resource-group educonnect-rg --plan educonnect-plan --name educonnect-app --runtime "DOTNETCORE|9.0"
   ```

5. **Deploy Application**
   ```bash
   dotnet publish -c Release -o ./publish
   cd publish
   az webapp up --resource-group educonnect-rg --name educonnect-app --plan educonnect-plan
   ```

6. **Configure Connection String** (if using SQL Database)
   ```bash
   az webapp config connection-string set --resource-group educonnect-rg --name educonnect-app --settings DefaultConnection="Server=tcp:educonnect-server.database.windows.net,1433;Initial Catalog=educonnect-db;Persist Security Info=False;User ID=adminuser;Password=YourPassword@123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
   ```

**Estimated Cost:** Free tier available ($0-$10/month with paid tier)
**URL Format:** https://educonnect-app.azurewebsites.net

---

### Option 2: **Heroku (Easy Deployment)**

#### Prerequisites:
- Heroku Account (heroku.com)
- Heroku CLI installed
- Git installed

#### Steps:

1. **Create Heroku App**
   ```bash
   heroku login
   heroku create educonnect-app
   ```

2. **Add Procfile** (create file: `Procfile`)
   ```
   web: dotnet EduConnect.dll --urls http://+:$PORT
   ```

3. **Deploy**
   ```bash
   git push heroku main
   ```

**Estimated Cost:** $7-50/month (paid dyno)
**URL Format:** https://educonnect-app.herokuapp.com

---

### Option 3: **DigitalOcean App Platform (Affordable)**

#### Prerequisites:
- DigitalOcean Account (digitalocean.com)
- GitHub Repository (push your code there)

#### Steps:

1. **Connect GitHub Repository to DigitalOcean**
   - Log into DigitalOcean
   - Click "Apps" → "Create App"
   - Select your GitHub repository
   - Authorize DigitalOcean to access GitHub

2. **Configure Build Settings**
   - Build Command: `dotnet build`
   - Run Command: `dotnet EduConnect.dll`
   - Port: 8080

3. **Add Environment Variables** (if needed)
   - `ASPNETCORE_ENVIRONMENT=Production`
   - `ConnectionString=your-database-connection`

4. **Deploy**
   - Click "Deploy App"

**Estimated Cost:** $12-20/month
**URL Format:** https://educonnect-app-xxxxx.ondigitalocean.app

---

### Option 4: **AWS (Scalable, More Complex)**

#### Prerequisites:
- AWS Account
- AWS CLI configured

#### Services Needed:
- **EC2** - Virtual Server (~$10-30/month)
- **RDS** - Managed Database (~$15-30/month)
- **Route 53** - Domain Management
- **CloudFront** - CDN (optional)

#### Basic Steps:
1. Launch EC2 instance with Linux
2. Install .NET 9 runtime
3. Deploy application to instance
4. Configure security groups for HTTP/HTTPS
5. Set up RDS database
6. Configure domain with Route 53

**Estimated Cost:** $25-100+/month
**URL Format:** https://educonnect.yourdomain.com

---

## Recommended: Azure Deployment (Step-by-Step)

### Prerequisites:
1. Azure Account with subscription
2. Azure CLI or Azure Portal access
3. Application built and tested locally

### Detailed Steps:

#### Step 1: Prepare for Production

Update `appsettings.json` for production:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=educonnect-server.database.windows.net;Database=educonnect-db;User Id=adminuser;Password=YourPassword@123;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

#### Step 2: Create appsettings.Production.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "#{DatabaseConnectionString}#"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Error",
      "Microsoft": "Error"
    }
  }
}
```

#### Step 3: Publish Application

```bash
dotnet publish -c Release --output ./publish
```

#### Step 4: Deploy to Azure

Using Azure Portal:
1. Create App Service
2. Configure Connection Strings
3. Enable HTTPS
4. Deploy from local ZIP or GitHub

Using Azure CLI:
```bash
az webapp deployment source config-zip --resource-group educonnect-rg --name educonnect-app --src publish.zip
```

#### Step 5: Configure Database

Option A: Use SQL Server
- Create Azure SQL Database
- Update connection string in App Service settings

Option B: Keep SQLite
- Deploy SQLite database file with application
- Ensure write permissions on App Service

---

## Important Configuration Changes for Production

### 1. Update Program.cs for Production

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
```

### 2. Enable HTTPS

In Azure Portal:
- App Service → TLS/SSL settings
- Enable HTTPS Only

### 3. Security Headers

Add to Program.cs:
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    await next();
});
```

### 4. Database Migrations

Run migrations before deployment:
```bash
dotnet ef database update
```

### 5. Default Admin Account

Change credentials in Program.cs or database after deployment.

---

## Domain Name & DNS

### Get a Domain:
- GoDaddy, Namecheap, Google Domains
- Cost: $1-15/year

### Configure DNS:
Point your domain to your hosting provider's nameservers or use their DNS records.

---

## Monitoring & Maintenance

### Azure Monitor:
- Application Insights for monitoring
- Log Analytics for troubleshooting
- Performance metrics

### Backup Strategy:
- Automated database backups
- Application code repository (GitHub)

### Regular Updates:
- Update NuGet packages monthly
- Apply security patches
- Monitor logs for errors

---

## SSL Certificate (HTTPS)

- **Azure**: Free SSL included with App Service
- **Let's Encrypt**: Free SSL with most hosting providers
- **CloudFlare**: Free SSL proxy (DNS routing)

---

## Cost Summary

| Platform | Cost/Month | Complexity | Performance |
|----------|-----------|-----------|-------------|
| Azure | $10-50 | Medium | Excellent |
| Heroku | $7-50 | Low | Good |
| DigitalOcean | $12-20 | Medium | Good |
| AWS | $25-100+ | High | Excellent |
| Local Server | $0 (hardware) | High | Depends |

---

## Next Steps

1. **Choose a platform** based on your needs and budget
2. **Create an account** on your chosen platform
3. **Follow the deployment steps** above
4. **Test the application** online
5. **Share the URL** with users
6. **Monitor performance** and user feedback
7. **Scale if needed** as user base grows

---

## Quick Start: Azure Deployment

```bash
# 1. Login to Azure
az login

# 2. Create resource group
az group create --name educonnect-rg --location eastus

# 3. Create app service plan (Free tier)
az appservice plan create --name educonnect-plan --resource-group educonnect-rg --sku F1 --is-linux

# 4. Create web app
az webapp create --resource-group educonnect-rg --plan educonnect-plan --name educonnect-app --runtime "DOTNET|9.0"

# 5. Publish application
dotnet publish -c Release -o ./publish

# 6. Deploy
az webapp up --resource-group educonnect-rg --name educonnect-app --plan educonnect-plan

# Done! Access at: https://educonnect-app.azurewebsites.net
```

---

## Support & Troubleshooting

- Check application logs: `az webapp log tail --resource-group educonnect-rg --name educonnect-app`
- Verify connection strings in Application Settings
- Test locally before deploying
- Monitor uptime and performance metrics

**Questions?** Contact your hosting provider support!
