# MedSecure Vision - User Guide

## Table of Contents

1. [Introduction](#introduction)
2. [Getting Started](#getting-started)
3. [Face Enrollment](#face-enrollment)
4. [Authentication](#authentication)
5. [Troubleshooting](#troubleshooting)
6. [FAQ](#faq)

## Introduction

MedSecure Vision is a biometric authentication system that uses facial recognition to secure your workstation. It provides:

- **Fast Authentication**: Unlock your computer in under 500ms using your face
- **Automatic Locking**: Your session locks automatically when you leave your desk
- **HIPAA Compliant**: Secure and compliant for healthcare environments
- **Fallback Options**: PIN, Windows Hello, or Smart Card if face recognition fails

## Getting Started

### First-Time Setup

1. **Launch the Application**
   - The MedSecure Vision client should start automatically when you log in
   - Look for the green shield icon in your system tray

2. **Initial Authentication**
   - On first launch, you'll need to authenticate using your password or PIN
   - After authentication, you'll be prompted to enroll your face

3. **Camera Setup**
   - Ensure your webcam is connected and working
   - The application will automatically detect and use your camera
   - A privacy indicator will show when the camera is active

## Face Enrollment

### Enrollment Process

Face enrollment captures multiple angles of your face to create a secure template. Follow these steps:

1. **Start Enrollment**
   - Click "Enroll Face" from the system tray menu, or
   - You'll be prompted during first-time setup

2. **Positioning**
   - Sit directly in front of your camera
   - Ensure good lighting (avoid backlighting)
   - Remove glasses if possible (or keep them on if you always wear them)
   - Remove face masks

3. **Capture Sequence**
   The system will guide you through capturing 8 frames:
   - **Front face** (3 frames) - Look directly at the camera
   - **Slight left** (2 frames) - Turn your head 15° to the left
   - **Slight right** (2 frames) - Turn your head 15° to the right
   - **Slight up** (1 frame) - Tilt your head up 10°
   - **Slight down** (1 frame) - Tilt your head down 10°

4. **Quality Checks**
   - The system will validate each frame for:
     - **Blur**: Frame must be sharp and clear
     - **Lighting**: Adequate and even lighting
     - **Position**: Face properly centered and sized
     - **Eyes**: Both eyes must be visible
   - If a frame fails, you'll be asked to retake it

5. **Liveness Check**
   - You may be asked to blink or move your head slightly
   - This prevents spoofing with photos or videos

6. **Completion**
   - Once all frames are captured and validated, enrollment is complete
   - You'll see a success message
   - Your face template is encrypted and stored securely

### Tips for Best Results

- **Lighting**: Use natural or overhead lighting. Avoid strong backlighting
- **Distance**: Sit 2-3 feet from the camera
- **Background**: Use a plain background if possible
- **Consistency**: Enroll with the same appearance you'll use daily (glasses, makeup, etc.)
- **Stability**: Keep your head still during capture

## Authentication

### Using Face Recognition

1. **Automatic Detection**
   - When you return to your workstation, the system automatically detects your face
   - You'll see an oval guide on the screen
   - Position your face within the guide

2. **Authentication States**
   - **Searching**: System is looking for a face
   - **Positioning**: Face detected, adjust position if needed
   - **Verifying**: Face recognized, verifying identity
   - **Success**: Authentication complete, desktop unlocked
   - **Failure**: Face not recognized, try again or use fallback

3. **Success Indicators**
   - Green checkmark animation
   - "Welcome, [Your Name]" message
   - Automatic transition to desktop

### Fallback Authentication

If face recognition fails, you can use alternative methods:

1. **PIN Code**
   - Enter your 6-8 digit PIN
   - After 5 failed attempts, you'll be locked out for 30 minutes
   - Contact your administrator to reset your PIN

2. **Windows Hello**
   - If your device supports Windows Hello (fingerprint or face), you can use it
   - Click "Use Windows Hello" on the fallback screen

3. **Smart Card/Badge**
   - Insert your hospital badge or smart card
   - The system will read and verify it

## Presence Monitoring

### How It Works

Once authenticated, MedSecure Vision continuously monitors your presence:

- **Check Interval**: Every 200ms (5 times per second)
- **Absence Timeout**: Default 5 seconds (configurable by administrator)
- **Automatic Lock**: Session locks if:
  - No face detected for the timeout period
  - Different face detected
  - Multiple faces detected
  - Camera disconnected or blocked

### What You'll See

- **Green Shield Icon**: Active and monitoring
- **Yellow Shield Icon**: Camera issue or degraded service
- **Red Shield Icon**: Disabled or error
- **Gray Shield Icon**: Not authenticated

### Manual Lock

You can manually lock your session:

- Press `Ctrl+Alt+L` (hotkey)
- Right-click system tray icon → "Lock Now"
- The system will lock immediately

## Troubleshooting

### Camera Issues

**Problem**: "No camera detected"
- **Solution**: 
  - Check camera connection
  - Ensure camera is not being used by another application
  - Restart the application

**Problem**: "Camera feed is black"
- **Solution**:
  - Check camera privacy settings in Windows
  - Ensure camera permissions are granted
  - Try unplugging and reconnecting the camera

### Authentication Issues

**Problem**: "Face not recognized"
- **Solutions**:
  - Ensure good lighting
  - Remove face mask
  - Look directly at the camera
  - Try re-enrolling if your appearance has changed significantly
  - Use fallback authentication (PIN)

**Problem**: "Multiple faces detected"
- **Solution**:
  - Ensure only you are in front of the camera
  - Move away from reflective surfaces (mirrors, windows)
  - Close background applications that might show faces

**Problem**: "Session locked unexpectedly"
- **Solutions**:
  - Check if you moved out of camera view
  - Verify camera is not blocked
  - Check system tray icon for error indicators
  - Contact administrator if issue persists

### Performance Issues

**Problem**: Slow authentication
- **Solutions**:
  - Close unnecessary applications
  - Check CPU usage
  - Ensure camera is working at full resolution
  - Restart the application

## FAQ

### General Questions

**Q: Is my face data stored securely?**
A: Yes. Face templates are encrypted using AES-256-GCM and stored separately from your personal information. The template cannot be used to recreate your face image.

**Q: Can I opt out of face recognition?**
A: Yes. You can use PIN, Windows Hello, or Smart Card authentication instead. Contact your administrator to disable face recognition for your account.

**Q: What happens if I change my appearance?**
A: Minor changes (haircut, glasses) usually don't require re-enrollment. Significant changes (beard, major weight change) may require re-enrollment. You can re-enroll from the system tray menu.

**Q: Does it work in the dark?**
A: The system works best with adequate lighting. Very low light may reduce accuracy. Consider using a desk lamp if your workspace is dim.

**Q: Can someone use a photo of me to unlock?**
A: No. The system includes liveness detection that prevents photo and video spoofing. It requires a live person present.

### Privacy Questions

**Q: Is the camera always recording?**
A: No. The camera is only active when:
- You're authenticating
- You're enrolled
- Presence monitoring is active (after authentication)

**Q: Are video recordings stored?**
A: No. Frames are processed in memory only and never saved to disk. Only encrypted face templates are stored.

**Q: Who can see my face data?**
A: Only the encrypted template is stored. Administrators can see enrollment status but cannot view your face or recreate your image from the template.

### Technical Questions

**Q: What are the system requirements?**
A: 
- Windows 10/11 (64-bit)
- Webcam (720p minimum, 1080p recommended)
- 8GB RAM minimum
- Internet connection for cloud authentication

**Q: Does it work offline?**
A: Limited offline functionality is available with cached credentials. Full features require internet connectivity for cloud authentication.

**Q: How accurate is it?**
A: The system achieves 99.83% accuracy on standard benchmarks. False acceptance rate is less than 0.001% (1 in 100,000).

## Getting Help

### Support Contacts

- **IT Help Desk**: [Your IT Support Contact]
- **Email**: support@medsecurevision.com
- **Phone**: [Support Phone Number]

### Reporting Issues

When reporting issues, please provide:
1. Description of the problem
2. Steps to reproduce
3. Screenshots if applicable
4. System information (Windows version, camera model)
5. Error messages (if any)

## Glossary

- **Enrollment**: The process of capturing your face to create a biometric template
- **Embedding**: A mathematical representation of facial features (512 numbers)
- **Template**: Encrypted face data stored for authentication
- **Liveness Detection**: Technology that ensures a live person is present (not a photo/video)
- **Presence Monitoring**: Continuous checking to ensure the authenticated user is still present
- **Fallback Authentication**: Alternative authentication methods when face recognition fails

---

*Last Updated: January 2025*






