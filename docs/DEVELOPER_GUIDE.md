# MedSecure Vision - Developer Guide

## Table of Contents

1. [Getting Started](#getting-started)
2. [Architecture Overview](#architecture-overview)
3. [Development Setup](#development-setup)
4. [Code Structure](#code-structure)
5. [Testing](#testing)
6. [Contributing](#contributing)

## Getting Started

### Prerequisites

- **.NET 8 SDK**: [Download](https://dotnet.microsoft.com/download)
- **Python 3.10+**: [Download](https://www.python.org/downloads/)
- **Visual Studio 2022** or **VS Code**
- **PostgreSQL 16+** or **SQL Server**
- **Git**: [Download](https://git-scm.com/downloads)

### Clone Repository

```bash
git clone https://github.com/your-org/MedSecureVision.git
cd MedSecureVision
```

## Architecture Overview

### System Components

```
┌─────────────────┐
│  WPF Client     │ ← C# .NET 8
│  (Windows)      │
└────────┬────────┘
         │ Named Pipes (IPC)
         │
┌────────▼────────┐
│ Python Service │ ← Python 3.10+
│ (Face AI)      │
└────────┬────────┘
         │ Camera
         │
    ┌────▼────┐
    │ Webcam  │
    └─────────┘

┌─────────────────┐
│  WPF Client     │
└────────┬────────┘
         │ HTTPS/REST
         │
┌────────▼────────┐
│ Backend API     │ ← ASP.NET Core 8
│ (Cloud)         │
└────────┬────────┘
         │
    ┌────▼────┐
    │Database │
    │(PostgreSQL)│
    └─────────┘
```

### Technology Stack

| Component | Technology |
|-----------|------------|
| Client | C# .NET 8, WPF, MVVM |
| Face Service | Python 3.10+, MediaPipe, InsightFace |
| Backend | ASP.NET Core 8, Entity Framework Core |
| Database | PostgreSQL 16+ |
| Admin Console | React, TypeScript |
| Authentication | Auth0 OAuth 2.0 |

## Development Setup

### 1. Backend Setup

```bash
cd MedSecureVision.Backend

# Install dependencies
dotnet restore

# Configure database connection in appsettings.json
# Run migrations
dotnet ef database update

# Start backend
dotnet run
```

Backend will be available at `https://localhost:5001`

### 2. Python Face Service Setup

```bash
cd MedSecureVision.FaceService

# Create virtual environment
python -m venv venv
venv\Scripts\activate  # Windows
# or
source venv/bin/activate  # Linux/Mac

# Install dependencies
pip install -r requirements.txt

# Start service
python main.py
```

### 3. Client Setup

```bash
cd MedSecureVision.Client

# Restore packages
dotnet restore

# Build
dotnet build

# Run
dotnet run
```

### 4. Admin Console Setup

```bash
cd MedSecureVision.AdminConsole

# Install dependencies
npm install

# Configure .env file
cp .env.example .env
# Edit .env with your Auth0 credentials

# Start development server
npm start
```

## Code Structure

### Client (WPF)

```
MedSecureVision.Client/
├── Services/          # Business logic services
│   ├── FaceServiceClient.cs      # Python service communication
│   ├── CameraService.cs           # Webcam capture
│   ├── AuthenticationService.cs  # Auth orchestration
│   └── PresenceMonitorService.cs # Presence monitoring
├── ViewModels/        # MVVM view models
├── Views/             # XAML views
└── Models/            # Data models
```

**Key Patterns:**
- **MVVM**: Model-View-ViewModel pattern
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Async/Await**: All I/O operations are async

### Python Face Service

```
MedSecureVision.FaceService/
├── main.py            # Entry point
├── face_detector.py   # MediaPipe wrapper
├── face_recognizer.py # InsightFace wrapper
├── ipc_server.py      # Named pipe server
├── liveness_detector.py
└── quality_validator.py
```

**Key Patterns:**
- **Modular Design**: Each module has single responsibility
- **Type Hints**: Python type annotations
- **Logging**: Structured logging with Python logging module

### Backend API

```
MedSecureVision.Backend/
├── Controllers/       # API endpoints
├── Services/          # Business logic
├── Models/            # Entity models
├── Data/              # DbContext
└── Security/          # Security utilities
```

**Key Patterns:**
- **RESTful API**: Standard REST conventions
- **Repository Pattern**: Entity Framework Core
- **Dependency Injection**: Built-in DI container

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test MedSecureVision.Tests

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### Test Structure

```
MedSecureVision.Tests/
├── FaceDetectionTests.cs
├── FaceEmbeddingTests.cs
├── EncryptionServiceTests.cs
├── FaceVerificationServiceTests.cs
└── IntegrationTests.cs
```

### Writing Tests

**Example Unit Test:**

```csharp
[Fact]
public async Task EncryptDecrypt_ShouldRoundTripCorrectly()
{
    // Arrange
    var service = new EncryptionService(config, logger);
    var data = Encoding.UTF8.GetBytes("Test data");
    
    // Act
    var encrypted = await service.EncryptAsync(data, "user-id");
    var decrypted = await service.DecryptAsync(encrypted, "user-id");
    
    // Assert
    decrypted.Should().BeEquivalentTo(data);
}
```

**Test Guidelines:**
- Use **Arrange-Act-Assert** pattern
- One assertion per test (when possible)
- Use descriptive test names
- Mock external dependencies
- Test edge cases and error conditions

## Code Quality

### Coding Standards

1. **C# Conventions**
   - Follow Microsoft C# coding conventions
   - Use `async/await` for I/O operations
   - Use `IDisposable` for resources
   - Prefer `IEnumerable<T>` over arrays

2. **Python Conventions**
   - Follow PEP 8 style guide
   - Use type hints
   - Document functions with docstrings
   - Use virtual environments

3. **Documentation**
   - XML documentation for public APIs
   - Inline comments for complex logic
   - README files for each project

### Code Review Checklist

- [ ] Code follows style guidelines
- [ ] All tests pass
- [ ] No linter errors
- [ ] Documentation updated
- [ ] Security considerations addressed
- [ ] Performance implications considered
- [ ] Error handling implemented

## Contributing

### Development Workflow

1. **Create Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make Changes**
   - Write code
   - Add tests
   - Update documentation

3. **Commit Changes**
   ```bash
   git add .
   git commit -m "Add feature: description"
   ```

4. **Push and Create PR**
   ```bash
   git push origin feature/your-feature-name
   ```
   Then create Pull Request on GitHub

### Commit Message Format

```
Type: Short description

Longer description if needed

- Bullet point 1
- Bullet point 2
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation
- `test`: Tests
- `refactor`: Code refactoring
- `perf`: Performance improvement

### Pull Request Process

1. **Create PR** with description
2. **Ensure CI passes** (tests, linting)
3. **Request review** from team
4. **Address feedback**
5. **Merge** after approval

## Debugging

### Client Debugging

1. **Attach Debugger**
   - Set breakpoints in Visual Studio
   - Press F5 to start debugging

2. **View Logs**
   - Check `%AppData%\MedSecureVision\logs`
   - Or use Application Insights

3. **Named Pipe Debugging**
   - Use named pipe monitor tools
   - Check Python service logs

### Backend Debugging

1. **API Debugging**
   - Use Swagger UI: `https://localhost:5001/swagger`
   - Set breakpoints in controllers
   - Use Postman for API testing

2. **Database Debugging**
   - Use Entity Framework logging
   - Check SQL Server Profiler
   - Review query performance

### Python Service Debugging

1. **Python Debugger**
   ```python
   import pdb
   pdb.set_trace()  # Breakpoint
   ```

2. **Logging**
   ```python
   import logging
   logging.basicConfig(level=logging.DEBUG)
   ```

## Performance Optimization

### Client Optimization

- Use `DispatcherTimer` for UI updates
- Cache face embeddings when possible
- Optimize frame processing (skip frames if needed)

### Backend Optimization

- Use database indexes
- Implement caching (Redis)
- Optimize queries (avoid N+1)
- Use async I/O

### Python Service Optimization

- Enable GPU acceleration
- Use batch processing
- Optimize model loading
- Frame skipping for presence monitoring

## Security Considerations

### Secure Coding

1. **Input Validation**
   - Validate all user inputs
   - Sanitize data before database queries
   - Use parameterized queries

2. **Authentication**
   - Never store passwords in plain text
   - Use secure token storage
   - Implement proper session management

3. **Encryption**
   - Encrypt sensitive data at rest
   - Use TLS for data in transit
   - Protect encryption keys

4. **Error Handling**
   - Don't expose sensitive information in errors
   - Log errors securely
   - Handle exceptions gracefully

## Resources

### Documentation

- [.NET Documentation](https://docs.microsoft.com/dotnet)
- [Python Documentation](https://docs.python.org)
- [MediaPipe Documentation](https://google.github.io/mediapipe/)
- [InsightFace Documentation](https://github.com/deepinsight/insightface)

### Tools

- **Visual Studio**: IDE for C# development
- **VS Code**: Lightweight editor
- **Postman**: API testing
- **Docker**: Containerization

### Community

- GitHub Issues: Bug reports and feature requests
- Discussions: Questions and discussions
- Wiki: Additional documentation

---

*Last Updated: January 2025*






