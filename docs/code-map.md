# Msanga API: code map

Where everything lives, and why. Kept up to date with every milestone.

_Last updated: M2a-1 search keys and the first tests (13 Nov 2026)_

## Layers

Each project only knows the ones below it:

```
Presentation (web API, composition root)
 ├─ Infrastructure (sign-in, JWT, email, settings) ─┬─ Application (use cases) ── Domain
 │                                                   └─ Persistence (SQL Server) ── Domain
 └─ Worker (background jobs, none yet) ── Application

UnitTests ── Domain (more layers as their tests arrive)
```

| Project | What lives there |
|---|---|
| `Msanga.Domain` | entities (`Member`, `RefreshToken`, `AccountCode`), `Result` / `Error`, `AuthErrors`, interfaces (`IAuthService`, `IEmailSender`, repositories, `IUnitOfWork`, `ICurrentUser`), constants (setting keys, roles, policies), `SearchKey`. No packages. |
| `Msanga.Application` | MediatR commands / queries and their FluentValidation validators; the pipeline (Logging → Validation → handler). |
| `Msanga.Persistence` | `MsangaDbContext` (Identity's tables + ours), table configurations, repositories, migrations (created with `dotnet ef`), start-up seeding (migrate, roles). |
| `Msanga.Infrastructure` | `AuthService` (Identity + JWT + emailed codes), `AccountEmails` (wording), SMTP sender (MailKit) and the development outbox folder, settings checked at start-up. |
| `Msanga.Presentation` | `Program.cs`, endpoints (`/api/hello`, `/api/auth`, `/api/me`), JWT authentication, CORS, rate limits, ProblemDetails errors, OpenAPI + Swagger, Serilog. |
| `Msanga.Worker` | the place for scheduled jobs (none yet). |
| `Msanga.UnitTests` | fast tests of the rules (xUnit v3), no database; `dotnet test`, and on every push. |

## M1: accounts (13 Nov 2026)

Matches the web app's stand-in (`msanga-web/src/api/authTypes.ts`), so the web app can switch from the stand-in to
these endpoints. Decisions: SQL Server; JWT (access token 15 minutes, refresh token 30 days, rotated on every
refresh); email + password; email must be confirmed before signing in; **6-digit codes, not links** (decided 13 Nov
2026: typed into the page, so they work across devices and in the mobile app later; Msanga never emails sign-in
links): each works once, for 30 minutes (confirm) or 15 (reset), with 5 wrong tries;
password at least 10 characters, no "one capital, one symbol" rules; 5 wrong passwords lock for 15 minutes.

| File | What it does |
|---|---|
| `Domain/Entities/Member.cs` | the person: names, surname at school, bio, photo; same Id as the account; no counts |
| `Domain/Entities/AccountCode.cs`, `Enums/AccountCodePurpose.cs` | emailed 6-digit codes (confirm / reset): stored as a SHA-256 hash, single use, expiring, 5 wrong tries (`TryUse`) |
| `Domain/Entities/RefreshToken.cs` | refresh tokens, hashed, rotated; re-use of a revoked one ends every session |
| `Domain/Shared/Errors/AuthErrors.cs` | every account failure, with its code and plain-English message |
| `Domain/Interfaces/IAuthService.cs` | the accounts contract (and `NewAccount`, `AuthTokens`, `MemberAccount`) |
| `Application/Features/Auth/*` | Register, ConfirmEmail, ResendConfirmation, Login, Refresh, Logout, ForgotPassword, ResetPassword, ChangePassword, GetMe; validators |
| `Application/Features/Members/*` | UpdateMyProfile and its validator |
| `Persistence/MsangaDbContext.cs` | Identity's tables + Members, RefreshTokens, AccountCodes; UTC dates; the unit of work |
| `Persistence/Configurations/*` | table mappings (`MemberConfiguration`, `Identity/AccountCodeConfiguration`, `Identity/RefreshTokenConfiguration`, `ApplicationUser`, `ApplicationRole`) |
| `Persistence/Seeding/DbSeeder.cs` | at start-up: migrations, then roles (Member, Admin); stops with the `dotnet ef` command if there's no migration yet |
| `Infrastructure/Services/Identity/AuthService.cs` | the accounts logic: privacy-safe register, confirm with a code, sign-in with lockout, token rotation, reset with a code / change password, "your password was changed" email |
| `Infrastructure/Services/Email/AccountEmails.cs` | the four account emails (HTML + text): the code large and spaced ("482 913"), no links, "Msanga will never email you a link to sign in" |
| `Infrastructure/Settings/Email/*` | `Email` settings; `SmtpEmailSender` (any SMTP provider) or `OutboxFolderEmailSender` (`App_Data/emails`, development) |
| `Infrastructure/Settings/Identity/*` | `Identity` (password, lockout, `Codes` lifetimes) and `Jwt` settings, Identity setup, `JwtTokenGenerator` (access and refresh tokens) |
| `Presentation/Endpoints/*` | `AuthEndpoints` (`/api/auth`), `MemberEndpoints` (`/api/me`), `HelloEndpoints` (`/api/hello`) |
| `Presentation/Settings/*` | JWT authentication and policies, CORS (the web app: `http://localhost:5174` in development), rate limits (sign-in 20 / 5 min, email-sending 5 / 15 min, typing codes 10 / 5 min, per IP), OpenAPI |
| `Presentation/Middleware/GlobalExceptionHandler.cs` | thrown exceptions → ProblemDetails (validation 400 with each field, conflicts 409, the rest 500 without details) |
| `Presentation/Msanga.Presentation.http` | every request, ready to try |
| `.github/workflows/msanga-api-ci.yml` | restore and build on every push (no deploy until M9) |

## Template fixes (same day)

- `.gitignore` is now a .NET one (it was a copy of the web app's): `bin/`, `obj/`, `*.user`, `logs/`, `App_Data/` and
  secrets are ignored; the committed `obj/` files and `Msanga.Presentation.csproj.user` were removed.
- Ports are `https://localhost:7311` and `http://localhost:5311`, as the web app expects.
- The projects reference each other as in the diagram above; the template's `Class1.cs` files and the
  WeatherForecast sample are gone.

## M2a-1: search keys (13 Nov 2026)

The first piece of M2 (communities: schools). Decisions: any member can add a school (marked "New" until an admin
reviews it in M7); levels Primary, Secondary, Tertiary (universities, polytechnics, colleges of education); any
country, Nigeria first; the starting list is the pilot schools, then members add.

| File | What it does |
|---|---|
| `Domain/Shared/SearchKey.cs` | a name → its search key: no accents, lowercase, no apostrophes, marks → spaces, initials joined ("F.G.C." → "fgc"), "the" dropped, short forms written out (`ShortForms`: "govt" → "government", "fgc" → "federal government college"…); whole words only |
| `UnitTests/Domain/SearchKeyTests.cs` | every way of writing a name we expect, and the key it must give |
| `.github/workflows/msanga-api-ci.yml` | now also runs `dotnet test` |

## Coming next

- **M2a-2:** the `Community` and `CommunityAlias` tables (migration `Communities`), `GET /api/communities?search=`,
  `GET /api/communities/{id}`, development seed of pilot schools.
- **M2b:** `POST /api/communities` with "Did you mean…?" (409 with suggestions) and a rate limit.

- **More tests:** AuthService against SQLite or Testcontainers, endpoints with `WebApplicationFactory`.
- **Web app:** "Check your email" and "Choose a new password" become code boxes (`inputmode="numeric"`,
  `autocomplete="one-time-code"`); then switch `msanga-web/src/api/auth.ts` from the stand-in to these endpoints
  (tokens kept by the web app, refresh before the access token expires).
- **M3:** sets (Class of 1998…) inside each school.
- **Photos (in the first milestone that handles pictures: the profile photo or the "Then & Now" wall):** add the
  `Storage` and `Images` settings then, together with the code that reads and checks them at start-up. Starting point
  (from zayun, 13 Nov 2026), to be agreed in plain English first:
  - **Storage:** private only, never a public `wwwroot/uploads` folder (photos are for confirmed set members:
    served by the API after a membership check, or by short-lived signed links). A provider setting: a local private
    folder (e.g. `App_Data/private-photos`) in development, a private cloud bucket (Cloudflare R2 or Azure Blob) in
    production. Signed links: `SigningKey` (secret, User Secrets), `PrivateLinkMinutes` (e.g. 5).
  - **Images:** `MaxUploadBytes` 15 MB (phone photos), `MaxSide` 1920 (the full-size version, loaded only for
    pinch-to-zoom), `ThumbnailMaxSide` smaller than zayun's 640 (try 320–400: data-light for Nigeria), `WebpQuality`
    around 82; test the numbers on a real phone. Strip hidden data (GPS location, phone model) on upload.
