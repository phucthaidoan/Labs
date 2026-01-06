# FCM Demo API Documentation

Complete API reference for the Firebase Cloud Messaging Demo ASP.NET Core 9 API.

**Base URL**: `https://localhost:7001/api`

**Swagger UI**: `https://localhost:7001`

---

## Table of Contents

1. [Device Tokens Endpoints](#device-tokens-endpoints)
2. [Notifications Endpoints](#notifications-endpoints)
3. [Error Responses](#error-responses)
4. [Example Requests](#example-requests)

---

## Device Tokens Endpoints

### 1. Register Device Token

Register or update a device FCM token with the backend.

**Endpoint**: `POST /api/devicetokens/register`

**Request Body**:
```json
{
  "userId": "string",      // User identifier (max 100 chars)
  "token": "string",       // FCM token from Firebase (max 500 chars)
  "deviceId": "string",    // Unique device identifier (max 200 chars)
  "platform": "string"     // Platform: "Web", "iOS", or "Android" (max 50 chars)
}
```

**Success Response** (200 OK):
```json
{
  "message": "Device token registered successfully",
  "tokenId": 1,
  "userId": "test-user-1",
  "deviceId": "web-browser-1",
  "platform": "Web",
  "registeredAt": "2025-12-12T10:30:00Z"
}
```

**Error Responses**:
- `400 Bad Request` - Invalid request data or token validation failed
- `500 Internal Server Error` - Server error

**Curl Example**:
```bash
curl -X POST https://localhost:7001/api/devicetokens/register \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "test-user-1",
    "token": "dXW7k...xyz",
    "deviceId": "web-browser-1",
    "platform": "Web"
  }'
```

---

### 2. Unregister Device Token

Deactivate a device token (soft delete).

**Endpoint**: `DELETE /api/devicetokens/{deviceId}`

**Path Parameters**:
- `deviceId` (string) - Device identifier to unregister

**Success Response** (200 OK):
```json
{
  "message": "Device token unregistered successfully",
  "deviceId": "web-browser-1"
}
```

**Error Responses**:
- `404 Not Found` - Device token not found
- `500 Internal Server Error` - Server error

**Curl Example**:
```bash
curl -X DELETE https://localhost:7001/api/devicetokens/web-browser-1
```

---

### 3. Get User Tokens

Retrieve all tokens for a specific user.

**Endpoint**: `GET /api/devicetokens/user/{userId}`

**Path Parameters**:
- `userId` (string) - User identifier

**Success Response** (200 OK):
```json
{
  "userId": "test-user-1",
  "tokenCount": 2,
  "tokens": [
    {
      "id": 1,
      "deviceId": "web-browser-1",
      "platform": "Web",
      "isActive": true,
      "registeredAt": "2025-12-12T10:30:00Z",
      "lastUsedAt": "2025-12-12T10:35:00Z",
      "token": "dXW7k...xyz (truncated)"
    },
    {
      "id": 2,
      "deviceId": "mobile-device-1",
      "platform": "Android",
      "isActive": false,
      "registeredAt": "2025-12-11T15:20:00Z",
      "lastUsedAt": null,
      "token": "eYX8m...abc (truncated)"
    }
  ]
}
```

**Curl Example**:
```bash
curl https://localhost:7001/api/devicetokens/user/test-user-1
```

---

## Notifications Endpoints

### 1. Send Simple Notification

Send a simple text notification to a specific device.

**Endpoint**: `POST /api/notifications/send`

**Request Body**:
```json
{
  "deviceToken": "string",     // Target device FCM token
  "title": "string",           // Notification title (max 100 chars)
  "body": "string",            // Notification body (max 500 chars)
  "imageUrl": "string"         // Optional image URL (max 500 chars)
}
```

**Success Response** (200 OK):
```json
{
  "success": true,
  "messageId": "projects/your-project/messages/0:1234567890",
  "error": null,
  "deviceToken": "dXW7k...xyz"
}
```

**Failure Response** (400 Bad Request):
```json
{
  "success": false,
  "messageId": null,
  "error": "Firebase error: INVALID_ARGUMENT - Invalid token format",
  "deviceToken": "invalid-token"
}
```

**Curl Example**:
```bash
curl -X POST https://localhost:7001/api/notifications/send \
  -H "Content-Type: application/json" \
  -d '{
    "deviceToken": "dXW7k...xyz",
    "title": "Hello from API!",
    "body": "This is a test notification",
    "imageUrl": "https://example.com/image.png"
  }'
```

---

### 2. Send Notification with Data

Send a notification with custom data payload.

**Endpoint**: `POST /api/notifications/send-with-data`

**Request Body**:
```json
{
  "deviceToken": "string",
  "title": "string",
  "body": "string",
  "imageUrl": "string",
  "data": {                    // Custom key-value pairs
    "key1": "value1",
    "key2": "value2"
  }
}
```

**Success Response** (200 OK):
```json
{
  "success": true,
  "messageId": "projects/your-project/messages/0:1234567890",
  "error": null,
  "deviceToken": "dXW7k...xyz"
}
```

**Curl Example**:
```bash
curl -X POST https://localhost:7001/api/notifications/send-with-data \
  -H "Content-Type: application/json" \
  -d '{
    "deviceToken": "dXW7k...xyz",
    "title": "New Message",
    "body": "You have a new message",
    "data": {
      "action": "view-message",
      "messageId": "msg-123",
      "senderId": "user-456",
      "url": "https://example.com/messages/123"
    }
  }'
```

---

### 3. Send to User by ID

Send a notification to a user by their user ID (token lookup is automatic).

**Endpoint**: `POST /api/notifications/send-to-user/{userId}`

**Path Parameters**:
- `userId` (string) - User identifier

**Request Body**:
```json
{
  "title": "string",
  "body": "string",
  "imageUrl": "string",
  "data": {
    "key1": "value1"
  }
}
```

**Success Response** (200 OK):
```json
{
  "success": true,
  "messageId": "projects/your-project/messages/0:1234567890",
  "error": null,
  "deviceToken": "dXW7k...xyz"
}
```

**Error Responses**:
- `404 Not Found` - No active device token found for user
- `400 Bad Request` - Notification send failed

**Curl Example**:
```bash
curl -X POST https://localhost:7001/api/notifications/send-to-user/test-user-1 \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Hello User!",
    "body": "This message was sent to your user ID",
    "data": {
      "type": "broadcast"
    }
  }'
```

---

## Error Responses

### Common Error Structure

All error responses follow this structure:

```json
{
  "message": "Error description",
  "error": "Detailed error message"
}
```

### HTTP Status Codes

| Status Code | Meaning | Example |
|-------------|---------|---------|
| 200 OK | Request successful | Notification sent |
| 400 Bad Request | Invalid request data | Missing required field |
| 404 Not Found | Resource not found | Device token doesn't exist |
| 500 Internal Server Error | Server error | Database connection failed |

### Firebase Error Codes

Common Firebase Messaging error codes:

| Error Code | Meaning | Solution |
|------------|---------|----------|
| INVALID_ARGUMENT | Invalid token format | Verify token is correct |
| NOT_FOUND | Token not found or expired | Re-register device |
| UNREGISTERED | Device unregistered | Remove token from database |
| SENDER_ID_MISMATCH | Token from different project | Use correct Firebase project |
| QUOTA_EXCEEDED | Rate limit exceeded | Implement rate limiting |

---

## Example Requests

### Scenario 1: Register New User Device

```bash
# Step 1: Register device token
curl -X POST https://localhost:7001/api/devicetokens/register \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "alice",
    "token": "dXW7k...xyz",
    "deviceId": "alice-laptop",
    "platform": "Web"
  }'

# Step 2: Send welcome notification
curl -X POST https://localhost:7001/api/notifications/send-to-user/alice \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Welcome Alice!",
    "body": "Thanks for registering"
  }'
```

### Scenario 2: Send Notification with Deep Link

```bash
curl -X POST https://localhost:7001/api/notifications/send \
  -H "Content-Type: application/json" \
  -d '{
    "deviceToken": "dXW7k...xyz",
    "title": "New Order",
    "body": "You have a new order #1234",
    "data": {
      "action": "view-order",
      "orderId": "1234",
      "url": "/orders/1234"
    }
  }'
```

### Scenario 3: Broadcast to Multiple Users

```bash
# Get all tokens for multiple users
for userId in user1 user2 user3; do
  curl -X POST https://localhost:7001/api/notifications/send-to-user/$userId \
    -H "Content-Type: application/json" \
    -d '{
      "title": "System Maintenance",
      "body": "Scheduled maintenance at 2 AM"
    }'
done
```

### Scenario 4: Handle Token Refresh

```bash
# Step 1: Unregister old token
curl -X DELETE https://localhost:7001/api/devicetokens/device-old-123

# Step 2: Register new token
curl -X POST https://localhost:7001/api/devicetokens/register \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "bob",
    "token": "new-token-xyz",
    "deviceId": "device-new-123",
    "platform": "Web"
  }'
```

---

## Rate Limits

**Current Implementation**: No rate limits (demo purposes)

**Production Recommendations**:
- 100 requests per minute per user
- 1000 notifications per hour per device
- Implement exponential backoff for retries

---

## Authentication

**Current Implementation**: None (demo purposes)

**Production Recommendations**:
- Implement JWT Bearer authentication
- Add API key validation
- Use role-based access control (RBAC)

Example with JWT:
```bash
curl -X POST https://localhost:7001/api/notifications/send \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer your-jwt-token" \
  -d '{ ... }'
```

---

## Postman Collection

You can import the API endpoints into Postman:

1. Open Postman
2. Import → Link
3. Enter: `https://localhost:7001/swagger/v1/swagger.json`
4. All endpoints will be imported automatically

---

## Testing Tips

### Using Swagger UI

1. Navigate to `https://localhost:7001`
2. Click on any endpoint to expand
3. Click **"Try it out"**
4. Fill in parameters
5. Click **"Execute"**
6. View response

### Using curl with Self-Signed Certificate

If you get SSL errors with curl, use `-k` flag:

```bash
curl -k -X POST https://localhost:7001/api/notifications/send ...
```

### Testing Error Scenarios

```bash
# Test invalid token
curl -X POST https://localhost:7001/api/notifications/send \
  -H "Content-Type: application/json" \
  -d '{
    "deviceToken": "invalid",
    "title": "Test",
    "body": "Test"
  }'

# Test missing required field
curl -X POST https://localhost:7001/api/notifications/send \
  -H "Content-Type: application/json" \
  -d '{
    "deviceToken": "dXW7k...xyz"
  }'
```

---

## Database Schema

### DeviceToken Entity

```csharp
{
  "id": int,              // Primary key
  "token": string,        // FCM token
  "userId": string,       // User identifier
  "deviceId": string,     // Device identifier (unique)
  "platform": string,     // "Web", "iOS", "Android"
  "isActive": bool,       // Active status
  "registeredAt": DateTime,
  "lastUsedAt": DateTime?,
  "updatedAt": DateTime?
}
```

**Indexes**:
- Unique index on `deviceId`
- Index on `userId`
- Composite index on `userId` + `isActive`

---

## Additional Resources

- [Firebase Cloud Messaging Documentation](https://firebase.google.com/docs/cloud-messaging)
- [Firebase Admin SDK for .NET](https://firebase.google.com/docs/admin/setup)
- [ASP.NET Core Web API Tutorial](https://docs.microsoft.com/en-us/aspnet/core/tutorials/first-web-api)
