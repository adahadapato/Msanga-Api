# Msanga API

The API of **Msanga**, the reunion app: find your old classmates, then keep the class organised and trusted. The
name is coined from *mme nsanga*, "my friends" in Annang. The web app is in **msanga-web**.

**.NET 11, ASP.NET Core minimal APIs, EF Core on SQL Server, ASP.NET Core Identity + JWT, MediatR + FluentValidation,
MailKit (SMTP), Serilog.** Same layers and patterns as zayun-api.

> **Status: M1 (accounts).** Create account, confirm email, sign in (access + refresh tokens), refresh, sign out,
> forgot / reset / change password, who am I, edit my profile. Plus M0's `GET /api/hello`. See `docs/code-map.md`.

## Run it on your PC (first time)

You need the .NET 11 SDK and SQL Server (LocalDB comes with Visual Studio; or SQL Server Express / Developer).

1. **Secrets** (kept outside the repo, in User Secrets). In `Msanga.Presentation`:

   ```powershell
   cd Msanga.Presentation
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=Msanga;Trusted_Connection=True;TrustServerCertificate=True"
   $bytes = New-Object byte[] 48                                   # a random 64-character key
   [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
   dotnet user-secrets set "Jwt:Key" ([Convert]::ToBase64String($bytes))
   cd ..
   ```

2. **The first database migration** (once; it creates `Msanga.Persistence/Migrations`, which you commit):

   ```powershell
   dotnet tool restore        # Msanga's own dotnet-ef (11.x), from dotnet-tools.json
   dotnet ef migrations add InitialAccounts --project Msanga.Persistence --startup-project Msanga.Presentation
   ```

   Later model changes get their own migration the same way (`dotnet ef migrations add <Name> …`).

   The tool is local to this repo, so zayun's pinned 10.0.15 isn't touched. When EF Core moves on (the `11.*`
   packages pick that up) and `dotnet ef` warns that the tools are older than the runtime, run
   `dotnet tool update dotnet-ef` in this folder.

