# 📄 Resume-AI - Real-Time AI-Powered Resume Builder

Resume-AI is a modern, scalable, and intelligent resume building platform built with **Microservices Architecture**, **ASP.NET Core**, and **Event-Driven Design**. It enables users to create professional resumes, manage sections, leverage AI for content generation, and export resumes to high-fidelity PDFs.

---

## 🚀 Tech Stack

| Technology | Purpose |
|-----------|---------|
| **ASP.NET Core Web API** | Backend Microservices |
| **PostgreSQL** | Primary Database (Database per service) |
| **Entity Framework Core** | ORM & Migrations |
| **Redis** | Distributed Caching & AI Quota Tracking |
| **RabbitMQ** | Event-Driven Messaging (Async Jobs & Notifications) |
| **OpenAI / Groq API** | AI Content Generation & ATS Scoring |
| **PuppeteerSharp** | High-Fidelity PDF Rendering |
| **JWT Authentication** | Security & Authorization |
| **Docker & Docker Compose** | Containerization & Orchestration |
| **YARP API Gateway** | API Gateway & Routing |
| **xUnit** | Unit Testing |

---

## 🏗 System Architecture

Resume-AI follows a **Microservices Architecture** with the following principles:

✅ **Microservices Pattern** - Independent, deployable services  
✅ **Event-Driven Architecture** - Asynchronous communication via RabbitMQ  
✅ **Clean Architecture** - Domain-centric design with clear separation of concerns  
✅ **API Gateway Pattern** - Single entry point for all client requests  
✅ **Distributed Caching** - Redis for high-performance AI quota tracking  

---

## 📦 Microservices Overview

| Service | Port | Responsibility |
|---------|------|----------------|
| **AuthService** | 5000 | User authentication, registration, JWT generation, OAuth, Profile management |
| **ResumeService** | 5001 | Managing user resumes (CRUD), tracking target job titles |
| **SectionService** | 5003 | Managing individual resume sections (Experience, Education, Skills) |
| **AiService** | 5004 | Generating summaries, bullet points, ATS scoring, translating via LLMs |
| **ExportService** | 5005 | Rendering resumes to PDF/Word using PuppeteerSharp |
| **TemplateService** | 5006 | Managing HTML/CSS resume templates |
| **JobMatchService** | 5007 | Matching resumes against job descriptions |
| **NotificationService**| 5008 | Handling email and system notifications |
| **GatewayService** | 8080 | YARP API Gateway, request routing, authentication validation |

---

## 🎯 Core Features

### 👤 User Features
✅ User registration with email  
✅ Login with credentials  
✅ JWT-based authentication  
✅ User profile management  
✅ Subscription plan management  

### 📄 Resume Management Features
✅ Create and manage multiple resumes  
✅ Manage individual sections (Experience, Education, Skills, Projects)  
✅ Select custom HTML/CSS templates  
✅ Auto-save capabilities  

### 🤖 AI Integration Features
✅ **Generate Summary**: Create professional summaries based on context.  
✅ **Improve Bullets**: Enhance experience bullet points with action verbs.  
✅ **Tailor Resume**: Adjust resume phrasing to match a specific Job Description.  
✅ **ATS Scoring**: Analyze resume against a Job Description for a match score.  

### 📥 Export Features
✅ High-fidelity PDF rendering (PuppeteerSharp)  
✅ Export job queueing  
✅ Download link generation  

### 🔔 Notification Features
✅ Real-time system notifications  
✅ Email notifications  
✅ Notification history  
✅ Mark as read/unread  

---

## 🏛 Architecture Diagrams

### System Architecture Diagram

