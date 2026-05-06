# ResumeAI.Gateway

## Purpose
The single entry point for all client traffic. Built on [YARP (Yet Another Reverse Proxy)](https://microsoft.github.io/reverse-proxy/). The gateway handles JWT validation, routes requests to the correct downstream service, and is the only service exposed to the public internet. All other services are internal.

## Tech Stack
| Layer | Choice | Why |
|---|---|---|
| Framework | ASP.NET Core 8 | |
| Proxy | YARP (`Yarp.ReverseProxy`) | Microsoft's official reverse proxy library for .NET |
| Auth | JWT Bearer middleware | Validate token once at the edge; downstream services can also re-validate |

## Routing Table
| Path Prefix | Downstream Service | Port |
|---|---|---|
| `/api/auth/**` | ResumeAI.Auth.API | 8080 |
| `/api/resumes/**` | ResumeAI.Resume.API | 8080 |
| `/api/sections/**` | ResumeAI.Section.API | 8080 |
| `/api/ai/**` | ResumeAI.AI.API | 8080 |
| `/api/templates/**` | ResumeAI.Template.API | 8080 |
| `/api/exports/**` | ResumeAI.Export.API | 8080 |
| `/api/job-matches/**` | ResumeAI.JobMatch.API | 8080 |
| `/api/notifications/**` | ResumeAI.Notification.API | 8080 |
| `/hubs/**` | ResumeAI.Notification.API | 8080 (WebSocket) |

## Ports
| Environment | Gateway port |
|---|---|
| Docker Compose | 5000 (external) → 8080 (internal) |
| Local dev | 5000 |

## Environment Variables
| Key | Description |
|---|---|
| `Jwt__Secret` | Same secret as all other services — validates token at the edge |
| `Jwt__Issuer` | Must match Auth.API |
| `Jwt__Audience` | Must match Auth.API |

## Design Decisions
- **YARP over custom middleware** — YARP is Microsoft's production-grade reverse proxy. It handles connection pooling, retries, health checks, and load balancing out of the box. Writing a custom proxy would violate YAGNI.
- **JWT validation at the gateway** — invalid tokens are rejected before ever hitting a downstream service. This means downstream services technically don't need auth middleware, but they still have it for defence-in-depth (direct internal calls should also be validated).
- **Gateway does NOT do service discovery** — in this project, downstream URLs are static (set via environment variables / `appsettings.json`). In a Kubernetes setup, you'd point to service DNS names.
- **WebSocket passthrough** — YARP supports WebSocket proxying natively. The `/hubs/**` prefix is proxied as a WebSocket upgrade to the Notification service. No special config needed beyond enabling WebSocket in the pipeline.
- **No business logic in the gateway** — it routes and validates. Any feature logic belongs in the downstream service. Keep the gateway thin.
- **Rate limiting** — ASP.NET Core 8's built-in rate limiting middleware (`AddRateLimiter`) can be layered on here at the gateway level to protect all downstream services uniformly.

## Running Locally
```bash
dotnet run --project src/ResumeAI.Gateway
# All APIs accessible via http://localhost:5000
```

## Running with Docker Compose
```bash
docker compose up -d
# Gateway: http://localhost:5000
# All other services are internal to the Docker network
```
