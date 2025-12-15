# EncryptionService Module Documentation

<div align="center">

![CXA](https://img.shields.io/badge/CXA-R1M1-blue?style=for-the-badge)
![.NET 8](https://img.shields.io/badge/.NET-8.0-purple?style=for-the-badge)
![FIPS 140-2](https://img.shields.io/badge/FIPS-140--2-green?style=for-the-badge)
![AES-256](https://img.shields.io/badge/AES-256--GCM-red?style=for-the-badge)

**FIPS 140-2 Compliant Biometric Data Encryption**

</div>

---

## 📋 Table of Contents

- [Overview](#overview)
- [Security Architecture](#security-architecture)
- [Cryptographic Specifications](#cryptographic-specifications)
- [Class Reference](#class-reference)
- [Methods](#methods)
- [Key Management](#key-management)
- [HIPAA Compliance](#hipaa-compliance)
- [Usage Examples](#usage-examples)

---

## Overview

The `EncryptionService` provides FIPS 140-2 compliant encryption for biometric face templates and other sensitive data. It implements AES-256-GCM (Galois/Counter Mode) encryption with user-specific key derivation.

### Key Features

| Feature | Description |
|---------|-------------|
| **AES-256-GCM** | Industry-standard authenticated encryption |
| **User-Specific Keys** | Keys derived per-user for data isolation |
| **PBKDF2 Key Derivation** | 100,000 iterations with SHA-256 |
| **Random IV** | Unique initialization vector per encryption |
| **Authentication Tag** | Built-in integrity verification |

---

## Security Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Encryption Flow                                   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌──────────────┐     ┌──────────────┐     ┌──────────────┐        │
│  │  Master Key  │     │   User ID    │     │  Plaintext   │        │
│  │ (from config)│     │              │     │    Data      │        │
│  └──────┬───────┘     └──────┬───────┘     └──────┬───────┘        │
│         │                    │                    │                 │
│         │                    ▼                    │                 │
│         │           ┌──────────────┐              │                 │
│         │           │   SHA-256    │              │                 │
│         │           │  (Derive     │              │                 │
│         │           │    Salt)     │              │                 │
│         │           └──────┬───────┘              │                 │
│         │                  │                      │                 │
│         ▼                  ▼                      │                 │
│  ┌────────────────────────────────┐               │                 │
│  │           PBKDF2               │               │                 │
│  │    (100,000 iterations)        │               │                 │
│  │         SHA-256                │               │                 │
│  └────────────┬───────────────────┘               │                 │
│               │                                   │                 │
│               ▼                                   │                 │
│  ┌──────────────────┐                            │                 │
│  │  256-bit AES Key │                            │                 │
│  └────────┬─────────┘                            │                 │
│           │                                      │                 │
│           │    ┌──────────────┐                  │                 │
│           │    │  Random IV   │                  │                 │
│           │    │   (12 bytes) │                  │                 │
│           │    └──────┬───────┘                  │                 │
│           │           │                          │                 │
│           ▼           ▼                          ▼                 │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                      AES-256-GCM                            │   │
│  │                    (Encrypt/Decrypt)                        │   │
│  └──────────────────────────┬──────────────────────────────────┘   │
│                             │                                       │
│                             ▼                                       │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  Output: [IV (12)] + [Ciphertext (N)] + [Auth Tag (16)]     │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Cryptographic Specifications

### Algorithm Details

| Component | Specification |
|-----------|---------------|
| **Cipher** | AES-256-GCM |
| **Key Size** | 256 bits (32 bytes) |
| **IV Size** | 96 bits (12 bytes) |
| **Tag Size** | 128 bits (16 bytes) |
| **KDF** | PBKDF2-HMAC-SHA256 |
| **KDF Iterations** | 100,000 |

### Data Format

```
┌────────────────────────────────────────────────────────────────┐
│                    Encrypted Data Layout                        │
├──────────┬───────────────────────────────────┬─────────────────┤
│    IV    │           Ciphertext              │   Auth Tag      │
│ 12 bytes │          Variable                 │    16 bytes     │
├──────────┼───────────────────────────────────┼─────────────────┤
│  0-11    │           12 to (N-16)            │   (N-16) to N   │
└──────────┴───────────────────────────────────┴─────────────────┘
```

---

## Class Reference

### Namespace
```csharp
CXA.Backend.Services
```

### Class Declaration
```csharp
public class EncryptionService : IEncryptionService
```

### Dependencies

| Dependency | Purpose |
|------------|---------|
| `IConfiguration` | Access to master key configuration |
| `ILogger<EncryptionService>` | Security event logging |

### Constants

| Constant | Value | Description |
|----------|-------|-------------|
| `KeySize` | 256 | AES key size in bits |
| `Iterations` | 100000 | PBKDF2 iteration count |

---

## Methods

### EncryptAsync

Encrypts data using AES-256-GCM with a user-specific key.

```csharp
public async Task<byte[]> EncryptAsync(byte[] data, string userId)
```

**Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `data` | `byte[]` | Data to encrypt (face template bytes) |
| `userId` | `string` | User ID for key derivation |

**Returns:** Encrypted data with IV and authentication tag

**Process:**
1. Derive salt from user ID using SHA-256
2. Derive encryption key using PBKDF2
3. Generate random 12-byte IV
4. Encrypt with AES-256-GCM
5. Concatenate: IV + Ciphertext + Tag

---

### DecryptAsync

Decrypts data that was encrypted with EncryptAsync.

```csharp
public async Task<byte[]> DecryptAsync(byte[] encryptedData, string userId)
```

**Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `encryptedData` | `byte[]` | Encrypted data with IV and tag |
| `userId` | `string` | User ID for key derivation (must match) |

**Returns:** Decrypted original data

**Throws:** `CryptographicException` if:
- Wrong user ID (key mismatch)
- Data tampered (tag verification fails)
- Data corrupted

---

## Key Management

### Master Key Configuration

```json
{
  "Encryption": {
    "MasterKey": "YOUR-256-BIT-KEY-BASE64-ENCODED"
  }
}
```

### Production Recommendations

| Practice | Implementation |
|----------|----------------|
| **Key Storage** | Azure Key Vault / AWS KMS |
| **Key Rotation** | Quarterly rotation schedule |
| **Key Backup** | Encrypted offline backup |
| **Key Access** | Principle of least privilege |

### Key Derivation

```csharp
// Salt derivation (user-specific)
private byte[] DeriveSalt(string userId)
{
    using var sha256 = SHA256.Create();
    return sha256.ComputeHash(Encoding.UTF8.GetBytes(userId));
}

// Key derivation (PBKDF2)
private byte[] DeriveKey(string masterKey, byte[] salt)
{
    using var pbkdf2 = new Rfc2898DeriveBytes(
        masterKey, 
        salt, 
        Iterations, 
        HashAlgorithmName.SHA256);
    return pbkdf2.GetBytes(KeySize / 8);
}
```

---

## HIPAA Compliance

### Technical Safeguards

| Requirement | Implementation |
|-------------|----------------|
| **Encryption Standard** | AES-256 (HIPAA recommended) |
| **Key Management** | User-specific keys with master key |
| **Data Integrity** | GCM authentication tag |
| **Access Control** | Keys derived from user identity |

### Biometric Data Protection

```
┌─────────────────────────────────────────────────────────────────┐
│                  Biometric Data Lifecycle                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  [Face Captured] ──▶ [Embedding Extracted] ──▶ [Encrypted]      │
│                                                    │             │
│                                                    ▼             │
│                                            [Stored in DB]        │
│                                                    │             │
│                                                    ▼             │
│  [Verification] ◀── [Decrypted] ◀── [Retrieved from DB]         │
│                                                                  │
│  ⚠️ Raw biometric data NEVER stored - only encrypted embeddings │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Usage Examples

### Encrypting Face Template

```csharp
// During enrollment
var embedding = await _faceService.ExtractEmbeddingAsync(frame);
var embeddingBytes = JsonSerializer.SerializeToUtf8Bytes(embedding.Vector);
var encryptedTemplate = await _encryptionService.EncryptAsync(
    embeddingBytes, 
    userId.ToString());

// Store in database
var template = new FaceTemplate
{
    UserId = userId,
    EncryptedTemplate = encryptedTemplate,
    CreatedAt = DateTime.UtcNow
};
await _context.FaceTemplates.AddAsync(template);
```

### Decrypting for Verification

```csharp
// During authentication
var template = await _context.FaceTemplates
    .FirstOrDefaultAsync(t => t.UserId == userId);

var decryptedBytes = await _encryptionService.DecryptAsync(
    template.EncryptedTemplate,
    userId.ToString());

var storedEmbedding = JsonSerializer.Deserialize<float[]>(decryptedBytes);
var similarity = CalculateCosineSimilarity(inputEmbedding, storedEmbedding);
```

### Error Handling

```csharp
try
{
    var decrypted = await _encryptionService.DecryptAsync(data, userId);
}
catch (CryptographicException ex)
{
    _logger.LogError(ex, "Decryption failed - possible tampering or wrong key");
    await _auditService.LogSecurityAlert("decryption_failed", userId);
    throw new SecurityException("Data integrity verification failed");
}
```

---

## Testing

### Unit Test Examples

```csharp
[Fact]
public async Task EncryptDecrypt_RoundTrip_Success()
{
    var data = Encoding.UTF8.GetBytes("test data");
    var userId = "test-user-123";
    
    var encrypted = await _service.EncryptAsync(data, userId);
    var decrypted = await _service.DecryptAsync(encrypted, userId);
    
    Assert.Equal(data, decrypted);
}

[Fact]
public async Task Decrypt_WrongUserId_ThrowsException()
{
    var data = Encoding.UTF8.GetBytes("test data");
    var encrypted = await _service.EncryptAsync(data, "user-1");
    
    await Assert.ThrowsAsync<CryptographicException>(() =>
        _service.DecryptAsync(encrypted, "user-2"));
}
```

---

## Related Documentation

- [Face Verification Service](./FaceVerificationService.md)
- [HIPAA Compliance Guide](../HIPAA_COMPLIANCE.md)
- [Key Management Best Practices](../DEPLOYMENT.md#key-management)

---

<div align="center">

**CXA** - HIPAA-Compliant Biometric Authentication

*Protecting Biometric Data with Military-Grade Encryption*

*© 2024 CXA. All rights reserved.*

</div>






