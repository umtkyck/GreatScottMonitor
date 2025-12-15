# MedSecure Vision - API Reference

## Base URL

```
https://api.medsecurevision.com
```

## Authentication

All API endpoints (except `/api/auth/callback`) require JWT authentication via Auth0.

Include the access token in the Authorization header:

```
Authorization: Bearer <access_token>
```

## Endpoints

### Authentication

#### POST /api/auth/verify

Verify face embedding against enrolled templates.

**Request:**
```json
{
  "faceEmbedding": [0.123, 0.456, ...],
  "workstationId": "WORKSTATION-001"
}
```

**Response (Success):**
```json
{
  "success": true,
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "userName": "John Doe",
  "confidenceScore": 0.95,
  "sessionToken": "session-token-here"
}
```

**Response (Failure):**
```json
{
  "success": false,
  "error": "Face not recognized"
}
```

**Status Codes:**
- 200: Success (match or no match)
- 400: Bad request
- 500: Internal server error

---

#### POST /api/enrollment/start

Initiate face enrollment process.

**Request:**
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response:**
```json
{
  "enrollmentId": "enrollment-id-here"
}
```

**Status Codes:**
- 200: Success
- 404: User not found
- 500: Internal server error

---

#### POST /api/enrollment/upload-template

Upload encrypted face template.

**Request:**
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "faceEmbeddings": [
    [0.123, 0.456, ...],
    [0.789, 0.012, ...]
  ],
  "qualityScore": 0.85
}
```

**Response:**
```json
{
  "success": true,
  "enrollmentId": "enrollment-id-here"
}
```

**Status Codes:**
- 200: Success
- 400: Bad request
- 404: User not found
- 500: Internal server error

---

#### POST /api/enrollment/complete

Complete enrollment process.

**Request:**
```json
{
  "enrollmentId": "enrollment-id-here",
  "userId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response:**
```json
{
  "message": "Enrollment completed"
}
```

---

### Audit Logs

#### GET /api/audit-logs

Retrieve audit logs with optional filtering.

**Query Parameters:**
- `startDate` (optional): ISO 8601 date
- `endDate` (optional): ISO 8601 date
- `eventType` (optional): authentication, enrollment, lock, unlock, admin_action, security_alert
- `userId` (optional): User ID (UUID)

**Response:**
```json
[
  {
    "logId": "550e8400-e29b-41d4-a716-446655440000",
    "eventType": "authentication",
    "timestamp": "2025-01-15T10:30:00Z",
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "workstationId": "WORKSTATION-001",
    "ipAddress": "192.168.1.100",
    "result": "success",
    "confidenceScore": 0.95,
    "failureReason": null,
    "sessionId": "session-id-here",
    "metadata": "{\"embedding_length\": 512}"
  }
]
```

**Status Codes:**
- 200: Success
- 401: Unauthorized
- 500: Internal server error

---

#### GET /api/audit-logs/export

Export audit logs as CSV.

**Query Parameters:**
- `startDate` (optional): ISO 8601 date
- `endDate` (optional): ISO 8601 date

**Response:**
- Content-Type: `text/csv`
- File download with CSV data

**Status Codes:**
- 200: Success
- 401: Unauthorized
- 500: Internal server error

---

### Users

#### GET /api/users

Get list of users (Admin only).

**Response:**
```json
[
  {
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "email": "user@example.com",
    "name": "John Doe",
    "role": "User",
    "status": "active",
    "enrolledAt": "2025-01-10T08:00:00Z",
    "lastActive": "2025-01-15T10:30:00Z"
  }
]
```

---

#### POST /api/users/{userId}/suspend

Suspend a user account (Admin only).

**Response:**
```json
{
  "message": "User suspended"
}
```

---

#### POST /api/users/{userId}/activate

Activate a suspended user account (Admin only).

**Response:**
```json
{
  "message": "User activated"
}
```

---

#### POST /api/users/{userId}/re-enroll

Force re-enrollment for a user (Admin only).

**Response:**
```json
{
  "message": "Re-enrollment initiated"
}
```

---

### Policies

#### GET /api/policies

Get current policy configuration (Admin only).

**Response:**
```json
{
  "absenceTimeoutSeconds": 5,
  "retryAttemptsBeforeLockout": 3,
  "fallbackMethods": ["pin", "windows_hello"],
  "multiFacePolicy": "lock",
  "workingHoursStart": "08:00",
  "workingHoursEnd": "17:00"
}
```

---

#### POST /api/policies

Update policy configuration (Admin only).

**Request:**
```json
{
  "absenceTimeoutSeconds": 5,
  "retryAttemptsBeforeLockout": 3,
  "fallbackMethods": ["pin", "windows_hello"],
  "multiFacePolicy": "lock",
  "workingHoursStart": "08:00",
  "workingHoursEnd": "17:00"
}
```

**Response:**
```json
{
  "message": "Policy configuration updated"
}
```

---

## Error Responses

All endpoints may return error responses in the following format:

```json
{
  "error": "Error message here"
}
```

**Common Status Codes:**
- 400: Bad Request - Invalid input
- 401: Unauthorized - Missing or invalid token
- 403: Forbidden - Insufficient permissions
- 404: Not Found - Resource not found
- 500: Internal Server Error - Server error

## Rate Limiting

API requests are rate-limited:
- Authentication endpoints: 10 requests per minute per IP
- Other endpoints: 100 requests per minute per user

Rate limit headers:
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1640000000
```

## Webhooks (Future)

Webhooks may be configured for:
- Authentication events
- Enrollment completion
- Security alerts
- Session lock events






