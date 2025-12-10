# MedSecure Vision - Git Commit Script
# Run this script after Git is installed and configured
# Usage: .\COMMIT_SCRIPT.ps1

# Initialize repository if not already initialized
if (-not (Test-Path .git)) {
    Write-Host "Initializing Git repository..."
    git init
    git config user.name "MedSecure Vision"
    git config user.email "dev@medsecurevision.com"
}

# Commit 1: Project Structure
Write-Host "Committing project structure..."
git add MedSecureVision.sln MedSecureVision.Client/MedSecureVision.Client.csproj MedSecureVision.Backend/MedSecureVision.Backend.csproj MedSecureVision.Shared/MedSecureVision.Shared.csproj MedSecureVision.Tests/MedSecureVision.Tests.csproj .gitignore README.md docker-compose.yml .github/workflows/ci.yml
git commit -m "feat: Initialize project structure and solution files

- Add solution file with all projects (Client, Backend, Shared, Tests)
- Configure .NET 8 projects with required NuGet packages
- Add CI/CD pipeline configuration
- Add Docker Compose for PostgreSQL
- Add .gitignore and README"

# Commit 2: Shared Models
Write-Host "Committing shared models..."
git add MedSecureVision.Shared/
git commit -m "feat: Add shared models and contracts

- Add FaceDetectionResult, FaceEmbedding, IpcMessage models
- Add AuthenticationModels for request/response
- Add PresenceState enum and models
- Define IPC command constants"

# Commit 3: Python Face Service
Write-Host "Committing Python face service..."
git add MedSecureVision.FaceService/
git commit -m "feat: Implement Python face service with MediaPipe and InsightFace

- Add MediaPipe BlazeFace face detection wrapper
- Add InsightFace ArcFace recognition with 512-dim embeddings
- Implement named pipe IPC server for C# communication
- Add liveness detection (blink, head movement)
- Add quality validation (blur, lighting, coverage)
- Add performance optimization utilities
- Include comprehensive docstrings and comments"

# Commit 4: WPF Client
Write-Host "Committing WPF client..."
git add MedSecureVision.Client/
git commit -m "feat: Implement WPF client with authentication and presence monitoring

- Add WPF UI with face guide overlay and animations
- Implement face service client with named pipe communication
- Add camera service for webcam capture
- Add authentication service with cloud verification
- Add presence monitoring service (200ms intervals)
- Add session lock service (Windows integration)
- Add fallback authentication (PIN, Windows Hello)
- Add enrollment workflow UI
- Add comprehensive XML documentation comments"

# Commit 5: Backend API
Write-Host "Committing backend API..."
git add MedSecureVision.Backend/
git commit -m "feat: Implement ASP.NET Core backend API with Auth0 integration

- Add ASP.NET Core 8 Web API with Auth0 JWT authentication
- Implement Entity Framework Core with PostgreSQL support
- Add face verification service with cosine similarity matching
- Add AES-256-GCM encryption service for face templates
- Add HIPAA-compliant audit logging service
- Add controllers: Auth, Authentication, Enrollment, AuditLog
- Add comprehensive XML documentation comments"

# Commit 6: Admin Console
Write-Host "Committing admin console..."
git add MedSecureVision.AdminConsole/
git commit -m "feat: Implement React admin console

- Add React TypeScript application with Auth0 authentication
- Add dashboard with authentication metrics and charts
- Add user management (CRUD operations)
- Add policy configuration interface
- Add audit log viewer with filtering and CSV export
- Add responsive layout with sidebar navigation"

# Commit 7: Tests
Write-Host "Committing tests..."
git add MedSecureVision.Tests/
git commit -m "test: Add comprehensive unit test suite

- Add tests for FaceServiceClient (named pipe communication)
- Add tests for PresenceMonitorService
- Add tests for FaceVerificationService
- Add tests for EncryptionService (round-trip, edge cases)
- Add tests for AuditLogService
- Add integration tests for workflows
- Use xUnit, FluentAssertions, and Moq"

# Commit 8: Documentation
Write-Host "Committing documentation..."
git add docs/
git commit -m "docs: Add comprehensive documentation and help guides

- Add USER_GUIDE.md (end user documentation)
- Add ADMIN_GUIDE.md (administrator documentation)
- Add DEVELOPER_GUIDE.md (developer documentation)
- Add QUICK_REFERENCE.md (quick lookup guide)
- Add TROUBLESHOOTING.md (problem solving guide)
- Add ARCHITECTURE.md (system architecture)
- Add DEPLOYMENT.md (deployment instructions)
- Add HIPAA_COMPLIANCE.md (compliance documentation)
- Add API.md (API reference)"

Write-Host "`nAll commits completed successfully!"
Write-Host "`nCommit history:"
git log --oneline

