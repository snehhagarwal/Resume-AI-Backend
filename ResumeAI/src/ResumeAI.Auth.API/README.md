# ResumeAI.Auth.API

## Purpose
Handles all identity concerns: registration, login, JWT issuance, OAuth2 (Google & LinkedIn), subscription management, and user lifecycle. Every other service trusts JWTs that this service mints — it is the single source of truth for identity.

## Tech Stack
| Layer | Choice | Why |
|---|---|---|
| Framework | ASP.NET Core 8 Minimal/Controllers | Controllers used — keeps auth logic easy to follow |
| ORM | EF Core 8 + Npgsql | Code-first migrations, strong typing |
| Auth | ASP.NET Identity + JWT Bearer | Industry standard; JWT stateless, scales horizontally |
| OAuth2 | `Microsoft.AspNetCore.Authentication.Google` + `AspNet.Security.OAuth.LinkedIn` | Official + community packages for OAuth handshake |
| Password hashing | `PasswordHasher<T>` (ASP.NET Identity) | PBKDF2, industry standard |

## Database
`resumeai_auth` on the shared PostgreSQL instance.

### Key Entities
| Entity | Purpose |
|---|---|
| `User` | Core identity record — email, hashed password, role, plan, OAuth provider, active flag |

### Enums
- `Role`: `USER`, `ADMIN`
- `AuthProvider`: `LOCAL`, `GOOGLE`, `LINKEDIN`
- `SubscriptionPlan`: `FREE`, `PREMIUM`

## Endpoints
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | None | Create account with email + password |
| POST | `/api/auth/login` | None | Login, returns JWT + refresh token |
| POST | `/api/auth/logout` | JWT | Invalidates session (stateless — clears client token) |
| POST | `/api/auth/refresh` | None | Exchange refresh token for new JWT |
| GET  | `/api/auth/profile` | JWT | Get current user profile |
| PUT  | `/api/auth/profile` | JWT | Update name + phone |
| PUT  | `/api/auth/password` | JWT | Change password |
| PUT  | `/api/auth/subscription` | JWT | Upgrade/downgrade plan |
| DELETE | `/api/auth/deactivate` | JWT | Soft-delete own account |
| GET  | `/api/auth/users` | ADMIN | List all users |
| PUT  | `/api/auth/users/{id}/subscription` | ADMIN | Admin update user plan |
| DELETE | `/api/auth/users/{id}` | ADMIN | Admin deactivate user |
| GET  | `/api/auth/oauth/google` | None | Redirect to Google consent |
| GET  | `/api/auth/oauth/linkedin` | None | Redirect to LinkedIn consent |
| GET  | `/api/auth/oauth/{provider}/callback` | Cookie (transient) | OAuth callback → issues JWT |

## OAuth2 Flow
```
Browser → GET /api/auth/oauth/google
       → Google consent screen
       → Redirect to /signin-google (middleware callback)
       → Middleware validates code, writes identity to short-lived cookie
       → Redirect to /api/auth/oauth/google/callback
       → Controller reads cookie → find-or-create user → issue JWT → delete cookie
       → Returns { token, user }
```

## Environment Variables
| Key | Description |
|---|---|
| `ConnectionStrings__AuthDb` | PostgreSQL connection string |
| `Jwt__Secret` | ≥32-char signing secret |
| `Jwt__Issuer` | Token issuer claim (e.g. `ResumeAI`) |
| `Jwt__Audience` | Token audience claim |
| `OAuth__Google__ClientId` | From Google Cloud Console |
| `OAuth__Google__ClientSecret` | From Google Cloud Console |
| `OAuth__LinkedIn__ClientId` | From LinkedIn Developer Portal |
| `OAuth__LinkedIn__ClientSecret` | From LinkedIn Developer Portal |

## Design Decisions
- **Soft delete** (`IsActive = false`) rather than hard delete — preserves audit trail and lets admins reactivate accounts.
- **JWT is stateless** — logout is client-side token discard. For a production system, add a Redis token-denylist.
- **OAuth cookie lifetime = 5 minutes** — it's only a handshake buffer. The cookie is explicitly deleted the moment the callback controller issues the JWT.
- **`OAuthLoginAsync` guards against provider mismatch** — a user who registered with Google can't accidentally log in via LinkedIn with the same email and vice versa.
- **Single `User` table** — no separate `OAuthIdentity` table because each user has exactly one auth provider. If multi-provider linking were needed, this would need a `UserLogin` junction table.

## Running Locally
```bash
# From repo root
dotnet run --project src/ResumeAI.Auth.API
# Swagger: http://localhost:5001/swagger
```
