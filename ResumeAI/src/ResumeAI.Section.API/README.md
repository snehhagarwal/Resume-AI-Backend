# ResumeAI.Section.API

## Purpose
Manages the individual content blocks (sections) within a resume — Summary, Experience, Education, Skills, etc. This is where the actual textual content of a resume lives. Sections are ordered, typed, and togglable (visible/hidden).

## Tech Stack
| Layer | Choice |
|---|---|
| Framework | ASP.NET Core 8 Controllers |
| ORM | EF Core 8 + Npgsql |
| Auth | JWT Bearer |

## Database
`resumeai_section` on the shared PostgreSQL instance.

### Key Entities
| Entity | Key Fields |
|---|---|
| `Section` | `SectionId`, `ResumeId`, `SectionType`, `Title`, `Content`, `DisplayOrder`, `IsVisible`, `CreatedAt`, `UpdatedAt` |

### Section Types
`SUMMARY` · `EXPERIENCE` · `EDUCATION` · `SKILLS` · `CERTIFICATIONS` · `PROJECTS` · `LANGUAGES` · `CUSTOM`

## Endpoints
| Method | Route | Auth | Description |
|---|---|---|---|
| POST   | `/api/sections` | JWT | Add a section to a resume |
| GET    | `/api/sections/by-resume/{resumeId}` | JWT | Get all sections for a resume (ordered) |
| GET    | `/api/sections/{id}` | JWT | Get a single section |
| PUT    | `/api/sections/{id}` | JWT | Update title, content, visibility |
| DELETE | `/api/sections/{id}` | JWT | Delete a section |
| PUT    | `/api/sections/reorder/{resumeId}` | JWT | Re-order sections (pass ordered ID array) |

## Environment Variables
| Key | Description |
|---|---|
| `ConnectionStrings__SectionDb` | PostgreSQL connection string |
| `Jwt__Secret` | Shared signing secret |
| `Jwt__Issuer` | Must match Auth.API |
| `Jwt__Audience` | Must match Auth.API |

## Design Decisions
- **Content is raw text / Markdown** — the rendering layer (template/export service) decides how to style it. Storing raw text keeps this service agnostic to output format.
- **`DisplayOrder` is explicit** — instead of using insert order (fragile) we store an integer order that the client can reorder via the `/reorder` endpoint. The reorder endpoint accepts a full ordered ID array and does a batch update in a single transaction.
- **`IsVisible` flag** — lets users hide a section (e.g. hide References) without deleting it. Hidden sections are excluded by the Export service when rendering.
- **No direct resume ownership check here** — the Resume service owns that. Section service trusts that a valid JWT means an authenticated user, and validates that the `ResumeId` exists. In a stricter setup, a service-to-service call to Resume.API to verify ownership would be added.
- **`CUSTOM` section type** — the escape hatch. Lets users add anything the enum doesn't cover (e.g. "Volunteer Work", "Publications").

## Running Locally
```bash
dotnet run --project src/ResumeAI.Section.API
# Swagger: http://localhost:5003/swagger
```
