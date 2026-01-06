# 🚀 Quick Start Guide

Get the FCM Demo running in **5 minutes**!

## Step 1: Firebase Setup (5 minutes)

### 1.1 Create Firebase Project
1. Go to https://console.firebase.google.com/
2. Click "Add project" → Name: `fcm-demo-aspnet-web`
3. Disable Google Analytics → Create project

### 1.2 Add Web App
1. Click web icon (`</>`) → App nickname: `FCM Demo Web Client`
2. **Copy the config object** and save it somewhere

### 1.3 Download Service Account Key
1. Click ⚙️ → Project settings → Service accounts
2. Click "Generate new private key" → Download JSON
3. **Rename to `firebase-adminsdk.json`**
4. **Move to**: `backend/FCMDemo.API/firebase-adminsdk.json`

### 1.4 Get VAPID Key
1. Project settings → Cloud Messaging tab
2. Web Push certificates → Generate key pair
3. **Copy the key** (starts with `B...`)

---

## Step 2: Configure Files (2 minutes)

### 2.1 Backend Configuration
**File**: `backend/FCMDemo.API/appsettings.json`

```json
{
  "Firebase": {
    "ProjectId": "YOUR_PROJECT_ID",  // ← Change this
    "CredentialsPath": "firebase-adminsdk.json"
  }
}
```

### 2.2 Web Client Firebase Config
**File**: `client/web/firebase-config.js`

Replace lines 8-14 with your Firebase config from Step 1.2:
```javascript
const firebaseConfig = {
    apiKey: "AIza...",                    // ← Your values
    authDomain: "your-project.firebaseapp.com",
    projectId: "your-project-id",
    storageBucket: "your-project.appspot.com",
    messagingSenderId: "123456789",
    appId: "1:123456789:web:abc123..."
};
```

### 2.3 Service Worker Config
**File**: `client/web/firebase-messaging-sw.js`

Replace lines 6-12 with the **same config** as above.

### 2.4 VAPID Key
**File**: `client/web/app.js`

Line 5, replace:
```javascript
const VAPID_KEY = 'YOUR_VAPID_KEY_HERE';  // ← Paste VAPID key from Step 1.4
```

---

## Step 3: Run Backend (1 minute)

Open terminal:
```bash
cd c:\Workspaces\Learn\Labs\firebase\cloud-messaging\backend\FCMDemo.API
dotnet run
```

✅ Should see: `FCM Demo API started successfully`
✅ Open browser: https://localhost:7001 (Swagger UI)

**Keep this terminal open!**

---

## Step 4: Run Web Client (1 minute)

Open **new terminal**:

### Option A: Using http-server
```bash
cd c:\Workspaces\Learn\Labs\firebase\cloud-messaging\client\web
npx http-server -p 8080 -c-1
```

### Option B: Using Python
```bash
cd c:\Workspaces\Learn\Labs\firebase\cloud-messaging\client\web
python -m http.server 8080
```

✅ Open browser: http://localhost:8080

---

## Step 5: Test! (2 minutes)

1. **Request Permission**
   - Click "Request Permission" button
   - Allow notifications in browser

2. **Get Token**
   - Click "Get Token" button
   - Token appears in text field

3. **Register Token**
   - Enter User ID: `test-user-1`
   - Enter Device ID: `web-browser-1`
   - Click "Register Token"
   - ✅ See success message

4. **Send Notification**
   - Fill in Title: `Hello!`
   - Fill in Body: `Test notification`
   - Click "Send Simple Notification"
   - ✅ See browser notification!

5. **Test Background**
   - **Minimize browser** or switch tabs
   - Go to Swagger UI: https://localhost:7001
   - Use POST `/api/notifications/send` with your token
   - ✅ See OS notification!

---

## 🎉 Success!

You now have a working FCM demo!

### Next Steps:
- Try "Send with Data" button
- Test with multiple browser tabs
- Explore API endpoints in Swagger
- Read [docs/SETUP.md](docs/SETUP.md) for detailed info

---

## 🐛 Troubleshooting

### "Firebase credentials file not found"
→ Make sure `firebase-adminsdk.json` is in `backend/FCMDemo.API/` folder

### "Permission denied"
→ Check browser notification settings (not blocked)

### Service worker errors
→ Make sure you're on `localhost` or `https://`

### CORS errors
→ Backend must be running. Restart backend API if needed.

### Still stuck?
→ See [docs/SETUP.md#troubleshooting](docs/SETUP.md#troubleshooting)

---

## 📚 Full Documentation

- [FIREBASE_SETUP.md](docs/FIREBASE_SETUP.md) - Detailed Firebase setup
- [SETUP.md](docs/SETUP.md) - Complete setup guide
- [API_DOCS.md](docs/API_DOCS.md) - API reference
- [README.md](README.md) - Project overview

---

**Need help?** All docs are in the `docs/` folder!
