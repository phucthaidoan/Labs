# Firebase Cloud Messaging Demo

A complete, production-ready demonstration of Firebase Cloud Messaging (FCM) integration with **ASP.NET Core 9** and a **Web Client**.

## 🎯 Features

- ✅ **ASP.NET Core 9 Web API** with Firebase Admin SDK
- ✅ **Web Client** with Firebase JavaScript SDK
- ✅ **Push Notifications** (foreground and background)
- ✅ **Token Management** (register, update, delete)
- ✅ **Custom Data Payloads** for rich notifications
- ✅ **In-Memory Database** (EF Core) for quick setup
- ✅ **Swagger UI** for API testing
- ✅ **Comprehensive Documentation**

---

## 📋 Prerequisites

- **.NET 9.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Web Server** (Node.js with `http-server`, Python, or VS Code Live Server)
- **Modern Browser** (Chrome, Edge, or Firefox)
- **Firebase Account** - [Get Started](https://console.firebase.google.com/)

---

## 🚀 Quick Start

### 1. Clone and Navigate

```bash
cd c:\Workspaces\Learn\Labs\firebase\cloud-messaging
```

### 2. Configure Firebase

Follow the detailed guide: [docs/FIREBASE_SETUP.md](docs/FIREBASE_SETUP.md)

**Key steps**:
1. Create Firebase project
2. Register web app and copy config
3. Generate service account key (`firebase-adminsdk.json`)
4. Get VAPID key for web push
5. Update configuration files

### 3. Run Backend API

```bash
cd backend/FCMDemo.API
dotnet restore
dotnet run
```

API will be available at: **https://localhost:7001**
Swagger UI at: **https://localhost:7001**

### 4. Run Web Client

```bash
cd client/web
# Using http-server (Node.js)
http-server -p 8080 -c-1

# OR using Python
python -m http.server 8080
```

Web client at: **http://localhost:8080**

### 5. Test the Application

1. Open web client in browser
2. Click "Request Permission" → Allow notifications
3. Click "Get Token" → Copy the FCM token
4. Fill in User ID and Device ID → Click "Register Token"
5. Send a test notification!

---

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [FIREBASE_SETUP.md](docs/FIREBASE_SETUP.md) | Complete Firebase Console setup guide |
| [SETUP.md](docs/SETUP.md) | Detailed project setup and running instructions |
| [API_DOCS.md](docs/API_DOCS.md) | Complete API endpoint reference |
| [REACT_NATIVE_SETUP.md](docs/REACT_NATIVE_SETUP.md) | Future mobile app implementation guide |

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────┐
│   Firebase Cloud Messaging (FCM)        │
└────────────┬───────────────┬────────────┘
             │               │
             │ Push          │ Push
             ↓               ↓
┌─────────────────────┐  ┌──────────────────┐
│  ASP.NET Core 9 API │  │   Web Client     │
│                     │  │                  │
│  - Firebase Admin   │←─┤  - Firebase JS   │
│  - Token Storage    │  │  - Service Worker│
│  - Send API         │  │  - Token Mgmt    │
└─────────────────────┘  └──────────────────┘
```

---

## 📁 Project Structure

```
firebase/cloud-messaging/
├── backend/
│   └── FCMDemo.API/                # ASP.NET Core 9 Web API
│       ├── Controllers/            # API endpoints
│       ├── Services/               # Business logic
│       ├── Data/                   # EF Core entities
│       ├── Models/                 # DTOs
│       ├── Middleware/             # Error handling
│       └── Program.cs              # Application entry point
│
├── client/
│   └── web/                        # Web client
│       ├── index.html              # Main UI
│       ├── app.js                  # Application logic
│       ├── firebase-config.js      # Firebase setup
│       ├── firebase-messaging-sw.js # Service worker
│       ├── api-service.js          # Backend API calls
│       └── styles.css              # Styling
│
├── docs/                           # Documentation
│   ├── FIREBASE_SETUP.md
│   ├── SETUP.md
│   ├── API_DOCS.md
│   └── REACT_NATIVE_SETUP.md
│
├── .gitignore
└── README.md
```

---

## 🔑 API Endpoints

### Device Tokens

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/devicetokens/register` | Register/update device token |
| DELETE | `/api/devicetokens/{deviceId}` | Unregister device token |
| GET | `/api/devicetokens/user/{userId}` | Get user's tokens |

### Notifications

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/notifications/send` | Send simple notification |
| POST | `/api/notifications/send-with-data` | Send notification with data payload |
| POST | `/api/notifications/send-to-user/{userId}` | Send to user by ID |

Full API documentation: [docs/API_DOCS.md](docs/API_DOCS.md)

---

## 🧪 Testing Scenarios

### Scenario 1: Simple Notification
```bash
curl -X POST https://localhost:7001/api/notifications/send \
  -H "Content-Type: application/json" \
  -d '{
    "deviceToken": "YOUR_FCM_TOKEN",
    "title": "Hello World",
    "body": "This is a test notification"
  }'
