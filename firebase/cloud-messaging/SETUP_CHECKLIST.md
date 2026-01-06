# Setup Checklist ✅

Use this checklist to ensure everything is configured correctly.

---

## 📋 Pre-Setup

- [ ] .NET 9.0 SDK installed (`dotnet --version`)
- [ ] Node.js or Python installed (for web server)
- [ ] Modern browser (Chrome/Edge/Firefox)
- [ ] Google account for Firebase

---

## 🔥 Firebase Console Setup

### Create Project
- [ ] Visited https://console.firebase.google.com/
- [ ] Created new project (name: `fcm-demo-aspnet-web`)
- [ ] Disabled Google Analytics (optional)

### Register Web App
- [ ] Clicked web icon (`</>`) in Firebase Console
- [ ] Registered app (nickname: `FCM Demo Web Client`)
- [ ] **COPIED** Firebase configuration object
- [ ] Saved config somewhere accessible

### Service Account Key
- [ ] Opened Project Settings (⚙️ icon)
- [ ] Went to "Service accounts" tab
- [ ] Clicked "Generate new private key"
- [ ] **DOWNLOADED** JSON file
- [ ] **RENAMED** to `firebase-adminsdk.json`
- [ ] **MOVED** to `backend/FCMDemo.API/firebase-adminsdk.json`

### VAPID Key
- [ ] Opened Project Settings → Cloud Messaging tab
- [ ] Found "Web Push certificates" section
- [ ] Generated key pair (or copied existing)
- [ ] **COPIED** VAPID key (starts with `B...`)

---

## ⚙️ Configuration Files

### Backend: appsettings.json
**File**: `backend/FCMDemo.API/appsettings.json`

- [ ] Opened file in editor
- [ ] Found `"Firebase"` section (line 10)
- [ ] Replaced `"ProjectId": "your-firebase-project-id"` with actual Project ID
- [ ] Saved file

Example:
```json
"Firebase": {
  "ProjectId": "fcm-demo-aspnet-web",  // ← Your project ID here
  "CredentialsPath": "firebase-adminsdk.json"
}
```

### Web Client: firebase-config.js
**File**: `client/web/firebase-config.js`

- [ ] Opened file in editor
- [ ] Found `firebaseConfig` object (line 8)
- [ ] Replaced ALL placeholder values with config from Firebase Console
- [ ] Saved file

Example:
```javascript
const firebaseConfig = {
    apiKey: "AIzaSyD...",                          // ← Your values
    authDomain: "fcm-demo-aspnet-web.firebaseapp.com",
    projectId: "fcm-demo-aspnet-web",
    storageBucket: "fcm-demo-aspnet-web.appspot.com",
    messagingSenderId: "123456789012",
    appId: "1:123456789012:web:abc123def456"
};
```

### Service Worker: firebase-messaging-sw.js
**File**: `client/web/firebase-messaging-sw.js`

- [ ] Opened file in editor
- [ ] Found `firebaseConfig` object (line 6)
- [ ] Replaced with **SAME CONFIG** as firebase-config.js
- [ ] Saved file

### VAPID Key: app.js
**File**: `client/web/app.js`

- [ ] Opened file in editor
- [ ] Found line 5: `const VAPID_KEY = ...`
- [ ] Replaced `'YOUR_VAPID_KEY_HERE'` with actual VAPID key
- [ ] Saved file

Example:
```javascript
const VAPID_KEY = 'BNxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx...';
```

---

## 🏗️ Build Verification

### Backend Build
- [ ] Opened terminal
- [ ] Navigated to `backend/FCMDemo.API`
- [ ] Ran `dotnet build`
- [ ] ✅ Build succeeded (no errors)

### File Structure Check
- [ ] `firebase-adminsdk.json` exists in `backend/FCMDemo.API/`
- [ ] `firebase-adminsdk.json` is in `.gitignore` (CRITICAL!)
- [ ] All web client files exist in `client/web/`

---

## 🚀 Running the Application

### Backend API
- [ ] Opened terminal #1
- [ ] Ran: `cd backend/FCMDemo.API`
- [ ] Ran: `dotnet run`
- [ ] ✅ Saw: "FCM Demo API started successfully"
- [ ] ✅ Saw: "Swagger UI available at: https://localhost:7001"
- [ ] Opened browser: https://localhost:7001
- [ ] ✅ Swagger UI loaded successfully
- [ ] Kept terminal open (backend running)

### Web Client
- [ ] Opened terminal #2 (new window)
- [ ] Ran: `cd client/web`
- [ ] Started web server:
  - [ ] Option A: `npx http-server -p 8080 -c-1`
  - [ ] Option B: `python -m http.server 8080`
- [ ] Opened browser: http://localhost:8080
- [ ] ✅ Web page loaded
- [ ] ✅ No console errors (F12 → Console)
- [ ] Kept terminal open (web server running)

---

## 🧪 Testing

### Test 1: Request Permission
- [ ] Clicked "Request Permission" button
- [ ] Browser showed permission dialog
- [ ] Clicked "Allow"
- [ ] ✅ Status changed to "granted"

