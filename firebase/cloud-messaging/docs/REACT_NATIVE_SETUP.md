# React Native Setup Guide (Future Implementation)

This guide provides information for implementing the React Native mobile app version of the FCM Demo.

> **Note**: This is a reference guide for future implementation. The current demo uses a web client for testing without physical devices.

---

## Overview

The React Native app would provide the same FCM functionality on iOS and Android devices:
- Request notification permissions
- Register FCM tokens
- Receive foreground and background notifications
- Handle notification tap events
- Navigate based on notification data

---

## Prerequisites

### Development Environment

- **Node.js** 20.x LTS
- **React Native CLI** (not Expo, for native module support)
- **iOS Development** (macOS only):
  - Xcode 15+
  - CocoaPods
  - iOS Simulator or physical device
- **Android Development**:
  - Android Studio 2024.x
  - Android SDK 34
  - Java Development Kit (JDK) 17
  - Android Emulator or physical device

### Firebase Setup

Complete all steps in [FIREBASE_SETUP.md](FIREBASE_SETUP.md), plus:
- Add iOS app in Firebase Console
- Add Android app in Firebase Console
- Download `google-services.json` (Android)
- Download `GoogleService-Info.plist` (iOS)

---

## Project Initialization

### Create React Native Project

```bash
cd c:\Workspaces\Learn\Labs\firebase\cloud-messaging
npx react-native@latest init FCMDemoApp --template react-native-template-typescript
cd FCMDemoApp
```

### Install Dependencies

```bash
# Firebase packages
npm install @react-native-firebase/app @react-native-firebase/messaging

# Navigation
npm install @react-navigation/native @react-navigation/native-stack
npm install react-native-screens react-native-safe-area-context

# HTTP client
npm install axios

# Storage
npm install @react-native-async-storage/async-storage

# Development dependencies
npm install --save-dev @types/react @types/react-native
```

---

## Firebase Configuration

### Android Setup

1. **Add Firebase Config**:
   - Place `google-services.json` in `android/app/`

2. **Update build.gradle** (project level):
```gradle
// android/build.gradle
buildscript {
    dependencies {
        classpath 'com.google.gms:google-services:4.4.0'
    }
}
```

3. **Update build.gradle** (app level):
```gradle
// android/app/build.gradle
apply plugin: 'com.google.gms.google-services'

dependencies {
    implementation platform('com.google.firebase:firebase-bom:32.7.0')
    implementation 'com.google.firebase:firebase-messaging'
}
```

4. **Update AndroidManifest.xml**:
```xml
<manifest>
    <uses-permission android:name="android.permission.POST_NOTIFICATIONS" />

    <application>
        <!-- ... -->
        <service
            android:name=".FCMService"
            android:exported="false">
            <intent-filter>
                <action android:name="com.google.firebase.MESSAGING_EVENT" />
            </intent-filter>
        </service>
    </application>
</manifest>
```

### iOS Setup

1. **Add Firebase Config**:
   - Drag `GoogleService-Info.plist` into Xcode project
   - Ensure it's added to target

2. **Update Podfile**:
```ruby
# ios/Podfile
$RNFirebaseAsStaticFramework = true

target 'FCMDemoApp' do
  # ... existing pods
  pod 'Firebase/Messaging'
end
```

3. **Install Pods**:
```bash
cd ios
pod install
cd ..
```

4. **Update Info.plist**:
```xml
<key>UIBackgroundModes</key>
<array>
  <string>remote-notification</string>
</array>
```

5. **Update AppDelegate.mm**:
```objc
#import <Firebase.h>
#import <UserNotifications/UserNotifications.h>

@implementation AppDelegate

- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)launchOptions
{
  [FIRApp configure];
  UNUserNotificationCenter *center = [UNUserNotificationCenter currentNotificationCenter];
  center.delegate = self;
  return [super application:application didFinishLaunchingWithOptions:launchOptions];
}

// ... notification delegates

@end
```

---

## Key Implementation Files

### FCM Service

**File**: `src/services/fcmService.ts`

```typescript
import messaging from '@react-native-firebase/messaging';
import { Platform } from 'react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { apiService } from './apiService';

class FCMService {
  async requestPermission(): Promise<boolean> {
    const authStatus = await messaging().requestPermission();
    return authStatus === messaging.AuthorizationStatus.AUTHORIZED ||
           authStatus === messaging.AuthorizationStatus.PROVISIONAL;
  }

  async getToken(): Promise<string | null> {
    return await messaging().getToken();
  }

  async registerToken(userId: string, deviceId: string): Promise<boolean> {
    const token = await this.getToken();
    if (!token) return false;

    await apiService.registerDeviceToken({
      userId,
      token,
      deviceId,
      platform: Platform.OS === 'ios' ? 'iOS' : 'Android',
    });

    return true;
  }

  onMessage(callback: (message: any) => void) {
    return messaging().onMessage(callback);
  }

  setBackgroundMessageHandler(handler: (message: any) => Promise<void>) {
    messaging().setBackgroundMessageHandler(handler);
  }
}

export const fcmService = new FCMService();
```

