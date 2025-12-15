# MedSecure Vision - Deployment Guide

## Prerequisites

- Windows 10/11 (64-bit)
- .NET 8 SDK
- Python 3.10 or higher
- PostgreSQL 16+ (or Azure SQL)
- Auth0 account
- Webcam (720p minimum, 1080p recommended)

## Backend Deployment

### 1. Database Setup

#### Option A: PostgreSQL (Docker)

```bash
docker-compose up -d postgres
```

#### Option B: Azure SQL

1. Create Azure SQL Database
2. Update connection string in `appsettings.json`

### 2. Configure Backend

Edit `MedSecureVision.Backend/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=medsecure;Username=medsecure_user;Password=medsecure_password"
  },
  "Auth0": {
    "Domain": "your-auth0-domain.auth0.com",
    "Audience": "https://api.medsecurevision.com",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  },
  "Encryption": {
    "MasterKey": "CHANGE-THIS-USE-AZURE-KEY-VAULT"
  }
}
```

### 3. Run Database Migrations

```bash
cd MedSecureVision.Backend
dotnet ef database update
```

### 4. Start Backend

```bash
dotnet run
```

Backend will be available at `https://localhost:5001`

## Python Face Service Deployment

### 1. Install Dependencies

```bash
cd MedSecureVision.FaceService
pip install -r requirements.txt
```

**Note:** InsightFace will download models automatically on first run (buffalo_l model, ~500MB).

### 2. Start Service

```bash
python main.py
```

The service will create a named pipe at `\\.\pipe\MedSecureFaceService`

## Client Deployment

### 1. Configure Client

Edit `MedSecureVision.Client/appsettings.json`:

```json
{
  "FaceService": {
    "PipeName": "\\\\.\\pipe\\MedSecureFaceService"
  },
  "BackendApi": {
    "BaseUrl": "https://localhost:5001"
  },
  "PresenceMonitoring": {
    "CheckIntervalMs": 200,
    "AbsenceTimeoutSeconds": 5
  }
}
```

### 2. Build Client

```bash
cd MedSecureVision.Client
dotnet build -c Release
```

### 3. Run Client

```bash
dotnet run
```

Or create an installer (MSI) for distribution.

## Admin Console Deployment

### 1. Configure Environment

Create `.env` file:

```
REACT_APP_AUTH0_DOMAIN=your-auth0-domain.auth0.com
REACT_APP_AUTH0_CLIENT_ID=your-client-id
REACT_APP_AUTH0_AUDIENCE=https://api.medsecurevision.com
REACT_APP_API_URL=https://localhost:5001
```

### 2. Install Dependencies

```bash
cd MedSecureVision.AdminConsole
npm install
```

### 3. Build

```bash
npm run build
```

### 4. Deploy

Deploy the `build` folder to a web server (IIS, Nginx, Azure Static Web Apps, etc.)

## Production Deployment

### Azure Deployment

1. **Backend API:**
   - Deploy to Azure App Service
   - Configure Application Insights
   - Set up Azure Key Vault for secrets
   - Enable HTTPS only

2. **Database:**
   - Use Azure SQL Database
   - Enable encryption at rest
   - Configure backup retention

3. **Admin Console:**
   - Deploy to Azure Static Web Apps
   - Configure custom domain
   - Enable HTTPS

### AWS Deployment

1. **Backend API:**
   - Deploy to ECS Fargate or EC2
   - Use Application Load Balancer
   - Configure CloudWatch logging

2. **Database:**
   - Use RDS PostgreSQL
   - Enable encryption
   - Configure automated backups

3. **Secrets:**
   - Use AWS Secrets Manager
   - Rotate keys regularly

## Security Checklist

- [ ] Change default encryption master key
- [ ] Use Azure Key Vault or AWS Secrets Manager
- [ ] Enable HTTPS only
- [ ] Configure CORS properly
- [ ] Set up firewall rules
- [ ] Enable database encryption
- [ ] Configure audit log retention
- [ ] Set up monitoring and alerts
- [ ] Configure backup and disaster recovery
- [ ] Document incident response procedures

## Troubleshooting

### Python Service Not Starting

- Check if named pipe is already in use
- Verify Python dependencies are installed
- Check camera permissions

### Backend API Errors

- Verify database connection string
- Check Auth0 configuration
- Review application logs

### Client Authentication Fails

- Verify backend API is accessible
- Check Auth0 token validity
- Review face service connection

## Monitoring

### Application Insights (Azure)

- Enable in `Program.cs`
- Monitor authentication success rates
- Track error rates and latency

### CloudWatch (AWS)

- Configure CloudWatch Logs
- Set up alarms for errors
- Monitor API Gateway metrics






