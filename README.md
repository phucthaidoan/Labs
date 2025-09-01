# Labs Learning Workspace

A comprehensive collection of learning projects and solutions covering various technologies and architectural patterns.

## 📚 Available Solutions

### 🔐 Audit Logging & Compliance System
A comprehensive, enterprise-grade GDPR-compliant audit logging solution built with .NET 8.0.

**Location**: `solution/audit/audit-01/`  
**Documentation**: See [AuditLogging Solution README](solution/audit/audit-01/README.md) for complete details.

**Key Features**:
- GDPR compliance with data protection and pseudonymization
- Dual storage strategy (operational + archival)
- Comprehensive REST API with export functionality
- Automated archival and retention policies
- Role-based access control and security

### 🚀 Azure Service Bus Examples
Various Azure Service Bus implementations demonstrating different messaging patterns.

**Location**: `azure-service-bus/` and `AzureServiceBus/`

**Projects Include**:
- Basic queue implementations
- Topic and subscription patterns
- Session-enabled queues
- FIFO (First-In-First-Out) queues
- Message processing and handling

### ⚡ Azure Durable Functions
Serverless workflow orchestration examples using Azure Durable Functions.

**Location**: `solution/azure-durable-function-01/`

**Features**:
- Workflow orchestration patterns
- State management and persistence
- Error handling and retry logic
- Monitoring and diagnostics

## 🏗️ Architecture Overview

This workspace demonstrates various architectural patterns and technologies:

```
Labs/
├── solution/audit/audit-01/          # Audit logging solution
├── azure-service-bus/                # Service Bus examples
│   ├── Queue4.Session/              # Session-enabled queues
│   ├── Queue4.Session.FIFO/         # FIFO session queues
│   └── Topic2.Processor/            # Topic processing patterns
├── AzureServiceBus/                  # Additional Service Bus patterns
│   ├── ServiceBusQueue1/            # Basic queue implementation
│   ├── ServiceBusQueue2/            # Advanced queue patterns
│   ├── ServiceBusQueue3/            # Sender/Receiver pattern
│   └── ServiceBusTopic1/            # Topic and subscription
└── solution/azure-durable-function-01/ # Durable Functions examples
```

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code
- Azure Storage Emulator (for local development)
- SQL Server LocalDB or PostgreSQL (for audit solution)

### Building Solutions

Each solution can be built independently:

#### Audit Logging Solution
```bash
cd solution/audit/audit-01
dotnet build AuditLogging.sln
```

#### Azure Service Bus Examples

**Queue4.Session (Session-enabled queues):**
```bash
cd azure-service-bus/Queue4.Session
dotnet build Queue4.Session.sln
```

**Queue4.Session.FIFO (FIFO session queues):**
```bash
cd azure-service-bus/Queue4.Session.FIFO
dotnet build Queue4.Session.FIFO.sln
```

**Topic2.Processor (Topic processing):**
```bash
cd azure-service-bus/Topic2.Processor
dotnet build Topic2.Processor.sln
```

**ServiceBusQueue1 (Basic queue):**
```bash
cd AzureServiceBus/ServiceBusQueue1
dotnet build ServiceBusQueue1.sln
```

**ServiceBusQueue2 (Advanced queue):**
```bash
cd AzureServiceBus/ServiceBusQueue2
dotnet build ServiceBusQueue2.sln
```

**ServiceBusQueue3 (Sender/Receiver):**
```bash
cd AzureServiceBus/ServiceBusQueue3
dotnet build ServiceBusQueue3.sln
```

**ServiceBusTopic1 (Topic and subscription):**
```bash
cd AzureServiceBus/ServiceBusTopic1
dotnet build ServiceBusTopic1.sln
```

#### Durable Functions
```bash
cd solution/azure-durable-function-01/DurableFunctionSolution1
dotnet build
```

## 📖 Learning Paths

### 1. **Audit Logging & Compliance**
- Start with the Core project to understand domain models
- Explore Infrastructure for data access patterns
- Review Services for business logic implementation
- Test the API endpoints for hands-on experience
- Understand background processing with the Archiver

### 2. **Azure Service Bus Messaging**
- Begin with basic queue implementations
- Progress to topics and subscriptions
- Explore session-enabled queues for ordered processing
- Understand FIFO patterns for strict ordering

### 3. **Azure Durable Functions**
- Learn workflow orchestration patterns
- Understand state management in serverless environments
- Explore error handling and retry mechanisms

## 🔧 Development Environment

### Recommended Tools
- **IDE**: Visual Studio 2022 or VS Code with C# extension
- **Database**: SQL Server LocalDB or PostgreSQL
- **Storage**: Azure Storage Emulator
- **Version Control**: Git

### Configuration
Each solution has its own configuration files:
- `appsettings.json` for application settings
- `appsettings.Development.json` for development overrides
- Environment-specific configurations as needed

## 📚 Documentation

- **Audit Solution**: Comprehensive documentation in `solution/audit/audit-01/README.md`
- **API Documentation**: Swagger UI available when running the audit API
- **Code Documentation**: XML documentation in source code
- **Configuration**: Detailed configuration examples in each project

## 🤝 Contributing

1. Explore the existing solutions to understand patterns
2. Create new learning projects following established conventions
3. Maintain consistent project structure and documentation
4. Add comprehensive README files for new solutions
5. Follow .NET coding standards and best practices

## 📄 License

This workspace is part of the Labs learning environment.

---

**Built for learning and exploration of modern .NET technologies and cloud patterns**