```text
┌─────────────────────────────────────────────────────────────────┐
│                         CLIENT LAYER                            │
│                    (React Web Application)                      │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         │ HTTP/REST
                         │
┌────────────────────────▼────────────────────────────────────────┐
│                      API GATEWAY (YARP)                         │
│                         Port: 8080                              │
│  • Request Routing  • Auth Validation  • Load Balancing         │
└──┬────────┬─────────┬─────────┬──────────┬──────────┬───────────┘
   │        │         │         │          │          │           
   │        │         │         │          │          │           
┌──▼────┐ ┌─▼────┐ ┌──▼────┐ ┌──▼────┐ ┌───▼────┐ ┌───▼────┐ ┌────────────┐
│ Auth  │ │Resume│ │Section│ │  AI   │ │ Export │ │Template│ │Notification│
│Service│ │Serv. │ │Serv.  │ │Service│ │Service │ │Service │ │ Service    │
│ :5000 │ │:5001 │ │ :5003 │ │ :5004 │ │ :5005  │ │ :5006  │ │   :5008    │
└───┬───┘ └───┬──┘ └──┬────┘ └───┬───┘ └───┬────┘ └───┬────┘ └─────┬──────┘
    │         │       │          │         │          │            │
    └─────────┴───────┴──────────┴─────────┴──────────┴────────────┘
                         │
        ┌────────────────┼────────────────┬─────────────┐
        │                │                │             │
   ┌────▼─────┐   ┌──────▼──────┐  ┌─────▼──────┐ ┌─────▼──────┐
   │PostgreSQL│   │    Redis    │  │  RabbitMQ  │ │External API│
   │  :5432   │   │    :6379    │  │  :5672     │ │Groq/OpenAI │
   │          │   │             │  │            │ │            │
   │ Multiple │   │ Quota Cache │  │   Events   │ │ AI Models  │
   │   DBs    │   │             │  │            │ │            │
   └──────────┘   └─────────────┘  └────────────┘ └────────────┘
```

### Microservice Communication Flow

```text
┌──────────┐                    ┌──────────┐
│  Client  │                    │ Gateway  │
└─────┬────┘                    └────┬─────┘
      │                              │
      │  1. Login Request            │
      ├─────────────────────────────►│
      │                              │
      │                         ┌────▼─────┐
      │                         │   Auth   │
      │                         │ Service  │
      │                         └────┬─────┘
      │                              │
      │  2. JWT Token                │
      │◄─────────────────────────────┤
      │                              │
      │  3. Request PDF Export       │
      ├─────────────────────────────►│
      │                              │
      │                         ┌────▼─────┐
      │                         │  Export  │
      │                         │ Service  │
      │                         └────┬─────┘
      │                              │
      │                              │ 4. Publish Event
      │                              │
      │                         ┌────▼─────┐
      │                         │ RabbitMQ │
      │                         └────┬─────┘
      │                              │
      │                              │ 5. Consume Event
      │                              │
      │                    ┌─────────┴──────────┐
      │                    │                    │
      │               ┌────▼─────┐      ┌──────▼────┐
      │               │  In-App  │      │   Email    │
      │               │  Alerts  │      │  Service   │
      │               └────┬─────┘      └──────┬─────┘
      │                    │                   │
      │  6. PDF Ready UI   │                   │ 7. Send Email
      │◄───────────────────┤                   │
      │                    │                   │
      │                    └───────────────────┘
```

---

### Message Flow Sequence Diagram

