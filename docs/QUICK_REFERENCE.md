# MedSecure Vision - Quick Reference Guide

## For End Users

### Common Tasks

**Enroll Your Face**
1. Right-click system tray icon → "Re-enroll Face"
2. Follow on-screen instructions
3. Capture 8 frames from different angles

**Unlock Your Computer**
- Sit in front of camera
- Face will be detected automatically
- Wait for green checkmark

**Use Fallback Authentication**
- Click "Use PIN" or "Use Windows Hello"
- Enter credentials
- Session unlocks

**Manually Lock Session**
- Press `Ctrl+Alt+L`
- Or right-click icon → "Lock Now"

### System Tray Icons

| Icon | Status | Meaning |
|------|--------|---------|
| 🟢 Green Shield | Active | Monitoring and working |
| 🟡 Yellow Shield | Warning | Camera issue or degraded |
| 🔴 Red Shield | Error | Disabled or error |
| ⚪ Gray Shield | Inactive | Not authenticated |

## For Administrators

### Common Tasks

**Add New User**
1. Admin Console → User Management
2. Click "Add User"
3. Enter user details
4. User enrolls on first login

**Suspend User**
1. User Management → Find user
2. Click "Suspend"
3. User cannot authenticate

**View Audit Logs**
1. Admin Console → Audit Logs
2. Apply filters (date, user, event type)
3. Click "Export CSV" if needed

**Change Policy**
1. Admin Console → Policy Configuration
2. Modify settings
3. Click "Save Changes"

### Policy Settings Quick Reference

| Setting | Default | Range | Description |
|---------|---------|-------|-------------|
| Absence Timeout | 5 seconds | 3-30s | Time before lock when no face |
| Retry Attempts | 3 | 1-10 | Failed attempts before lockout |
| Multi-Face Policy | Lock | Warn/Lock | Action when multiple faces detected |

## For Developers

### Common Commands

**Run Backend**
```bash
cd MedSecureVision.Backend
dotnet run
```

**Run Python Service**
```bash
cd MedSecureVision.FaceService
python main.py
```

**Run Tests**
```bash
dotnet test
```

**Build Solution**
```bash
dotnet build
```

### Key File Locations

| Component | Location |
|-----------|----------|
| Client Config | `MedSecureVision.Client/appsettings.json` |
| Backend Config | `MedSecureVision.Backend/appsettings.json` |
| Python Requirements | `MedSecureVision.FaceService/requirements.txt` |
| Database Migrations | `MedSecureVision.Backend/Migrations/` |

### API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/auth/verify` | POST | Verify face embedding |
| `/api/enrollment/upload-template` | POST | Upload face template |
| `/api/audit-logs` | GET | Get audit logs |
| `/api/users` | GET | List users |

## Troubleshooting Quick Fixes

### "No camera detected"
- Check camera connection
- Restart application
- Check Windows camera permissions

### "Face not recognized"
- Improve lighting
- Remove face mask
- Try re-enrolling
- Use fallback authentication

### "Session locked unexpectedly"
- Check if you moved out of view
- Verify camera not blocked
- Check system tray icon status

### High CPU usage
- Close unnecessary applications
- Reduce presence monitoring frequency
- Check for background processes

## Support Contacts

- **User Support**: support@medsecurevision.com
- **Admin Support**: admin@medsecurevision.com
- **Developer Support**: dev@medsecurevision.com

---

*Last Updated: January 2025*

