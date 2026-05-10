# ResumeAI.Notification.API

## Purpose
Delivers real-time and persistent notifications to users. Notification events are consumed from RabbitMQ (published by Export.API, AI.API, etc.) and pushed to the user's browser via SignalR. Notifications are also stored in the DB so users can review them even after reconnecting.

## Tech Stack
| Layer | Choice | Why |
|---|---|---|
| Framework | ASP.NET Core 8 Controllers + SignalR Hub | Controllers for REST, SignalR for real-time push |
| ORM | EF Core 8 + Npgsql | Persistent notification store |
| Message Broker | RabbitMQ consumer (background `IHostedService`) | Decoupled event consumption |
| Auth | JWT Bearer + SignalR JWT extraction | SignalR needs token from query string |

## Database
`resumeai_notification` on the shared PostgreSQL instance.

### Key Entities
| Entity | Key Fields |
|---|---|
| `Notification` | `NotificationId`, `UserId`, `Type`, `Title`, `Message`, `Channel`, `IsRead`, `SentAt` |

### Notification Types
`ATS_COMPLETE` · `EXPORT_READY` · `AI_DONE` · `JOB_MATCH` · `PLAN_CHANGE` · `QUOTA_WARNING`

### Channels
`IN_APP` · `EMAIL` (future) · `PUSH` (future)

## Endpoints
| Method | Route | Auth | Description |
|---|---|---|---|
| GET    | `/api/notifications` | JWT | Get all notifications for current user |
| GET    | `/api/notifications/unread-count` | JWT | Get count of unread notifications |
| PUT    | `/api/notifications/{id}/read` | JWT | Mark a notification as read |
| PUT    | `/api/notifications/read-all` | JWT | Mark all notifications as read |
| DELETE | `/api/notifications/{id}` | JWT | Delete a notification |

## SignalR Hub
| Hub Route | Event | Payload |
|---|---|---|
| `/hubs/notifications` | `ReceiveNotification` | `{ title, message, type }` |

### Connecting from the client
```typescript
import { HubConnectionBuilder } from '@microsoft/signalr'

const conn = new HubConnectionBuilder()
  .withUrl('/hubs/notifications', { accessTokenFactory: () => localStorage.getItem('token')! })
  .withAutomaticReconnect()
  .build()

conn.on('ReceiveNotification', (n) => console.log('New notification:', n))
await conn.start()
```

## RabbitMQ Consumer Flow
```
Export.API publishes → "notification.queue" → { userId, type, title, message }
Notification consumer
  → Saves Notification to DB
  → Looks up SignalR connection for userId
  → Calls hub.Clients.User(userId).SendAsync("ReceiveNotification", payload)
```

## Environment Variables
| Key | Description |
|---|---|
| `ConnectionStrings__NotificationDb` | PostgreSQL connection string |
| `RabbitMQ__Host` | RabbitMQ hostname |
| `RabbitMQ__Username` | RabbitMQ username |
| `RabbitMQ__Password` | RabbitMQ password |
| `Jwt__Secret` | Shared signing secret |
| `Jwt__Issuer` | Must match Auth.API |
| `Jwt__Audience` | Must match Auth.API |

## Design Decisions
- **SignalR uses JWT from query string** — browsers can't set `Authorization` headers on WebSocket connections. ASP.NET SignalR handles this via `OnMessageReceived` extracting the token from `context.Request.Query["access_token"]`.
- **Persistent + real-time** — notifications are saved to DB before being pushed via SignalR. If a user is offline, they still see the notification when they next open the app.
- **RabbitMQ consumer is a `BackgroundService`** — it runs on a dedicated thread alongside the ASP.NET host. It acks messages only after the DB write succeeds to prevent data loss on crash.
- **`IsRead` stays false until the user explicitly marks it** — the app badge count (`/unread-count`) reflects this. Marking as read is a deliberate user action, not automatic on view.
- **Multi-channel groundwork** — `Channel` enum supports `EMAIL` and `PUSH` without a schema change. Email delivery (via SendGrid etc.) can be wired in later by checking the channel before dispatching.

## Running Locally
```bash
# Requires RabbitMQ
docker run -p 5672:5672 rabbitmq:3-management-alpine
dotnet run --project src/ResumeAI.Notification.API
# Swagger: http://localhost:5008/swagger
# SignalR hub: ws://localhost:5008/hubs/notifications
```
