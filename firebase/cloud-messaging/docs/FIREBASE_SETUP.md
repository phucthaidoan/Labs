# Firebase Console Setup Guide

This guide walks you through setting up Firebase Cloud Messaging for the FCM Demo project.

## Prerequisites

- Google account
- Web browser

---

## Step 1: Create Firebase Project

1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Click **"Add project"** or **"Create a project"**
3. Enter project name: `fcm-demo-aspnet-web` (or your preferred name)
4. Click **"Continue"**
5. (Optional) Disable Google Analytics for this demo project
6. Click **"Create project"**
7. Wait for project creation to complete
8. Click **"Continue"** to enter your project

---

## Step 2: Register Web App

1. In the Firebase project overview page, click the **Web icon** (`</>`) to add a web app
2. Register your app:
   - **App nickname**: `FCM Demo Web Client`
   - **Also set up Firebase Hosting**: Leave unchecked (optional)
3. Click **"Register app"**
4. **Copy the Firebase configuration object** - you'll need this later:

```javascript
const firebaseConfig = {
    apiKey: "AIza...",
    authDomain: "your-project.firebaseapp.com",
    projectId: "your-project-id",
    storageBucket: "your-project.appspot.com",
    messagingSenderId: "123456789",
    appId: "1:123456789:web:abc123..."
};
```

5. Click **"Continue to console"**

---

## Step 3: Generate Service Account Key (for Backend)

1. In Firebase Console, click the **gear icon** (⚙️) next to "Project Overview"
2. Select **"Project settings"**
3. Navigate to the **"Service accounts"** tab
4. Click **"Generate new private key"**
5. A dialog will appear warning you to keep this file secure
6. Click **"Generate key"**
7. A JSON file will be downloaded (e.g., `your-project-firebase-adminsdk-xxxxx.json`)
8. **Rename the file** to `firebase-adminsdk.json`
9. **Move it to**: `c:\Workspaces\Learn\Labs\firebase\cloud-messaging\backend\FCMDemo.API\firebase-adminsdk.json`

**⚠️ IMPORTANT: Never commit this file to Git! It contains sensitive credentials.**

---

## Step 4: Get Web Push Certificate (VAPID Key)

1. Still in **Project Settings**, navigate to the **"Cloud Messaging"** tab
2. Scroll down to **"Web configuration"** section
3. Under **"Web Push certificates"**, you should see:
   - If you see a key pair: **Copy the "Key pair" value** (starts with `B...`)
   - If no key pair exists: Click **"Generate key pair"**, then copy the generated key

4. This is your **VAPID key** - you'll need it for the web client

Example VAPID key:
```
BNxxx...xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

---

## Step 5: Enable Cloud Messaging API

1. In the **"Cloud Messaging"** tab, ensure the **Cloud Messaging API** is enabled
2. If you see "Cloud Messaging API (Legacy) Disabled":
   - This is fine - the Firebase Admin SDK uses the newer API
3. Note your **Project ID** (shown at the top) - you'll need this for `appsettings.json`

---

## Step 6: Configure Your Project Files

### 6.1 Update Backend Configuration

**File**: `backend/FCMDemo.API/appsettings.json`

```json
{
  "Firebase": {
    "ProjectId": "your-project-id",  // Replace with your Firebase Project ID
    "CredentialsPath": "firebase-adminsdk.json"
  }
}
```

### 6.2 Update Web Client Firebase Config

**File**: `client/web/firebase-config.js`

Replace the placeholder values with your Firebase configuration:

```javascript
const firebaseConfig = {
    apiKey: "YOUR_API_KEY",              // From Step 2
    authDomain: "YOUR_PROJECT.firebaseapp.com",
    projectId: "YOUR_PROJECT_ID",
    storageBucket: "YOUR_PROJECT.appspot.com",
    messagingSenderId: "YOUR_SENDER_ID",
    appId: "YOUR_APP_ID"
};
```

### 6.3 Update Service Worker Firebase Config

**File**: `client/web/firebase-messaging-sw.js`

Update the same configuration (yes, it needs to be in two places):

```javascript
const firebaseConfig = {
    apiKey: "YOUR_API_KEY",
    authDomain: "YOUR_PROJECT.firebaseapp.com",
    projectId: "YOUR_PROJECT_ID",
    storageBucket: "YOUR_PROJECT.appspot.com",
    messagingSenderId: "YOUR_SENDER_ID",
    appId: "YOUR_APP_ID"
};
```

### 6.4 Update VAPID Key

**File**: `client/web/app.js`

Replace the placeholder VAPID key:

```javascript
const VAPID_KEY = 'YOUR_VAPID_KEY_HERE';  // From Step 4
```

---

## Verification Checklist

Before running the app, verify you've completed:

- ✅ Created Firebase project
- ✅ Registered web app and copied configuration
- ✅ Downloaded service account key (`firebase-adminsdk.json`)
- ✅ Placed `firebase-adminsdk.json` in `backend/FCMDemo.API/` folder
- ✅ Added `firebase-adminsdk.json` to `.gitignore`
- ✅ Generated Web Push certificate (VAPID key)
- ✅ Updated `appsettings.json` with Project ID
- ✅ Updated `firebase-config.js` with Firebase config
- ✅ Updated `firebase-messaging-sw.js` with Firebase config
- ✅ Updated `app.js` with VAPID key

---

## Troubleshooting

### "Permission denied" when generating service account key
- **Solution**: Make sure you have Owner or Editor role in the Firebase project

### Can't find "Web Push certificates"
- **Solution**: Make sure you're in the "Cloud Messaging" tab, not "General" tab

### "Firebase project not found"
- **Solution**: Double-check your Project ID in `appsettings.json` matches exactly

### Service worker registration fails
- **Solution**:
  - Ensure you're serving the app over HTTPS or localhost
  - Check browser console for specific errors
  - Verify `firebase-messaging-sw.js` is in the web root directory

---

## Next Steps

Once Firebase is configured, proceed to [SETUP.md](SETUP.md) for running the application.
