# ResumeAI.JobMatch.API

## Purpose
Analyses the compatibility between a user's resume and a job description, producing a match score (0–100%) along with identified skill gaps and improvement recommendations. This is a PREMIUM feature — FREE users receive a 402 response.

## Tech Stack
| Layer | Choice |
|---|---|
| Framework | ASP.NET Core 8 Controllers |
| ORM | EF Core 8 + Npgsql |
| AI | Delegated to AI.API via HTTP (or direct OpenAI call) |
| Auth | JWT Bearer |

## Database
`resumeai_jobmatch` on the shared PostgreSQL instance.

### Key Entities
| Entity | Key Fields |
|---|---|
| `JobMatch` | `MatchId`, `UserId`, `ResumeId`, `JobTitle`, `JobDescription`, `MatchScore`, `MissingSkills`, `Recommendations`, `Source`, `IsBookmarked`, `AnalysedAt` |

## Endpoints
| Method | Route | Auth | Plan | Description |
|---|---|---|---|---|
| POST | `/api/job-matches/analyze` | JWT | PREMIUM | Analyse resume vs job description |
| GET  | `/api/job-matches/by-resume/{resumeId}` | JWT | Any | Get all matches for a resume |
| GET  | `/api/job-matches/top` | JWT | Any | Get matches above a score threshold |
| GET  | `/api/job-matches/{id}` | JWT | Any | Get a single match |
| POST | `/api/job-matches/{id}/bookmark` | JWT | Any | Toggle bookmark |
| DELETE | `/api/job-matches/{id}` | JWT | Any | Delete a match |
| GET  | `/api/job-matches` | ADMIN | ADMIN | All matches |

## Scoring Logic
The match score is computed by sending the resume content + job description to the AI service with a structured prompt that asks for:
1. Overall match percentage
2. List of skills in the JD that are missing from the resume
3. Actionable recommendations to improve fit

The AI returns structured JSON; the service parses it and persists the `JobMatch` record.

## Environment Variables
| Key | Description |
|---|---|
| `ConnectionStrings__JobMatchDb` | PostgreSQL connection string |
| `AiService__BaseUrl` | Internal URL of AI.API (e.g. `http://ai:8080`) |
| `Jwt__Secret` | Shared signing secret |
| `Jwt__Issuer` | Must match Auth.API |
| `Jwt__Audience` | Must match Auth.API |

## Design Decisions
- **PREMIUM gate at the controller level** — the `plan` claim from the JWT is checked before any DB or AI call. If `plan != PREMIUM`, returns `402 Payment Required` with an upgrade message.
- **`IsBookmarked` flag** — users can save interesting job matches for later review, similar to a job-saved list. No separate entity needed.
- **`Source` field** — records where the job description came from (`MANUAL`, `LINKEDIN`, `INDEED`). Future integrations can set this automatically.
- **Match history is kept** — every analysis is persisted, not overwritten. A user can re-analyse the same job after updating their resume and compare scores over time.
- **Score is opaque to the client** — the AI produces it; we store it. We don't re-derive it. If the AI changes its scoring rubric, old scores stay as-is (no retroactive recalculation).

## Running Locally
```bash
dotnet run --project src/ResumeAI.JobMatch.API
# Swagger: http://localhost:5007/swagger
```
