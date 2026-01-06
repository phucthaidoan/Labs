# FCM Demo Setup Guide

Complete setup and running instructions for the Firebase Cloud Messaging demo project.

## Prerequisites

### Required Software

- **.NET 9.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Visual Studio 2022** (optional, but recommended) or **VS Code** with C# extension
- **Web Server** for serving static files:
  - Option 1: Node.js with `http-server` (`npm install -g http-server`)
  - Option 2: Python 3 (built-in `http.server`)
  - Option 3: Visual Studio Code with Live Server extension
- **Modern Web Browser**:
  - Google Chrome (recommended)
  - Microsoft Edge
  - Firefox

### Firebase Configuration

Before proceeding, **complete the Firebase setup** first:
- Follow [FIREBASE_SETUP.md](FIREBASE_SETUP.md) to configure Firebase Console
- Ensure you have all required configuration files

---

## Project Structure

```
firebase/cloud-messaging/
├── backend/
│   └── FCMDemo.API/                 # ASP.NET Core 9 Web API
│       ├── Controllers/
│       ├── Services/
│       ├── Data/
│       ├── Models/
│       ├── Program.cs
│       └── firebase-adminsdk.json   # Your Firebase credentials
├── client/
│   └── web/                         # Web client
│       ├── index.html
│       ├── app.js
│       ├── firebase-config.js
│       ├── firebase-messaging-sw.js
│       ├── api-service.js
│       └── styles.css
└── docs/                            # Documentation
```

---

## Step 1: Backend Setup (ASP.NET Core API)

### 1.1 Navigate to Backend Directory

```bash
cd c:\Workspaces\Learn\Labs\firebase\cloud-messaging\backend\FCMDemo.API
```

### 1.2 Verify Firebase Credentials

Ensure `firebase-adminsdk.json` exists in the backend folder:
```bash
dir firebase-adminsdk.json
```

If not found, refer to [FIREBASE_SETUP.md](FIREBASE_SETUP.md) Step 3.

### 1.3 Restore NuGet Packages

```bash
dotnet restore
```

### 1.4 Build the Project

```bash
dotnet build
```

### 1.5 Run the API

```bash
dotnet run
```

Expected output:
```
[12:00:00 INF] Firebase Admin SDK initialized successfully
[12:00:00 INF] FCM Demo API started successfully
[12:00:00 INF] Swagger UI available at: https://localhost:7001
[12:00:00 INF] Now listening on: https://localhost:7001
```

### 1.6 Verify API is Running

Open your browser and navigate to:
- **Swagger UI**: https://localhost:7001

You should see the API documentation with endpoints:
- `/api/devicetokens/register`
- `/api/devicetokens/{deviceId}`
- `/api/devicetokens/user/{userId}`
- `/api/notifications/send`
- `/api/notifications/send-with-data`
- `/api/notifications/send-to-user/{userId}`

**Keep this terminal window open** - the API needs to stay running.

---

## Step 2: Web Client Setup

### 2.1 Navigate to Web Client Directory

Open a **new terminal window** and navigate to:

```bash
cd c:\Workspaces\Learn\Labs\firebase\cloud-messaging\client\web
```

### 2.2 Serve the Web Client

Choose one of the following methods:

#### Option A: Using http-server (Node.js)

If you have Node.js installed:

```bash
# Install http-server globally (one-time)
npm install -g http-server

# Serve the web client
http-server -p 8080 -c-1
```

Access the app at: **http://localhost:8080**

#### Option B: Using Python

If you have Python 3 installed:

```bash
python -m http.server 8080
```

Access the app at: **http://localhost:8080**

#### Option C: Using VS Code Live Server

1. Open the `client/web` folder in VS Code
2. Install "Live Server" extension (if not already installed)
3. Right-click `index.html` and select "Open with Live Server"
4. The app will open automatically (usually at `http://127.0.0.1:5500`)

---

## Step 3: Test the Application

### 3.1 Open the Web Client

Navigate to: **http://localhost:8080** (or your server URL)

You should see the FCM Demo web interface.

### 3.2 Request Notification Permission

1. Click **"Request Permission"** button
2. Your browser will show a permission dialog
3. Click **"Allow"** to grant notification permission
4. Status should change to "granted"

### 3.3 Get FCM Token

1. Click **"Get Token"** button
2. Wait a few seconds
3. Your FCM token will appear in the text field
4. Click **"Copy"** to copy the token to clipboard

### 3.4 Register Token with Backend

1. Enter a **User ID** (e.g., `test-user-1`)
2. Enter a **Device ID** (e.g., `web-browser-1`)
3. Click **"Register Token"**
4. You should see a success message

