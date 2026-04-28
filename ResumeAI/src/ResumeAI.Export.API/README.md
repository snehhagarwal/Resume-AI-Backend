# ResumeAI.Export.API

## Purpose
Converts a resume (metadata + sections) into a downloadable PDF or DOCX file. Export jobs are queued via RabbitMQ so the HTTP response is immediate (202 Accepted + job ID). The consumer processes the render asynchronously, stores the file, and fires a notification when done.

## Tech Stack
| Layer | Choice | Why |
|---|---|---|
| Framework | ASP.NET Core 8 Controllers | |
| ORM | EF Core 8 + Npgsql | Job persistence |
| Queue | RabbitMQ (via `RabbitMQ.Client`) | Decouple heavy rendering from HTTP request |
| PDF | `QuestPDF` or `Puppeteer Sharp` | Configurable — QuestPDF for pure .NET, Puppeteer for pixel-perfect HTML |
| DOCX | `DocumentFormat.OpenXml` | Microsoft's official OpenXML SDK |
| Auth | JWT Bearer | |

## Database
`resumeai_export` on the shared PostgreSQL instance.

### Key Entities
| Entity | Key Fields |
|---|---|
| `ExportJob` | `JobId` (GUID), `UserId`, `ResumeId`, `Format`, `Status`, `FileUrl`, `ErrorMessage`, `RequestedAt`, `CompletedAt` |

### Statuses
`QUEUED` → `PROCESSING` → `COMPLETED` / `FAILED`

### Formats
`PDF` · `DOCX`

## Endpoints
| Method | Route | Auth | Description |
|---|---|---|---|
| POST   | `/api/exports/pdf` | JWT | Queue a PDF export job |
| POST   | `/api/exports/docx` | JWT | Queue a DOCX export job |
| GET    | `/api/exports/{jobId}/status` | JWT | Poll job status |
| GET    | `/api/exports/{jobId}/download` | JWT | Download completed file |
| GET    | `/api/exports/my` | JWT | List all export jobs for current user |
| GET    | `/api/exports` | ADMIN | List all jobs |

## RabbitMQ Flow
```
POST /api/exports/pdf
  → Create ExportJob (status=QUEUED) in DB
  → Publish { jobId, resumeId, format, userId } to "export.queue"
  → Return 202 { jobId }

RabbitMQ Consumer (background service)
  → Dequeue message
  → Fetch resume + sections from Resume.API + Section.API
  → Render PDF/DOCX using template layout
  → Save file to disk / blob storage
  → Update ExportJob (status=COMPLETED, fileUrl=...)
  → Publish notification event to "notification.queue"
```

## Environment Variables
| Key | Description |
|---|---|
| `ConnectionStrings__ExportDb` | PostgreSQL connection string |
| `RabbitMQ__Host` | RabbitMQ hostname |
| `RabbitMQ__Username` | RabbitMQ username |
| `RabbitMQ__Password` | RabbitMQ password |
| `Storage__OutputPath` | Local path for generated files |
| `Jwt__Secret` | Shared signing secret |
| `Jwt__Issuer` | Must match Auth.API |
| `Jwt__Audience` | Must match Auth.API |

## Design Decisions
- **Async via RabbitMQ** — PDF rendering (especially with Puppeteer) can take 2–5 seconds. Blocking the HTTP thread for that is unacceptable. The queue decouples the request from the rendering work.
- **Job ID is a GUID** — not an integer, to prevent enumeration attacks on the download endpoint.
- **Ownership check on download** — `ExportJob.UserId` must match the JWT subject. Admins bypass this.
- **`FAILED` status with `ErrorMessage`** — if rendering throws, the job is marked FAILED with a human-readable error rather than silently disappearing. The client can poll and show the error.
- **Notification on completion** — on `COMPLETED`, the consumer publishes a message to `notification.queue`. The Notification service picks this up and pushes a SignalR notification to the user's browser.
- **No direct cross-service DB access** — Export.API calls Resume.API and Section.API via HTTP to fetch content. Services don't share DB schemas.

## Running Locally
```bash
# Requires RabbitMQ
docker run -p 5672:5672 -p 15672:15672 rabbitmq:3-management-alpine
dotnet run --project src/ResumeAI.Export.API
# Swagger: http://localhost:5006/swagger
```