### API Service

**File**: `src/services/apiService.ts`

```typescript
import axios from 'axios';

const API_BASE_URL = __DEV__
  ? 'https://localhost:7001/api'
  : 'https://your-production-api.com/api';

export const apiService = {
  async registerDeviceToken(data: {
    userId: string;
    token: string;
    deviceId: string;
    platform: string;
  }) {
    const response = await axios.post(
      `${API_BASE_URL}/devicetokens/register`,
      data
    );
    return response.data;
  },

  async sendNotification(data: {
    deviceToken: string;
    title: string;
    body: string;
    data?: object;
  }) {
    const response = await axios.post(
      `${API_BASE_URL}/notifications/send`,
      data
    );
    return response.data;
  },
};
```

### Main App

**File**: `src/App.tsx`

```typescript
import React, { useEffect } from 'react';
import messaging from '@react-native-firebase/messaging';
import { NavigationContainer } from '@react-navigation/native';
import { fcmService } from './services/fcmService';

// Background message handler
messaging().setBackgroundMessageHandler(async (remoteMessage) => {
  console.log('Background message:', remoteMessage);
});

function App(): React.JSX.Element {
  useEffect(() => {
    setupNotifications();
  }, []);

  const setupNotifications = async () => {
    const hasPermission = await fcmService.requestPermission();
    if (!hasPermission) {
      console.log('Notification permission denied');
      return;
    }

    const token = await fcmService.getToken();
    console.log('FCM Token:', token);

    // Register token with backend
    // await fcmService.registerToken('user-123', 'device-456');

    // Handle foreground messages
    fcmService.onMessage(async (remoteMessage) => {
      console.log('Foreground message:', remoteMessage);
    });
  };

  return (
    <NavigationContainer>
      {/* Your navigation setup */}
    </NavigationContainer>
  );
}

export default App;
```

---

## Running the App

### iOS

```bash
npm run ios
# or
npx react-native run-ios
```

**Note**: iOS push notifications **do not work in the simulator**. You must use a physical device.

### Android

```bash
npm run android
# or
npx react-native run-android
```

Android push notifications work in both emulators and physical devices.

---

## Testing Notifications

### Test on Physical iOS Device

1. Connect iPhone via USB
2. Open Xcode → Select your device
3. Run the app from Xcode
4. Get FCM token from app logs
5. Send notification via Swagger UI
6. Test foreground and background scenarios

### Test on Android Emulator/Device

1. Start Android emulator or connect device
2. Run `npm run android`
3. Get FCM token from Metro logs
4. Send notification via Swagger UI
5. Test foreground and background scenarios

---

## Common Issues & Solutions

### iOS: "Error: No APNs token"
**Solution**:
- Ensure you're testing on a physical device
- Check that Push Notifications capability is enabled in Xcode
- Verify APNs certificate is uploaded in Firebase Console

### Android: Notifications not appearing
**Solution**:
- Check notification permission is granted (Android 13+)
- Verify `google-services.json` is in `android/app/`
- Ensure notification channel is created

### Token Refresh
**Solution**:
```typescript
messaging().onTokenRefresh(async (newToken) => {
  console.log('Token refreshed:', newToken);
  // Re-register with backend
  await apiService.registerDeviceToken({ ... });
});
```

---

## Reference Existing Implementation

Check the **`firebase-sample` branch** in this repository:
- Path: `firebase/dotnet-firebase-getting-started/aspnetcore-firebase-sample/`
- Contains working Firebase integration examples
- Service worker implementation patterns
- Notification handling logic

---

## Additional Resources

- [React Native Firebase Docs](https://rnfirebase.io/)
- [@react-native-firebase/messaging](https://rnfirebase.io/messaging/usage)
- [Firebase iOS Setup](https://firebase.google.com/docs/ios/setup)
- [Firebase Android Setup](https://firebase.google.com/docs/android/setup)
- [React Navigation Docs](https://reactnavigation.org/)

---

## Future Enhancements

When implementing the React Native app, consider:
- Push notification badges
- Notification categories (iOS)
- Notification channels (Android)
- Rich notifications with images
- Action buttons on notifications
- Background data sync triggered by notifications
- Silent notifications
- Notification scheduling
- Deep linking based on notification data

---

## Conclusion

This guide provides the foundation for implementing a React Native version of the FCM Demo. The current web client demonstrates all core FCM concepts that translate directly to mobile development.

For questions or issues, refer to:
- [Firebase Support](https://firebase.google.com/support)
- [React Native Firebase GitHub](https://github.com/invertase/react-native-firebase)