```text
User       Gateway    Auth       Resume     Section    Template   JobMatch   Export     RabbitMQ   Notify
 │          │          │          │          │          │          │          │          │          │
 │ Login    │          │          │          │          │          │          │          │          │
 ├─────────►│          │          │          │          │          │          │          │          │
 │          │ Validate │          │          │          │          │          │          │          │
 │          ├─────────►│          │          │          │          │          │          │          │
 │          │ Token    │          │          │          │          │          │          │          │
 │◄─────────┼──────────┤          │          │          │          │          │          │          │
 │          │          │          │          │          │          │          │          │          │
 │ Create   │          │          │          │          │          │          │          │          │
 ├─────────►│          │          │          │          │          │          │          │          │
 │          │ Forward  │          │          │          │          │          │          │          │
 │          ├────────────────────►│          │          │          │          │          │          │
 │          │          │          │ Save DB  │          │          │          │          │          │
 │          │          │          │ ┌──────┐ │          │          │          │          │          │
 │          │          │          │ │ PgSQL│ │          │          │          │          │          │
 │          │          │          │ └──────┘ │          │          │          │          │          │
 │          │ Success  │          │          │          │          │          │          │          │
 │◄─────────┼─────────────────────┤          │          │          │          │          │          │
 │          │          │          │          │          │          │          │          │          │
 │ Add Sect │          │          │          │          │          │          │          │          │
 ├─────────►│          │          │          │          │          │          │          │          │
 │          │ Forward  │          │          │          │          │          │          │          │
 │          ├───────────────────────────────►│          │          │          │          │          │
 │          │ Success                        │          │          │          │          │          │
 │◄─────────┼────────────────────────────────┤          │          │          │          │          │
 │          │          │          │          │          │          │          │          │          │
 │ Get Tmpl │          │          │          │          │          │          │          │          │
 ├─────────►│          │          │          │          │          │          │          │          │
 │          │ Forward                        │          │          │          │          │          │
 │          ├──────────────────────────────────────────►│          │          │          │          │
 │          │ HTML/CSS                                  │          │          │          │          │
 │◄─────────┼───────────────────────────────────────────┤          │          │          │          │
 │          │          │          │          │          │          │          │          │          │
 │ Match Job│          │          │          │          │          │          │          │          │
 ├─────────►│          │          │          │          │          │          │          │          │
 │          │ Forward                                   │          │          │          │          │
 │          ├─────────────────────────────────────────────────────►│          │          │          │
 │          │ Score                                                │          │          │          │
 │◄─────────┼──────────────────────────────────────────────────────┤          │          │          │
 │          │          │          │          │          │          │          │          │          │
 │ Export   │          │          │          │          │          │          │          │          │
 ├─────────►│          │          │          │          │          │          │          │          │
 │          │ Forward                                              │          │          │          │
 │          ├────────────────────────────────────────────────────────────────►│          │          │
 │          │ Queued                                                          │          │          │
 │◄─────────┼─────────────────────────────────────────────────────────────────┤          │          │
 │          │          │          │          │          │          │          │ Generate │          │
 │          │          │          │          │          │          │          │ ┌──────┐ │          │
 │          │          │          │          │          │          │          │ │ PDF  │ │          │
 │          │          │          │          │          │          │          │ └──────┘ │          │
 │          │          │          │          │          │          │          │ Publish  │          │
 │          │          │          │          │          │          │          ├─────────►│          │
 │          │          │          │          │          │          │          │          │ Consume  │
 │          │          │          │          │          │          │          │◄─────────┤          │
 │          │          │          │          │          │          │          │          │ Alert    │
 │          │          │          │          │          │          │          │          ├─────────►│
 │          │          │          │          │          │          │          │          │          │
```

---

### Entity Relationship Diagram

```text
┌─────────────────────┐
│       User          │
├─────────────────────┤
│ PK  UserId          │
│     FullName        │
│     Email           │
│     PasswordHash    │
│     SubscriptionPlan│
│     CreatedAt       │
└──────────┬──────────┘
           │
           │ 1:N
           │
┌──────────▼──────────┐          ┌─────────────────────┐
│       Resume        │          │      Section        │
├─────────────────────┤          ├─────────────────────┤
│ PK  ResumeId        │          │ PK  SectionId       │
│ FK  UserId          │          │ FK  ResumeId        │
│     Title           │◄────────┤│     SectionType     │
│     TargetJobTitle  │    1:N   │     Content         │
│     AtsScore        │          │     DisplayOrder    │
│     CreatedAt       │          │     CreatedAt       │
└──────────┬──────────┘          └─────────────────────┘
           │
           │ 1:N
           │
┌──────────▼──────────┐          ┌─────────────────────┐
│     AiRequest       │          │     ExportJob       │
├─────────────────────┤          ├─────────────────────┤
│ PK  RequestId       │          │ PK  JobId           │
│ FK  UserId          │          │ FK  UserId          │
│ FK  ResumeId        │          │ FK  ResumeId        │
│     RequestType     │          │     Format          │
│     Prompt          │          │     Status          │
│     AiResponse      │          │     DownloadUrl     │
│     TokensUsed      │          │     CreatedAt       │
└─────────────────────┘          └─────────────────────┘
```

