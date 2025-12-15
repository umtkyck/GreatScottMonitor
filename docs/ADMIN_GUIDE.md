# CXA - Administrator Guide

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Configuration](#configuration)
4. [User Management](#user-management)
5. [Policy Configuration](#policy-configuration)
6. [Monitoring and Reporting](#monitoring-and-reporting)
7. [Troubleshooting](#troubleshooting)
8. [Security Best Practices](#security-best-practices)

## Overview

This guide is for system administrators responsible for deploying, configuring, and maintaining CXA in a healthcare environment.

### Key Responsibilities

- User enrollment and management
- Policy configuration
- Security monitoring
- Audit log review
- Incident response
- System maintenance

## Installation

### Prerequisites

- Windows 10/11 (64-bit) workstations
- Webcams (720p minimum, 1080p recommended)
- Network connectivity to backend API
- Admin privileges for installation

### Client Installation

1. **Download Installer**
   - Obtain MSI installer from deployment package
   - Verify installer signature

2. **Deploy to Workstations**
   - Use Group Policy or deployment tool (SCCM, Intune)
   - Or install manually on each workstation

3. **Initial Configuration**
   - Configure `appsettings.json` with backend API URL
   - Set face service pipe name (if custom)
   - Configure presence monitoring settings

4. **Verify Installation**
   - Launch application
   - Check system tray for green shield icon
   - Verify camera access permissions

### Backend Installation

See [DEPLOYMENT.md](DEPLOYMENT.md) for detailed backend installation instructions.

## Configuration

### Client Configuration

Edit `MedSecureVision.Client/appsettings.json`:

```json
{
  "FaceService": {
    "PipeName": "\\\\.\\pipe\\CXAFaceService"
  },
  "BackendApi": {
    "BaseUrl": "https://api.medsecurevision.com"
  },
  "PresenceMonitoring": {
    "CheckIntervalMs": 200,
    "AbsenceTimeoutSeconds": 5
  },
  "LockBehavior": {
    "Action": "LockScreen",
    "GracePeriodSeconds": 3
  }
}
```

### Backend Configuration

Edit `MedSecureVision.Backend/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db.example.com;Database=medsecure;..."
  },
  "Auth0": {
    "Domain": "your-domain.auth0.com",
    "Audience": "https://api.medsecurevision.com",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  },
  "Encryption": {
    "MasterKey": "USE-AZURE-KEY-VAULT"
  }
}
```

**Important**: Never commit `appsettings.json` with production secrets. Use Azure Key Vault or environment variables.

## User Management

### Admin Console Access

1. Navigate to admin console URL
2. Log in with Auth0 admin credentials
3. Access "User Management" from sidebar

### Adding Users

1. **Create User in Auth0**
   - Go to Auth0 Dashboard → Users
   - Create new user with email
   - Assign appropriate roles

2. **Add User in Admin Console**
   - Navigate to User Management
   - Click "Add User"
   - Enter user details:
     - Email (must match Auth0)
     - Name
     - Role (User, Admin, etc.)
   - Click "Save"

3. **User Enrollment**
   - User will be prompted to enroll on first login
   - Or admin can initiate enrollment from User Management

### Managing Users

**Suspend User**
- Find user in User Management
- Click "Suspend"
- User will be unable to authenticate
- Session will be locked if currently active

**Activate User**
- Find suspended user
- Click "Activate"
- User can authenticate again

**Force Re-enrollment**
- Find user
- Click "Re-enroll"
- User will be prompted to re-enroll on next login
- Previous templates are invalidated

**Delete User**
- **Warning**: This permanently deletes user data including face templates
- Use with caution
- Consider suspending instead for temporary access removal

### Bulk Operations

For bulk user management:
- Use Auth0 Management API
- Or export user list, modify, and import
- Contact support for bulk enrollment scripts

## Policy Configuration

### Accessing Policy Settings

1. Log in to Admin Console
2. Navigate to "Policy Configuration"
3. Modify settings as needed
4. Click "Save Changes"

### Available Policies

#### Presence Monitoring

**Absence Timeout (seconds)**
- Range: 3-30 seconds
- Default: 5 seconds
- Time before locking when no face detected
- **Recommendation**: 5-10 seconds for most environments

#### Authentication

**Retry Attempts Before Lockout**
- Range: 1-10 attempts
- Default: 3 attempts
- Number of failed authentication attempts before lockout
- **Recommendation**: 3-5 attempts

#### Fallback Authentication

**Enabled Methods**
- PIN Code: 6-8 digit PIN
- Windows Hello: Fingerprint or face (if supported)
- Smart Card: Hospital badge integration

**Recommendation**: Enable at least 2 fallback methods

#### Security

**Multiple Faces Policy**
- **Warn**: Show warning but don't lock
- **Lock Immediately**: Lock session when multiple faces detected
- **Recommendation**: "Lock Immediately" for high-security areas

#### Working Hours

**Start Time / End Time**
- Restrict authentication to specific hours
- Outside hours, users must use fallback methods
- **Recommendation**: Set to facility operating hours

### Policy Best Practices

1. **Start Conservative**: Begin with stricter policies, relax as needed
2. **Monitor Impact**: Review audit logs after policy changes
3. **User Communication**: Inform users of policy changes
4. **Test Changes**: Test policy changes in non-production first

## Monitoring and Reporting

### Dashboard

The admin dashboard shows:
- **Active Sessions**: Number of currently authenticated users
- **Success Rates**: Authentication success rates (24h, 7d, 30d)
- **Failed Attempts**: Timeline of failed authentication attempts
- **Workstation Status**: Map of workstation health

### Audit Logs

#### Viewing Audit Logs

1. Navigate to "Audit Logs" in Admin Console
2. Use filters:
   - Date range
   - Event type (authentication, enrollment, lock, etc.)
   - User ID
   - Workstation ID

#### Audit Log Events

**Event Types:**
- `authentication`: Face recognition attempts
- `enrollment`: Face enrollment events
- `lock`: Session lock events
- `unlock`: Session unlock events
- `admin_action`: Administrative actions
- `security_alert`: Security incidents

#### Exporting Audit Logs

1. Apply desired filters
2. Click "Export CSV"
3. File downloads with all filtered logs
4. Use for compliance reporting or analysis

#### Audit Log Retention

- **Minimum**: 6 years (HIPAA requirement)
- **Default**: 7 years
- **Configuration**: Set in backend configuration
- **Archival**: Old logs can be archived to cold storage

### Security Monitoring

#### Failed Authentication Patterns

Monitor for:
- Multiple failed attempts from same user
- Failed attempts from unknown workstations
- Unusual time patterns
- High failure rates

#### Incident Response

1. **Identify Incident**: Review audit logs
2. **Assess Severity**: Determine impact
3. **Contain**: Suspend affected accounts if needed
4. **Investigate**: Review detailed logs
5. **Remediate**: Fix issues, update policies
6. **Document**: Record incident in audit log

## Troubleshooting

### Common Issues

#### Users Cannot Authenticate

**Check:**
1. User account status (active/suspended)
2. User enrollment status
3. Camera functionality
4. Network connectivity to backend
5. Backend API status

**Solutions:**
- Verify user is enrolled
- Check camera permissions
- Test backend API connectivity
- Review audit logs for errors

#### High False Rejection Rate

**Possible Causes:**
- Poor lighting conditions
- Camera quality issues
- Users need re-enrollment
- Threshold too strict

**Solutions:**
- Improve workstation lighting
- Upgrade cameras if needed
- Lower similarity threshold (with caution)
- Re-enroll affected users

#### Performance Issues

**Check:**
1. CPU usage on workstations
2. Network latency to backend
3. Database performance
4. Python service status

**Solutions:**
- Optimize presence monitoring interval
- Scale backend infrastructure
- Optimize database queries
- Restart Python service if needed

### Diagnostic Tools

#### Client Diagnostics

- Check system tray icon status
- Review client logs: `%AppData%\MedSecureVision\logs`
- Use "View Diagnostics" from system tray menu

#### Backend Diagnostics

- Health check endpoint: `GET /api/health`
- Application Insights (if configured)
- Database query performance

#### Python Service Diagnostics

- Check service logs
- Verify named pipe is accessible
- Test camera access
- Check model loading

## Security Best Practices

### Access Control

1. **Principle of Least Privilege**
   - Grant admin access only to necessary personnel
   - Use role-based access control

2. **Multi-Factor Authentication**
   - Require MFA for admin console access
   - Use Auth0 MFA features

3. **Regular Access Reviews**
   - Review admin access quarterly
   - Remove access for departed employees immediately

### Data Protection

1. **Encryption**
   - Use Azure Key Vault or AWS Secrets Manager for master keys
   - Never store keys in code or configuration files
   - Rotate encryption keys annually

2. **Backup and Recovery**
   - Daily database backups
   - Test restore procedures quarterly
   - Store backups in separate geographic location

3. **Data Retention**
   - Follow HIPAA retention requirements (6 years minimum)
   - Securely delete data after retention period
   - Document deletion procedures

### Monitoring

1. **Continuous Monitoring**
   - Monitor authentication success rates
   - Alert on security incidents
   - Review audit logs weekly

2. **Incident Response**
   - Document incident response procedures
   - Train staff on procedures
   - Conduct tabletop exercises

3. **Compliance**
   - Regular HIPAA compliance audits
   - Document all security measures
   - Maintain audit trail

### Updates and Maintenance

1. **Regular Updates**
   - Apply security patches promptly
   - Test updates in non-production first
   - Schedule maintenance windows

2. **Vulnerability Management**
   - Regular security scans
   - Address vulnerabilities promptly
   - Document remediation

3. **Change Management**
   - Document all configuration changes
   - Test changes before production
   - Maintain rollback procedures

## Compliance

### HIPAA Requirements

CXA implements HIPAA technical safeguards:
- Access Control (§164.312(a)(1))
- Audit Controls (§164.312(b))
- Integrity (§164.312(c)(1))
- Person Authentication (§164.312(d))
- Transmission Security (§164.312(e)(1))
- Encryption (§164.312(a)(2)(iv))

See [HIPAA_COMPLIANCE.md](HIPAA_COMPLIANCE.md) for detailed compliance documentation.

### Audit Requirements

- Maintain audit logs for minimum 6 years
- Export logs for compliance reporting
- Document all administrative actions
- Regular compliance reviews

## Support and Resources

### Documentation

- [Architecture Documentation](ARCHITECTURE.md)
- [API Reference](API.md)
- [Deployment Guide](DEPLOYMENT.md)
- [HIPAA Compliance](HIPAA_COMPLIANCE.md)

### Support Contacts

- **Technical Support**: support@medsecurevision.com
- **Security Issues**: security@medsecurevision.com
- **Compliance Questions**: compliance@medsecurevision.com

### Training

- Admin training sessions available
- Video tutorials
- Documentation updates

---

*Last Updated: January 2025*