3. **Start it:**

   ```powershell
   dotnet run --project Msanga.Presentation --launch-profile https
   ```

   The database and its tables are created (migrations applied) and the roles seeded at start-up. Swagger opens at
   <https://localhost:7311/swagger>; `GET https://localhost:7311/api/hello` answers **"Mme nsanga! Your set is waiting."**
   (the web app's connection check on Settings uses it).

`Msanga.Presentation/Msanga.Presentation.http` has every request ready to try from Visual Studio / VS Code.

### A demo member to sign in as (development only)

On your PC the API creates **Hilary Adeyemi** (`hilary@example.com`), already confirmed, so a fresh database
always has someone to sign in with. Her password is yours to choose, kept in User Secrets (never in the repo):

```powershell
cd Msanga.Presentation
dotnet user-secrets set "SeedData:DemoPassword" "reunion-1998-demo"
cd ..
```

Restart the API: the console says "Seeded demo member hilary@example.com". She's created once; after that she's left
alone. This only happens where `SeedData:DemoMembersEnabled` is true, i.e. `appsettings.Development.json`; the live
server never has demo members.

The same goes for the **starting list of schools** (`SeedData:CommunitiesEnabled`): six Nigerian schools with their
other names (FGCIE, GCU, KC, UniUyo, UNILAG, Akwapoly), so "Find your school" has something to find.

## Tests

```powershell
dotnet test
```

`Msanga.UnitTests` checks Msanga's rules (no database or web server needed), e.g. that "Govt. Coll., Umuahia" and
"Government College Umuahia" give the same search key. GitHub runs them on every push too.

## Emails

Confirming an email and resetting a password use a **6-digit code** from the email, typed into the page (it works
when the email is read on another device, and Msanga never emails sign-in links). A confirmation code works for 30
minutes, a reset code for 15; each works once and allows 5 wrong tries.

While `Email:Enabled` is `false` (the default), nothing is sent: each email is written as an HTML file to
`Msanga.Presentation/App_Data/emails/`. Open the newest one in a browser to read its code.

To send real emails, use any provider with SMTP (Brevo, Gmail / Google Workspace, Zoho, Microsoft 365, your host's
mail). For example Brevo (free tier, 300 emails a day):

```powershell
cd Msanga.Presentation
dotnet user-secrets set "Email:Enabled" "true"
dotnet user-secrets set "Email:Host" "smtp-relay.brevo.com"
dotnet user-secrets set "Email:Port" "587"
dotnet user-secrets set "Email:Username" "<your Brevo SMTP login>"
dotnet user-secrets set "Email:Password" "<your Brevo SMTP key>"
dotnet user-secrets set "Email:FromAddress" "<an address you verified with Brevo>"
```

## Endpoints

| Method and path | Who | What |
|---|---|---|
| `GET /api/hello` | anyone | the greeting |
| `POST /api/auth/register` | anyone | create an account; emails a 6-digit confirmation code; **always 204** |
| `POST /api/auth/confirm-email` | anyone | `{ email, code }`; 400 `Auth.InvalidCode` if wrong, expired or used (same answer for an unknown email) |
| `POST /api/auth/resend-confirmation` | anyone | a new confirmation code; **always 204** |
| `POST /api/auth/login` | anyone | `{ email, password }` → access + refresh token and the member; 401 / 403 `Auth.EmailNotConfirmed` / `Auth.LockedOut` |
| `POST /api/auth/refresh` | anyone | `{ refreshToken }` → a new pair (the old one stops working) |
| `POST /api/auth/logout` | anyone | `{ refreshToken }` → it stops working |
| `POST /api/auth/forgot-password` | anyone | emails a 6-digit reset code; **always 204** |
| `POST /api/auth/reset-password` | anyone | `{ email, code, newPassword }`; signs out every session |
| `PUT /api/auth/password` | member | `{ currentPassword, newPassword }`; signs out every session |
| `GET /api/me` | member | who I am |
| `PUT /api/me` | member | `{ firstName, lastName, schoolSurname, bio }` |
| `GET /api/communities?search=` | member | find a school ("fgc ikot", "UNILAG"); best first, at most 20; fewer than 2 letters → none |
| `GET /api/communities/{id}` | member | one school; 404 `Community.NotFound` |
| `GET /api/communities/{id}/membership` | member | have I joined this school? my years; 404 `Community.NotAMember` if not |
| `POST /api/communities/{id}/membership` | member | "I was a student here": `{ fromYear, toYear, finished }` → 201, status Joined; 409 `Community.AlreadyMember` |
| `PUT /api/communities/{id}/membership` | member | correct my years |
| `DELETE /api/communities/{id}/membership` | member | leave the school (204) |
| `GET /api/me/schools` | member | My Schools: my schools and years, most recent first |
| `POST /api/communities` | member | `{ name, level, town, state, countryCode, otherNames, addAnyway }` → 201, marked new; 409 `Community.AlreadyListed` (same name and town) or `Community.LooksLike` (send again with `addAnyway: true`), both with `suggestions`; 20 tries a day |

Errors are ProblemDetails with a `code` (e.g. `Auth.InvalidCredentials`); validation errors list each field under
`errors`. Too many tries: 429 `RateLimit.TooManyRequests`.

## Rules

- **Privacy first:** nothing tells a stranger whether an email has an account (same answer for a wrong email or
  password, or a wrong code and an unknown email; register, resend and forgot-password always say yes). Codes and
  refresh tokens are stored only as hashes. Msanga never emails sign-in links.
- **No popularity counts** about members, anywhere.
- **No secrets in this repo:** connection string, JWT key and SMTP password live in User Secrets (development) and
  environment variables (production, e.g. `Jwt__Key`).
- Every setting is checked at start-up: a missing or wrong value stops the API with a message saying which.