---

### Clean Architecture Layers (Per Microservice)

```text
┌──────────────────────────────────────────────────────────────┐
│                        API LAYER                             │
│  • Controllers         • Middleware       • Program.cs       │
│  • Authentication      • Error Handling   • Dependency Setup │
└─────────────────────────┬────────────────────────────────────┘
                          │
┌─────────────────────────▼────────────────────────────────────┐
│                   APPLICATION LAYER                          │
│  • DTOs               • Validation        • Mapping          │
│  • Interfaces         • Business Logic    • Service Layer    │
└─────────────────────────┬────────────────────────────────────┘
                          │
┌─────────────────────────▼────────────────────────────────────┐
│                     DOMAIN LAYER                             │
│  • Entities           • Enums             • Constants        │
│  • Domain Models      • Business Rules                       │
└─────────────────────────┬────────────────────────────────────┘
                          │
┌─────────────────────────▼────────────────────────────────────┐
│                 INFRASTRUCTURE LAYER                         │
│  • DbContext          • Repositories      • External APIs    │
│  • Migrations         • Caching           • Message Queues   │
└──────────────────────────────────────────────────────────────┘
```

---

## 📂 Project Structure

```text
ResumeAI/
│
├── src/
│   ├── ResumeAI.Auth.API/         # Authentication & User Management
│   ├── ResumeAI.Resume.API/       # Resumes CRUD
│   ├── ResumeAI.Section.API/      # Sections CRUD
│   ├── ResumeAI.AI.API/           # OpenAI/Groq Integration
│   ├── ResumeAI.Export.API/       # PDF/Word Generation (PuppeteerSharp)
│   ├── ResumeAI.Template.API/     # HTML/CSS Templates
│   ├── ResumeAI.JobMatch.API/     # Resume Scoring
│   ├── ResumeAI.Notification.API/ # Email & Push Notifications
│   ├── ResumeAI.Gateway/          # YARP Gateway
│   └── ResumeAI.Shared/           # Common Enums, DTOs, Constants
│
├── tests/
│   ├── ResumeAI.Auth.Tests/
│   ├── ResumeAI.Resume.Tests/
│   └── ...
│
└── docker-compose.yml
```

---

## 🔄 Complete Workflow

### 1️⃣ User Registration & Authentication Flow

```text
User Registration
      ↓
Validate Input
      ↓
Hash Password (PBKDF2/BCrypt)
      ↓
Save to PostgreSQL
      ↓
Generate JWT Token
      ↓
Return Token to Client
      ↓
Client Stores Token
```

### 2️⃣ Generating an AI Summary

```text
User Requests Summary
        ↓
Gateway Validates JWT
        ↓
AiService Receives Request
        ↓
Check Redis for User Quota limits
        ↓
Call ResumeService (Internal HTTP) for Resume Context
        ↓
Send Constructed Prompt to OpenAI / Groq
        ↓
Receive AI Output
        ↓
Save Request to PostgreSQL
        ↓
Update Redis Quota Counter
        ↓
Publish AiGenerationCompleted Event to RabbitMQ
        ↓
NotificationService Sends Notification
        ↓
Return Summary to User
```

### 3️⃣ Exporting Resume to PDF

```text
User Requests PDF Export
        ↓
Gateway Validates JWT
        ↓
ExportService Creates ExportJob (Status: QUEUED)
        ↓
Return JobID to Client (Async processing)
        ↓
ExportService fetches full Resume, Sections, Template
        ↓
PuppeteerSharp Renders HTML/CSS to PDF
        ↓
Save PDF to temporary storage / cloud
        ↓
Update ExportJob (Status: COMPLETED)
        ↓
Publish ExportCompleted Event to RabbitMQ
        ↓
NotificationService Sends Notification
```

---

## 🌐 API Endpoints

### 🔑 Authentication Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Register new user |
| POST | `/api/auth/login` | Login with credentials |
| GET | `/api/auth/me` | Get current user profile |
| PUT | `/api/auth/profile` | Update user profile |

