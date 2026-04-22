# ResumeAI.Template.API

## Purpose
Manages the library of resume templates. A template defines the visual layout and style that the Export service uses when rendering a resume to PDF/DOCX. Free templates are available to all users; Premium templates require an active PREMIUM subscription.

## Tech Stack
| Layer | Choice |
|---|---|
| Framework | ASP.NET Core 8 Controllers |
| ORM | EF Core 8 + Npgsql |
| Auth | JWT Bearer |

## Database
`resumeai_template` on the shared PostgreSQL instance.

### Key Entities
| Entity | Key Fields |
|---|---|
| `Template` | `TemplateId`, `Name`, `Description`, `Category`, `ThumbnailUrl`, `IsPremium`, `IsActive`, `UsageCount`, `CreatedAt` |

### Categories (suggested)
`PROFESSIONAL` · `CREATIVE` · `MINIMAL` · `ACADEMIC` · `EXECUTIVE`

## Endpoints
| Method | Route | Auth | Description |
|---|---|---|---|
| GET    | `/api/templates` | JWT | Get all active templates |
| GET    | `/api/templates/free` | JWT | Get free templates only |
| GET    | `/api/templates/popular` | JWT | Get top N by usage count |
| GET    | `/api/templates/{id}` | JWT | Get a single template |
| POST   | `/api/templates` | ADMIN | Create a new template |
| PUT    | `/api/templates/{id}` | ADMIN | Update template metadata |
| DELETE | `/api/templates/{id}` | ADMIN | Deactivate a template |
| POST   | `/api/templates/{id}/increment-usage` | JWT | Increment usage count when a resume uses this template |

## Environment Variables
| Key | Description |
|---|---|
| `ConnectionStrings__TemplateDb` | PostgreSQL connection string |
| `Jwt__Secret` | Shared signing secret |
| `Jwt__Issuer` | Must match Auth.API |
| `Jwt__Audience` | Must match Auth.API |

## Design Decisions
- **`IsPremium` is enforced at the API level** — when a FREE user requests a PREMIUM template, the service returns `403 Forbidden`. The subscription plan is read from the JWT `plan` claim; no call to Auth.API needed at runtime.
- **Soft delete (`IsActive`)** — deactivating a template doesn't orphan existing resumes that already reference it. Export service falls back to a default layout if a template is deactivated.
- **`UsageCount` is an optimistic counter** — incremented via a dedicated endpoint rather than a trigger. Slight inaccuracy is acceptable; it's for popularity ranking, not billing.
- **Templates don't own their layout files here** — the actual HTML/CSS layout files live in the Export service's embedded resources. The `TemplateId` is the contract between the two services. This keeps Template.API lightweight (metadata only) and Export.API in control of rendering.
- **Seeding** — initial templates are seeded via an EF Core `HasData` migration so the app works out of the box with no manual data entry.

## Running Locally
```bash
dotnet run --project src/ResumeAI.Template.API
# Swagger: http://localhost:5005/swagger
```
