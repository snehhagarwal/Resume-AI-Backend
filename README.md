# 📄 Resume-AI - Real-Time AI-Powered Resume Builder

Resume-AI is a modern, scalable, and intelligent resume building platform built with a **Microservices Architecture**, **ASP.NET Core**, **Entity Framework Core**, and **AI Integrations**. It enables users to create professional resumes, manage sections, leverage AI for content generation, and export resumes to high-fidelity PDFs.

---

## 🚀 Tech Stack

| Technology | Purpose |
| :--- | :--- |
| **ASP.NET Core 8 Web API** | Backend Microservices |
| **PostgreSQL** | Primary Database (Database per service) |
| **Entity Framework Core** | ORM & Migrations |
| **Redis** | Distributed Caching & AI Quota Tracking |
| **RabbitMQ** | Event-Driven Messaging (Notifications, Async Jobs) |
| **OpenAI / Groq API** | AI Content Generation & ATS Scoring |
| **PuppeteerSharp** | High-Fidelity PDF Rendering |
| **JWT Authentication** | Security & Authorization |
| **Docker & Docker Compose** | Containerization & Orchestration |
| **YARP** | API Gateway & Routing |

---

## 🏗 System Architecture

Resume-AI follows a Microservices Architecture with the following principles:

✅ **Microservices Pattern** - Independent, deployable services  
✅ **Event-Driven Architecture** - Asynchronous communication via RabbitMQ  
✅ **API Gateway Pattern** - Single entry point (YARP) for all client requests  
✅ **Distributed Caching** - Redis for high-performance AI quota tracking  
✅ **Clean Architecture** - Domain-centric design with clear separation of concerns  

### 📦 Microservices Overview

| Service | Port | Responsibility |
| :--- | :--- | :--- |
| **AuthService** | `5000` | User registration, JWT generation, OAuth, Profile management. |
| **ResumeService** | `5001` | Managing user resumes (CRUD), tracking target job titles. |
| **SectionService** | `5003` | Managing individual resume sections (Experience, Education, Skills). |
| **AiService** | `5004` | Generating summaries, bullet points, ATS scoring, translating via LLMs. |
| **ExportService** | `5005` | Rendering resumes to PDF/Word using PuppeteerSharp. |
| **TemplateService** | `5006` | Managing HTML/CSS resume templates. |
| **JobMatchService** | `5007` | Matching resumes against job descriptions. |
| **NotificationService**| `5008` | Handling email and in-app notifications. |
| **Gateway (YARP)** | `8080` | API Gateway, routing, authentication validation. |

---

## 🎯 Core Features

### 👤 User Features
✅ User registration with email & password
✅ JWT-based authentication
✅ User profile & subscription management
✅ Premium quota enforcement

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

---

## 🔧 Infrastructure Components

### PostgreSQL Databases
Each microservice has its own isolated database schema:
*   `AuthDb` - User accounts and tokens
*   `ResumeDb` - Resumes and Sections
*   `AiDb` - AI request history and logging
*   `ExportDb` - Export job status

### Redis Cache
Used for:
*   Tracking Monthly AI usage quotas per user
*   Caching template data for faster rendering

### RabbitMQ Message Queue
Event-driven communication for:
*   Sending async export completion notifications
*   System events (User Registered, Quota Exceeded)

---

## 🔐 Security Features

*   **Authentication**: Stateless JWT Tokens.
*   **Authorization**: Policy-based access (e.g., `PremiumOnly` endpoints).
*   **Password Hashing**: ASP.NET Core Identity PasswordHasher.
*   **Internal Security**: Microservices use `X-Internal-Key` headers or forwarded JWTs to verify inter-service requests, preventing direct external access.
*   **Input Sanitization**: `HtmlSanitizer` is used to prevent XSS attacks when users input rich text into resume sections.

---

## 🌐 API Endpoints (Sample)

### Authentication
*   `POST /api/auth/register` - Register new user
*   `POST /api/auth/login` - Login with credentials
*   `GET /api/auth/me` - Get current user profile

### Resume Management
*   `POST /api/resumes` - Create a new resume
*   `GET /api/resumes/{id}` - Get full resume context
*   `POST /api/sections` - Add a new section (Experience, Education)

### AI Generation
*   `POST /api/ai/summary` - Generate AI summary
*   `POST /api/ai/ats-score` - Check ATS compatibility

---

## 🚀 Getting Started

### Prerequisites
*   .NET 8.0 SDK or later
*   Docker & Docker Compose
*   PostgreSQL 15+
*   Redis 7
*   RabbitMQ

### Environment Variables
Create a `.env` file in the root directory:

```env
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_secure_password
JWT_SECRET=your_jwt_secret_key_minimum_32_characters
RABBITMQ_DEFAULT_USER=admin
RABBITMQ_DEFAULT_PASS=your_rabbitmq_password
OPENAI_API_KEY=your_groq_or_openai_api_key
```

### Installation & Setup

**Option 1: Using Docker Compose (Recommended)**
```bash
# Clone the repository
git clone <repository-url>
cd ResumeAI

# Build and start all services
docker-compose up --build

# Access the API Gateway
# http://localhost:8080
```

**Option 2: Manual Setup**
```bash
# Navigate to a service
cd src/ResumeAI.Auth.API

# Run database migrations
dotnet ef database update

# Run the service
dotnet run
```
*(Repeat for all microservices)*