### 📄 Resume Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/resumes` | Create a new resume |
| GET | `/api/resumes` | Get all user resumes |
| GET | `/api/resumes/{id}` | Get full resume details |
| PUT | `/api/resumes/{id}` | Update resume details |
| DELETE | `/api/resumes/{id}` | Delete resume |

### 🤖 AI Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/ai/summary` | Generate a professional summary |
| POST | `/api/ai/bullets` | Improve bullet points |
| POST | `/api/ai/ats-score` | Calculate ATS match score |
| GET | `/api/ai/quota` | Get remaining AI usage quota |

### 📥 Export Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/export/pdf` | Request PDF generation |
| GET | `/api/export/{jobId}` | Check export job status |
| GET | `/api/export/download/{jobId}` | Download completed file |

---

## 🔧 Infrastructure Components

### PostgreSQL Databases

Each microservice has its own isolated database:

- `AuthDb` - User accounts and tokens
- `ResumeDb` - Resumes and Sections
- `AiDb` - AI request history and logging
- `ExportDb` - Export jobs and statuses
- `NotificationDb` - Notification history

### Redis Cache

Used for:
- Tracking Monthly AI usage quotas per user
- Caching template HTML/CSS data for faster rendering
- Rate limiting

### RabbitMQ Message Queue

Event-driven communication for:
- Export completion notifications
- System events (User Registered, Quota Exceeded)

### API Gateway (YARP)

Responsibilities:
- Request routing to microservices
- JWT token validation
- Load balancing
- Rate limiting

---

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Docker & Docker Compose
- PostgreSQL 15+
- Redis 7
- RabbitMQ 3.13

### Environment Variables

Create a `.env` file in the root directory:

```env
# PostgreSQL
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_secure_password

# JWT
JWT_SECRET=your_jwt_secret_key_minimum_32_characters

# RabbitMQ
RABBITMQ_DEFAULT_USER=admin
RABBITMQ_DEFAULT_PASS=your_rabbitmq_password

# AI Configuration
OPENAI_API_KEY=your_groq_or_openai_api_key
```

### Installation & Setup

#### Option 1: Using Docker Compose (Recommended)

```bash
# Clone the repository
git clone https://github.com/yourusername/ResumeAI.git
cd ResumeAI

# Create .env file with your configurations
cp .env.example .env

# Build and start all services
docker-compose up --build

# Access the API Gateway
# http://localhost:8080
```

#### Option 2: Manual Setup

```bash
# Install dependencies for each service
cd src/ResumeAI.Auth.API
dotnet restore

# Run database migrations
dotnet ef database update

# Run each service in separate terminals
dotnet run --project src/ResumeAI.Auth.API
dotnet run --project src/ResumeAI.Resume.API
dotnet run --project src/ResumeAI.AI.API
dotnet run --project src/ResumeAI.Export.API
dotnet run --project src/ResumeAI.Gateway
# ... repeat for other services
```

---

## 🧪 Testing

```bash
# Run all tests
cd src/unittesting
dotnet test

# Run specific test project
dotnet test tests/ResumeAI.Auth.Tests
```

---

## 📊 Database Schema (Sample)

### User Table (AuthService)

```sql
CREATE TABLE "Users" (
    "UserId" integer GENERATED BY DEFAULT AS IDENTITY,
    "FullName" character varying(100) NOT NULL,
    "Email" character varying(256) NOT NULL,
    "PasswordHash" text NOT NULL,
    "Phone" text,
    "Role" integer NOT NULL,
    "SubscriptionPlan" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("UserId")
);
```

### Resume Table (ResumeService)

```sql
CREATE TABLE "Resumes" (
    "ResumeId" integer GENERATED BY DEFAULT AS IDENTITY,
    "UserId" integer NOT NULL,
    "TemplateId" integer,
    "Title" character varying(100) NOT NULL,
    "TargetJobTitle" character varying(100) NOT NULL,
    "AtsScore" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Resumes" PRIMARY KEY ("ResumeId")
);
```

---

## 🔐 Security Features

### Authentication & Authorization