```

### Scenario 2: Notification with Data
```bash
curl -X POST https://localhost:7001/api/notifications/send-with-data \
  -H "Content-Type: application/json" \
  -d '{
    "deviceToken": "YOUR_FCM_TOKEN",
    "title": "New Message",
    "body": "You have a new message",
    "data": {
      "action": "view",
      "messageId": "123"
    }
  }'
```

### Scenario 3: Background Notification
1. Minimize browser or switch tabs
2. Send notification via Swagger UI
3. Receive OS system notification
4. Click notification to return to app

---

## 🛠️ Technology Stack

### Backend
- ASP.NET Core 9.0
- Firebase Admin SDK v3.0.1
- Entity Framework Core 9.0 (In-Memory)
- Serilog for logging
- Swagger/OpenAPI

### Frontend
- HTML5 + Vanilla JavaScript
- Firebase JS SDK v10.7.1
- Service Worker API
- Fetch API

---

## 🔒 Security

**⚠️ CRITICAL**: Never commit `firebase-adminsdk.json` to version control!

The `.gitignore` file is pre-configured to exclude:
- `firebase-adminsdk.json`
- `google-services.json`
- `GoogleService-Info.plist`

**Production Considerations**:
- Store Firebase credentials in Azure Key Vault or AWS Secrets Manager
- Implement JWT authentication for API endpoints
- Add rate limiting
- Use HTTPS everywhere
- Validate all input data

---

## 🐛 Troubleshooting

### Backend Issues

**Problem**: "Firebase credentials file not found"
```
Solution: Ensure firebase-adminsdk.json is in backend/FCMDemo.API/ folder
```

**Problem**: Port 7001 already in use
```
Solution: Kill the process or change port in launchSettings.json
```

### Web Client Issues

**Problem**: "Permission denied" for notifications
```
Solution: Check browser notification settings (not blocked)
```

**Problem**: Service worker registration fails
```
Solution: Ensure you're serving over HTTPS or localhost
```

**Problem**: CORS errors
```
Solution: Verify your origin is in CORS policy in Program.cs
```

Full troubleshooting guide: [docs/SETUP.md#troubleshooting](docs/SETUP.md#troubleshooting)

---

## 📝 Example Use Cases

- **User Notifications**: Welcome messages, account alerts
- **Order Updates**: Order status, shipping notifications
- **Chat Applications**: New message alerts
- **Real-time Updates**: Breaking news, live scores
- **Reminders**: Appointment reminders, task notifications
- **Marketing**: Promotional offers, announcements

---

## 🎓 Learning Objectives

This demo teaches:
- ✅ Firebase Cloud Messaging integration
- ✅ ASP.NET Core 9 Web API development
- ✅ Entity Framework Core (In-Memory)
- ✅ Service Worker implementation
- ✅ Push notification best practices
- ✅ RESTful API design
- ✅ Error handling and logging

---

## 🚀 Future Enhancements

- [ ] React Native mobile app
- [ ] Batch notifications
- [ ] Notification scheduling
- [ ] User notification preferences
- [ ] Topic-based notifications
- [ ] JWT authentication
- [ ] PostgreSQL database
- [ ] Docker containerization
- [ ] CI/CD pipeline

See [REACT_NATIVE_SETUP.md](docs/REACT_NATIVE_SETUP.md) for mobile implementation guide.

---

## 📖 References

- [Firebase Cloud Messaging Docs](https://firebase.google.com/docs/cloud-messaging)
- [Firebase Admin SDK for .NET](https://firebase.google.com/docs/admin/setup#dotnet)
- [Web Push Notifications](https://developer.mozilla.org/en-US/docs/Web/API/Push_API)
- [Service Workers](https://developer.mozilla.org/en-US/docs/Web/API/Service_Worker_API)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core)

---

## 🤝 Contributing

This is a learning/demo project. Feel free to:
- Report issues
- Suggest improvements
- Fork and customize for your needs

---

## 📄 License

This project is for educational and demonstration purposes.

---

## 💡 Tips

1. **Start with the web client** - No mobile device setup needed
2. **Use Swagger UI** for quick API testing
3. **Check browser console** for detailed error messages
4. **Test background notifications** by minimizing the browser
5. **View service worker** in DevTools → Application → Service Workers

---

## 📧 Support

For issues or questions:
- Check [docs/SETUP.md](docs/SETUP.md) for setup help
- Review [docs/API_DOCS.md](docs/API_DOCS.md) for API reference
- Consult [Firebase Documentation](https://firebase.google.com/docs)

---

**Built with ❤️ using ASP.NET Core 9 and Firebase**

---

## Getting Started Checklist

Before you begin, make sure you have:
- [ ] .NET 9.0 SDK installed
- [ ] Firebase account created
- [ ] Firebase project configured
- [ ] `firebase-adminsdk.json` downloaded
- [ ] Web server ready (http-server or Python)
- [ ] Modern browser installed

**Ready?** Start with [docs/FIREBASE_SETUP.md](docs/FIREBASE_SETUP.md) 🚀