### Test 2: Get Token
- [ ] Clicked "Get Token" button
- [ ] ✅ Token appeared in text field
- [ ] ✅ Success message showed
- [ ] Clicked "Copy" button
- [ ] ✅ Token copied to clipboard

### Test 3: Register Token
- [ ] Entered User ID: `test-user-1`
- [ ] Entered Device ID: `web-browser-1`
- [ ] Clicked "Register Token"
- [ ] ✅ Success message showed
- [ ] ✅ Backend logs showed registration

### Test 4: Send Simple Notification
- [ ] Filled Title: `Hello FCM!`
- [ ] Filled Body: `This is a test notification`
- [ ] Clicked "Send Simple Notification"
- [ ] ✅ Success message showed
- [ ] ✅ Browser notification appeared
- [ ] ✅ Notification in "Received Notifications" list

### Test 5: Background Notification
- [ ] Minimized browser window OR switched to another tab
- [ ] Opened Swagger UI in another window: https://localhost:7001
- [ ] Expanded POST `/api/notifications/send`
- [ ] Clicked "Try it out"
- [ ] Entered request body with FCM token
- [ ] Clicked "Execute"
- [ ] ✅ Response 200 OK
- [ ] ✅ OS system notification appeared
- [ ] Clicked notification
- [ ] ✅ Browser tab opened/focused

### Test 6: Notification with Data
- [ ] In web client, clicked "Send with Data"
- [ ] ✅ Notification sent successfully
- [ ] Opened browser console (F12)
- [ ] ✅ Data payload logged in console

---

## 🔍 Verification

### Backend Endpoints (Swagger)
- [ ] GET /api/devicetokens/user/{userId} - visible
- [ ] POST /api/devicetokens/register - visible
- [ ] DELETE /api/devicetokens/{deviceId} - visible
- [ ] POST /api/notifications/send - visible
- [ ] POST /api/notifications/send-with-data - visible
- [ ] POST /api/notifications/send-to-user/{userId} - visible

### Web Client Features
- [ ] Permission section works
- [ ] Token section works
- [ ] Registration section works
- [ ] Send notification section works
- [ ] Received notifications section displays
- [ ] All buttons respond to clicks
- [ ] No JavaScript errors in console

---

## ✅ Success Criteria

All of these should be true:

- ✅ Backend builds without errors
- ✅ Backend runs on https://localhost:7001
- ✅ Swagger UI loads and shows all endpoints
- ✅ Web client loads on http://localhost:8080
- ✅ Notification permission can be requested
- ✅ FCM token generates successfully
- ✅ Token registers with backend
- ✅ Foreground notifications work
- ✅ Background notifications work
- ✅ Notifications with data work
- ✅ No errors in browser console
- ✅ No errors in backend logs

---

## 🐛 Troubleshooting

If something doesn't work:

### Backend Issues
**Error**: "Firebase credentials file not found"
- [ ] Check `firebase-adminsdk.json` is in `backend/FCMDemo.API/`
- [ ] Check filename is exact: `firebase-adminsdk.json` (no typos)
- [ ] Check `appsettings.json` has correct path

**Error**: Port 7001 already in use
- [ ] Stop any other .NET apps running
- [ ] Or change port in `Properties/launchSettings.json`

**Error**: Build fails
- [ ] Run `dotnet restore`
- [ ] Check .NET 9.0 SDK installed
- [ ] Check all NuGet packages restored

### Frontend Issues
**Error**: "Firebase configuration is not defined"
- [ ] Check `firebase-config.js` has your config
- [ ] Check `firebase-messaging-sw.js` has same config
- [ ] Clear browser cache (Ctrl+Shift+Delete)

**Error**: Service worker registration failed
- [ ] Ensure using `localhost` or `https://`
- [ ] Check `firebase-messaging-sw.js` is in `client/web/` folder
- [ ] Check no syntax errors in service worker file

**Error**: CORS error
- [ ] Ensure backend API is running
- [ ] Check web client URL matches CORS policy in `Program.cs`
- [ ] Restart backend if you changed CORS settings

**Error**: "Permission denied"
- [ ] Check browser notification settings
- [ ] Try different browser
- [ ] Ensure not in Incognito/Private mode

### Still Stuck?
- [ ] Read [docs/SETUP.md#troubleshooting](docs/SETUP.md#troubleshooting)
- [ ] Check browser console for errors
- [ ] Check backend logs for errors
- [ ] Verify all config files updated

---

## 📚 Next Steps

After everything works:

- [ ] Read [README.md](README.md) for project overview
- [ ] Explore [docs/API_DOCS.md](docs/API_DOCS.md) for API details
- [ ] Try different notification scenarios
- [ ] Test with multiple browser tabs
- [ ] Customize notification messages
- [ ] Explore the code to understand how it works

---

## 🎓 Learning Path

1. **First Run** - Get it working (this checklist)
2. **Understand** - Read the code and documentation
3. **Experiment** - Modify and test different scenarios
4. **Extend** - Add new features or customize

**Congratulations on completing the setup! 🎉**

---

**Last Updated**: December 2025
**Status**: Ready to use!