- **JWT Tokens**: Secure stateless authentication
- **Password Hashing**: ASP.NET Core Identity PasswordHasher
- **Token Expiration**: Configurable token lifetime
- **Role & Plan Based Access**: Premium-only endpoints

### API Security

- **Internal Security**: Microservices use internal keys or forwarded JWTs to verify inter-service requests, preventing direct external access.
- **CORS**: Cross-Origin Resource Sharing policies
- **Rate Limiting**: Prevent abuse via YARP
- **Input Validation**: `HtmlSanitizer` is used to prevent XSS attacks when users input rich text into resume sections.
- **SQL Injection Protection**: Parameterized queries via EF Core

---

## 📈 Performance Optimizations

### Caching Strategy

```text
Redis Cache Layers:
├── AI Quotas (TTL: Start of next month)
├── Template Data (TTL: 24 hours)
└── Configuration Settings (TTL: 1 hour)
```

### Async Operations

- All I/O operations are asynchronous
- Event-driven architecture reduces blocking (Exporting PDFs happens entirely in the background)
- Optimized database queries with eager loading where appropriate

---

## 🔄 Event-Driven Architecture

### Published Events

| Event | Publisher | Consumers | Description |
|-------|-----------|-----------|-------------|
| `ExportCompleted` | ExportService | NotificationService | PDF/Word generation finished |
| `AiGenerationCompleted`| AiService | NotificationService | AI text generation finished |
| `UserRegistered` | AuthService | NotificationService | New user signup |

### Event Format (JSON)

```json
{
  "eventId": "uuid",
  "eventType": "ExportCompleted",
  "timestamp": "2026-05-18T10:30:00Z",
  "payload": {
    "jobId": "uuid",
    "userId": 123,
    "resumeId": 456,
    "downloadUrl": "https://...",
    "completedAt": "2026-05-18T10:30:00Z"
  }
}
```

---

## 📱 API Documentation

### Swagger UI

Access interactive API documentation when running in Development mode:

```text
AuthService:         http://localhost:5000/swagger
ResumeService:       http://localhost:5001/swagger
AiService:           http://localhost:5004/swagger
ExportService:       http://localhost:5005/swagger
API Gateway:         http://localhost:8080/swagger
```

---

## 🐳 Docker Configuration

### Service Ports

| Service | Internal Port | External Port |
|---------|--------------|---------------|
| PostgreSQL | 5432 | 5432 |
| Redis | 6379 | 6379 |
| RabbitMQ | 5672 | 5672 |
| RabbitMQ Management | 15672 | 15672 |
| AuthService | 8080 | 5000 |
| ResumeService | 8080 | 5001 |
| SectionService | 8080 | 5003 |
| AiService | 8080 | 5004 |
| ExportService | 8080 | 5005 |
| Gateway | 8080 | 8080 |

---

## 🔧 Configuration

### JWT Settings

```json
{
  "Jwt": {
    "Secret": "your-secret-key-minimum-32-characters",
    "Issuer": "ResumeAI",
    "Audience": "ResumeAIUsers",
    "ExpiryMinutes": 120
  }
}
```

### Database Connection Strings

```json
{
  "ConnectionStrings": {
    "AuthDb": "Host=localhost;Port=5432;Database=ResumeAIAuthDb;Username=postgres;Password=your_password"
  }
}
```

---

## 🙏 Acknowledgments

- ASP.NET Core Team for the excellent framework
- OpenAI & Groq for LLM capabilities
- PuppeteerSharp for HTML to PDF rendering
- RabbitMQ for reliable messaging
- PostgreSQL for robust data storage

---

## 🗺 Roadmap

### Phase 1 (Completed)
- [x] User authentication and authorization
- [x] Resume and Section CRUD operations
- [x] AI integration for Summaries and Bullets
- [x] High-fidelity PDF exports
- [x] Microservices architecture implementation

### Phase 2 (Planned)
- [ ] Multi-language translation
- [ ] Cover letter generation
- [ ] LinkedIn profile import
- [ ] Direct job board integrations
- [ ] Analytics dashboard for application tracking