### 3.5 Send a Test Notification

#### Method 1: Using Web UI

1. Fill in notification **Title** and **Body**
2. Click **"Send Simple Notification"**
3. You should see:
   - Success message
   - Browser notification (if tab is focused)
   - Notification appears in "Received Notifications" section

#### Method 2: Using Swagger UI

1. Open Swagger UI: https://localhost:7001
2. Navigate to **POST /api/notifications/send**
3. Click **"Try it out"**
4. Enter the following JSON (replace with your token):

```json
{
  "deviceToken": "YOUR_FCM_TOKEN_HERE",
  "title": "Test from Swagger",
  "body": "This notification was sent via Swagger UI"
}
```

5. Click **"Execute"**
6. Check your browser for the notification

### 3.6 Test Background Notifications

1. **Minimize** or **switch away** from the browser tab
2. Send a notification using Swagger UI (Method 2 above)
3. You should see a **system notification** from your OS
4. Click the notification to return to the app

### 3.7 Send Notification with Data

1. Click **"Send with Data"** button
2. Check the received notification - it will include custom data payload
3. Open browser DevTools (F12) → Console to see the data logged

---

## Verification Checklist

✅ Backend API running on https://localhost:7001
✅ Swagger UI accessible
✅ Web client accessible on http://localhost:8080
✅ Notification permission granted in browser
✅ FCM token generated successfully
✅ Token registered with backend API
✅ Simple notification sent and received
✅ Background notification works
✅ Notification with data payload works

---

## Troubleshooting

### Backend Issues

#### "Firebase credentials file not found"
**Solution**:
- Ensure `firebase-adminsdk.json` exists in `backend/FCMDemo.API/`
- Check file name matches exactly: `firebase-adminsdk.json`

#### "Port 7001 is already in use"
**Solution**:
```bash
# Find process using port 7001
netstat -ano | findstr :7001
# Kill the process (replace PID with actual process ID)
taskkill /PID <PID> /F
```

#### API starts but can't send notifications
**Solution**:
- Check `appsettings.json` has correct `ProjectId`
- Verify Firebase credentials are valid
- Check API logs for specific error messages

### Web Client Issues

#### "Firebase configuration is not defined"
**Solution**:
- Update `firebase-config.js` with your Firebase configuration
- Update `firebase-messaging-sw.js` with same configuration
- Clear browser cache and reload

#### "Messaging: We are unable to register the default service worker"
**Solution**:
- Ensure `firebase-messaging-sw.js` is in the web root directory
- Serve over HTTPS or localhost (HTTP won't work for service workers except on localhost)
- Check browser console for specific errors

#### Notifications not appearing
**Solution**:
- Verify notification permission is "granted"
- Check browser notification settings (not blocked)
- Open DevTools → Console for error messages
- Ensure the API backend is running

#### CORS errors in console
**Solution**:
- Verify your web server URL is in the CORS policy in `Program.cs`
- Restart the backend API after making CORS changes

#### Service Worker registration fails
**Solution**:
- Ensure you're serving from localhost or HTTPS
- Check that `firebase-messaging-sw.js` is accessible at `/firebase-messaging-sw.js`
- Clear service worker cache: DevTools → Application → Service Workers → Unregister

---

## Development Tips

### Auto-restart Backend on Changes

Use `dotnet watch`:

```bash
cd backend/FCMDemo.API
dotnet watch run
```

### View Backend Logs

Logs are output to the console. For more detailed logging, change log level in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

### Clear In-Memory Database

Simply restart the backend API - data is not persisted.

### Test with Multiple Browsers

Open the web client in multiple browsers/windows with different User IDs to simulate multiple users.

---

## Next Steps

- Explore the [API Documentation](API_DOCS.md) for all endpoint details
- See [REACT_NATIVE_SETUP.md](REACT_NATIVE_SETUP.md) for mobile app setup (future)
- Test different notification scenarios
- Modify notification payloads and data structures

---

## Stopping the Application

1. **Backend**: Press `Ctrl+C` in the API terminal
2. **Web Server**: Press `Ctrl+C` in the web server terminal
3. **Clear Data**: No action needed (in-memory database)

---

## Production Considerations

This demo uses in-memory database and simplified configuration. For production:

- Replace in-memory database with SQL Server or PostgreSQL
- Add API authentication (JWT tokens)
- Implement rate limiting
- Store Firebase credentials in Azure Key Vault
- Use environment variables for configuration
- Add comprehensive error handling
- Implement notification delivery tracking
- Add user notification preferences
