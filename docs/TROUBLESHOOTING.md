# MedSecure Vision - Troubleshooting Guide

## Common Issues and Solutions

### Authentication Issues

#### Problem: "Face not recognized" repeatedly

**Symptoms:**
- Authentication fails even with correct positioning
- User must use fallback authentication frequently

**Possible Causes:**
1. Poor lighting conditions
2. Significant appearance change since enrollment
3. Camera quality issues
4. Threshold too strict

**Solutions:**
1. **Improve Lighting**
   - Ensure face is well-lit
   - Avoid backlighting
   - Use desk lamp if needed

2. **Re-enroll Face**
   - Right-click system tray → "Re-enroll Face"
   - Follow enrollment process
   - Capture in current appearance

3. **Check Camera**
   - Test camera in other applications
   - Clean camera lens
   - Check camera resolution settings

4. **Adjust Threshold** (Admin)
   - Lower similarity threshold slightly
   - Monitor false acceptance rate
   - Balance security vs. usability

#### Problem: "Multiple faces detected"

**Symptoms:**
- System detects multiple faces
- Session locks unexpectedly

**Possible Causes:**
1. Other people in camera view
2. Reflective surfaces (mirrors, windows)
3. Photos or screens in background

**Solutions:**
1. **Clear Background**
   - Ensure only user is in view
   - Remove photos from desk
   - Close applications showing faces

2. **Adjust Camera Position**
   - Angle camera to avoid reflections
   - Move away from windows/mirrors
   - Use privacy screen if needed

3. **Policy Adjustment** (Admin)
   - Change multi-face policy to "Warn" instead of "Lock"
   - Monitor for security implications

### Camera Issues

#### Problem: "No camera detected"

**Symptoms:**
- Application cannot find camera
- Camera feed is black

**Possible Causes:**
1. Camera not connected
2. Camera in use by another application
3. Windows permissions issue
4. Driver problem

**Solutions:**
1. **Check Connection**
   - Verify USB camera is plugged in
   - Try different USB port
   - Check for loose connections

2. **Close Other Applications**
   - Close video conferencing apps
   - Close other camera applications
   - Restart computer if needed

3. **Check Windows Permissions**
   - Settings → Privacy → Camera
   - Enable "Allow apps to access your camera"
   - Enable for MedSecure Vision specifically

4. **Update Drivers**
   - Device Manager → Cameras
   - Right-click camera → Update driver
   - Restart after update

#### Problem: Camera feed is slow or laggy

**Symptoms:**
- Frame rate is low
- Delayed response to movement
- High CPU usage

**Possible Causes:**
1. Insufficient CPU resources
2. Multiple applications using camera
3. Camera resolution too high

**Solutions:**
1. **Close Background Applications**
   - Task Manager → End unnecessary processes
   - Free up CPU resources

2. **Reduce Camera Resolution**
   - Camera settings → Lower resolution
   - 720p is sufficient for face recognition

3. **Optimize Settings** (Admin)
   - Increase presence monitoring interval
   - Enable frame skipping

### Performance Issues

#### Problem: High CPU usage

**Symptoms:**
- System becomes slow
- CPU usage > 20% constantly
- Battery drains quickly (laptops)

**Possible Causes:**
1. Presence monitoring too frequent
2. Face detection on every frame
3. Multiple services running

**Solutions:**
1. **Adjust Monitoring Interval** (Admin)
   - Increase check interval (e.g., 200ms → 500ms)
   - Reduces CPU usage significantly

2. **Enable Frame Skipping**
   - Skip every 2nd or 3rd frame
   - Still maintains good presence detection

3. **Optimize Python Service**
   - Use GPU acceleration if available
   - Reduce model complexity if needed

#### Problem: Slow authentication

**Symptoms:**
- Authentication takes > 1 second
- Delayed response to face detection

**Possible Causes:**
1. Network latency to backend
2. Slow database queries
3. Python service overloaded

**Solutions:**
1. **Check Network**
   - Ping backend API
   - Check network latency
   - Verify internet connection

2. **Database Optimization** (Admin)
   - Add indexes to face templates table
   - Optimize queries
   - Consider caching

3. **Service Scaling** (Admin)
   - Scale backend API
   - Add load balancer
   - Optimize database

### Enrollment Issues

#### Problem: Enrollment fails at quality check

**Symptoms:**
- Frames rejected for quality
- "Frame is too blurry" message
- Cannot complete enrollment

**Solutions:**
1. **Improve Lighting**
   - Use better lighting
   - Avoid shadows on face
   - Ensure even lighting

2. **Hold Still**
   - Keep head still during capture
   - Wait for "Capturing..." message
   - Don't move until capture completes

3. **Check Camera Focus**
   - Ensure camera is in focus
   - Clean camera lens
   - Adjust camera position

#### Problem: Liveness check fails

**Symptoms:**
- "Please blink" or "Move your head" message
   - Liveness detection doesn't recognize action

**Solutions:**
1. **Follow Instructions Clearly**
   - Blink naturally (not too fast)
   - Move head slowly and deliberately
   - Wait for confirmation

2. **Retry**
   - Try again if first attempt fails
   - Ensure good lighting
   - Face camera directly

### Network Issues

#### Problem: "Cannot connect to backend"

**Symptoms:**
- Authentication fails with network error
- Admin console cannot load

**Possible Causes:**
1. Backend API is down
2. Network connectivity issue
3. Firewall blocking connection

**Solutions:**
1. **Check Backend Status**
   - Verify backend is running
   - Check backend logs
   - Test API endpoint directly

2. **Check Network**
   - Ping backend server
   - Check DNS resolution
   - Verify VPN connection (if applicable)

3. **Check Firewall**
   - Allow application through firewall
   - Check corporate firewall rules
   - Verify port 443 (HTTPS) is open

### Database Issues

#### Problem: "Database connection failed"

**Symptoms:**
- Backend cannot connect to database
- API returns 500 errors

**Solutions:**
1. **Check Database Service**
   - Verify PostgreSQL/SQL Server is running
   - Check database logs
   - Restart database service

2. **Verify Connection String**
   - Check `appsettings.json`
   - Verify credentials
   - Test connection manually

3. **Check Network**
   - Verify database server is reachable
   - Check firewall rules
   - Verify port is open

## Diagnostic Steps

### Step 1: Check System Tray Icon

- **Green**: System is working
- **Yellow**: Warning condition
- **Red**: Error condition
- **Gray**: Not authenticated

### Step 2: Review Logs

**Client Logs:**
- Location: `%AppData%\MedSecureVision\logs`
- Check for error messages
- Look for patterns

**Backend Logs:**
- Check Application Insights
- Review server logs
- Look for exceptions

**Python Service Logs:**
- Check console output
- Review error messages
- Verify model loading

### Step 3: Test Components

1. **Test Camera**
   - Use Windows Camera app
   - Verify camera works
   - Check resolution

2. **Test Backend API**
   - Use Swagger UI
   - Test health endpoint
   - Verify authentication

3. **Test Python Service**
   - Check named pipe is created
   - Verify models loaded
   - Test face detection

### Step 4: Gather Information

When reporting issues, collect:
1. Error messages (screenshots)
2. System information (Windows version)
3. Camera model and driver version
4. Log files
5. Steps to reproduce

## Getting Help

### Support Channels

1. **IT Help Desk**: First point of contact
2. **Email Support**: support@medsecurevision.com
3. **GitHub Issues**: For developers
4. **Documentation**: Check user/admin guides

### Information to Provide

- Description of problem
- Steps to reproduce
- Error messages
- System information
- Log files (if available)
- Screenshots

---

*Last Updated: January 2025*






