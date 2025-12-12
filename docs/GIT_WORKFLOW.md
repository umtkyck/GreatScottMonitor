# Git Workflow Guide

## Overview

This guide explains the Git workflow for MedSecure Vision project. All commits should be in English.

## Initial Setup

### 1. Install Git

Download and install Git from: https://git-scm.com/download/win

### 2. Configure Git

```bash
git config --global user.name "Your Name"
git config --global user.email "your.email@example.com"
```

### 3. Initialize Repository

```bash
cd C:\Projects\GreatScottMonitor
git init
```

## Making Commits

### Quick Start (Automated)

Run the provided PowerShell script:

```powershell
.\COMMIT_SCRIPT.ps1
```

### Manual Commits

Follow the commit messages in `COMMIT_MESSAGES.md` for organized commits.

## Commit Message Format

We follow conventional commit format:

```
<type>: <short description>

<longer description if needed>

- Bullet point 1
- Bullet point 2
```

### Commit Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `test`: Test additions/changes
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `chore`: Maintenance tasks

### Examples

**Good commit message:**
```
feat: Add face enrollment workflow

- Implement multi-angle capture (8 frames)
- Add quality validation checks
- Add liveness detection
- Integrate with backend API
```

**Bad commit message:**
```
update
```

## Branching Strategy

### Main Branches

- `main`: Production-ready code
- `develop`: Development branch

### Feature Branches

```bash
git checkout -b feature/face-enrollment
# Make changes
git commit -m "feat: Add face enrollment"
git checkout develop
git merge feature/face-enrollment
```

### Branch Naming

- `feature/description`: New features
- `fix/description`: Bug fixes
- `docs/description`: Documentation
- `test/description`: Tests

## Regular Commits

### Best Practices

1. **Commit Often**: Make small, logical commits
2. **One Change Per Commit**: Each commit should represent one logical change
3. **Write Clear Messages**: Describe what and why, not how
4. **Test Before Committing**: Ensure code compiles and tests pass
5. **Review Before Pushing**: Review your changes with `git diff`

### Example Workflow

```bash
# Make changes to a file
# Stage changes
git add MedSecureVision.Client/Services/FaceServiceClient.cs

# Commit with descriptive message
git commit -m "docs: Add XML documentation to FaceServiceClient methods

- Document DetectFacesAsync method
- Document ExtractEmbeddingAsync method
- Add parameter descriptions
- Add return value descriptions"

# Continue working...
git add MedSecureVision.Client/Services/PresenceMonitorService.cs
git commit -m "docs: Add XML documentation to PresenceMonitorService

- Document presence monitoring logic
- Explain absence detection
- Document event handlers"
```

## Language Requirements

**All commits must be in English:**

- ✅ Commit messages in English
- ✅ Code comments in English
- ✅ Documentation in English
- ✅ Variable names in English
- ✅ Error messages in English

**Why English?**
- International collaboration
- Standard technical terminology
- Easier code reviews
- Professional documentation

## Common Commands

### Viewing Status

```bash
# Check what files have changed
git status

# See detailed changes
git diff

# See commit history
git log --oneline
```

### Staging Changes

```bash
# Stage specific file
git add path/to/file.cs

# Stage all changes
git add .

# Stage all changes in directory
git add MedSecureVision.Client/
```

### Committing

```bash
# Commit staged changes
git commit -m "Your message"

# Commit with multi-line message
git commit -m "Type: Short description" -m "Longer description"
```

### Viewing History

```bash
# One line per commit
git log --oneline

# Detailed log
git log

# Graph view
git log --oneline --graph
```

## Troubleshooting

### Undo Last Commit (Keep Changes)

```bash
git reset --soft HEAD~1
```

### Undo Last Commit (Discard Changes)

```bash
git reset --hard HEAD~1
```

### Amend Last Commit

```bash
# Add more changes
git add .
git commit --amend -m "Updated commit message"
```

### View What Will Be Committed

```bash
git diff --staged
```

## Remote Repository

### Add Remote

```bash
git remote add origin https://github.com/your-org/MedSecureVision.git
```

### Push to Remote

```bash
# First time
git push -u origin main

# Subsequent pushes
git push
```

### Pull from Remote

```bash
git pull origin main
```

## Resources

- [Git Documentation](https://git-scm.com/doc)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [GitHub Flow](https://guides.github.com/introduction/flow/)

---

*Remember: All commits and code must be in English!*


