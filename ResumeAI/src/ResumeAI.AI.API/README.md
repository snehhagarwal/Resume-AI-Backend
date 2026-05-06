# ResumeAI.AI.API

## Purpose
The intelligence layer. Calls OpenAI (GPT-4o) and/or Anthropic (Claude) to generate resume content, check ATS compatibility, suggest skills, and generate cover letters. Tracks per-user quota, caches responses in Redis, and logs every request for billing/audit.

## Tech Stack
| Layer | Choice | Why |
|---|---|---|
| Framework | ASP.NET Core 8 Controllers | |
| ORM | EF Core 8 + Npgsql | Request log persistence |
| Cache | Redis (`StackExchange.Redis`) | Avoid re-calling LLM for identical prompts |
| LLM | OpenAI `gpt-4o` (primary), Anthropic Claude (fallback) | Configurable per request type |
| Auth | JWT Bearer | |

## Database
`resumeai_ai` on the shared PostgreSQL instance.

### Key Entities
| Entity | Key Fields |
|---|---|
| `AiRequest` | `RequestId`, `UserId`, `ResumeId`, `RequestType`, `Prompt`, `AiResponse`, `Model`, `TokensUsed`, `Status`, `CreatedAt` |
| `AiQuota` | `QuotaId`, `UserId`, `Plan`, `MaxContentCalls`, `RemainingContentCalls`, `MaxAtsCalls`, `RemainingAtsCalls`, `ResetAt` |

### Request Types
`SUMMARY` · `BULLETS` · `ATS_CHECK` · `SKILLS` · `COVER_LETTER`

### Quota (by plan)
| Plan | Content calls/month | ATS calls/month |
|---|---|---|
| FREE | 10 | 3 |
| PREMIUM | 100 | 30 |

## Endpoints
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/ai/generate-summary` | JWT | Generate professional summary |
| POST | `/api/ai/generate-bullets` | JWT | Generate experience bullet points |
| POST | `/api/ai/check-ats` | JWT | ATS score + gap analysis |
| POST | `/api/ai/suggest-skills` | JWT | Suggest skills for target role |
| POST | `/api/ai/generate-cover-letter` | JWT | Generate tailored cover letter |
| GET  | `/api/ai/quota` | JWT | Get current user's quota |
| GET  | `/api/ai/history` | JWT | Get AI request history |

## Environment Variables
| Key | Description |
|---|---|
| `ConnectionStrings__AiDb` | PostgreSQL connection string |
| `Redis__ConnectionString` | Redis host (e.g. `redis:6379`) |
| `OpenAI__ApiKey` | OpenAI API key |
| `Anthropic__ApiKey` | Anthropic API key (optional fallback) |
| `Jwt__Secret` | Shared signing secret |
| `Jwt__Issuer` | Must match Auth.API |
| `Jwt__Audience` | Must match Auth.API |

## Design Decisions
- **Redis caching with a hash of (userId + requestType + prompt)** — identical prompts within the same hour return the cached response instantly, saving token costs. TTL = 1 hour.
- **Quota checked before calling LLM** — fail fast if quota exhausted. Never call the expensive API first.
- **ATS score is written back to Resume.API** — after `check-ats`, the service makes an internal HTTP call to `PUT /api/resumes/{id}/ats-score`. This keeps the ATS score visible in the resume list without the client needing a second call.
- **Model stored on every `AiRequest` row** — important for cost attribution and debugging when models are swapped.
- **Quota reset is time-based** (`ResetAt` field) — monthly reset. On each request, the service checks if `ResetAt < now` and resets counters if so. No cron job needed.
- **FREE plan is rate-limited, not blocked** — users see remaining quota in the dashboard. When quota hits 0, the endpoint returns 402 Payment Required with a clear upgrade message.

## Running Locally
```bash
dotnet run --project src/ResumeAI.AI.API
# Requires Redis running locally: docker run -p 6379:6379 redis:7-alpine
# Swagger: http://localhost:5004/swagger
```
