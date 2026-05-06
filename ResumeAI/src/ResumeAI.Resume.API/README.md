# ResumeAI.Resume.API

## Purpose
Owns the top-level Resume entity — the "envelope" that holds metadata like title, target job title, template choice, ATS score, and publication status. Every other domain (sections, AI, exports) references a `resumeId`. Think of this service as the index card; the other services fill in the content.

## Tech Stack
| Layer | Choice |
|---|---|
| Framework | ASP.NET Core 8 Controllers |
| ORM | EF Core 8 + Npgsql |
| Auth | JWT Bearer (validates token minted by Auth.API) |

## Database
`resumeai_resume` on the shared PostgreSQL instance.

### Key Entities
| Entity | Key Fields |
|---|---|
| `Resume` | `ResumeId`, `UserId`, `Title`, `TargetJobTitle`, `TemplateId`, `AtsScore`, `Status`, `Language`, `CreatedAt`, `UpdatedAt` |

### Enums
- `ResumeStatus`: `DRAFT`, `PUBLISHED`, `ARCHIVED`

## Endpoints
| Method | Route | Auth | Description |
|---|---|---|---|
| POST   | `/api/resumes` | JWT | Create a new resume |
| GET    | `/api/resumes/my` | JWT | List all resumes for current user |
| GET    | `/api/resumes/{id}` | JWT | Get a single resume (ownership enforced) |
| PUT    | `/api/resumes/{id}` | JWT | Update title, job title, template |
| DELETE | `/api/resumes/{id}` | JWT | Delete resume (cascades to sections) |
| PUT    | `/api/resumes/{id}/ats-score` | JWT | Update ATS score (called by AI service) |
| GET    | `/api/resumes` | ADMIN | List all resumes across all users |

## Environment Variables
| Key | Description |
|---|---|
| `ConnectionStrings__ResumeDb` | PostgreSQL connection string |
| `Jwt__Secret` | Same secret as Auth.API — shared HMAC key |
| `Jwt__Issuer` | Must match Auth.API issuer |
| `Jwt__Audience` | Must match Auth.API audience |

## Design Decisions
- **UserId comes from JWT claims** — never from the request body. This prevents one user from creating a resume under another user's ID.
- **Ownership check on every mutating endpoint** — the service reads `UserId` from the JWT and compares it to the resume's `UserId`. Admins bypass this via role check.
- **AtsScore is written by the AI service** — this endpoint exists so AI.API can persist the score after running an ATS analysis without needing direct DB access to this schema.
- **Status field** (`DRAFT`/`PUBLISHED`/`ARCHIVED`) allows a future publishing/portfolio feature without schema migration.
- **`TemplateId` is a foreign-key reference** to the Template service's data, but there is no cross-service FK enforced at the DB level — services own their own schemas. Referential integrity is enforced at the application layer.

## Running Locally
```bash
dotnet run --project src/ResumeAI.Resume.API
# Swagger: http://localhost:5002/swagger
```
