# Msanga API: code map

Where everything lives, and why. Kept up to date with every milestone.

_Last updated: M3a-1 joining a school (14 Nov 2026)_

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
| `Msanga.Domain` | entities (`Member`, `Community`, `CommunityAlias`, `CommunityMembership`, `RefreshToken`, `AccountCode`), `Result` / `Error`, `AuthErrors`, interfaces (`IAuthService`, `IEmailSender`, repositories, `IUnitOfWork`, `ICurrentUser`), constants (setting keys, roles, policies), `SearchKey`. No packages. |
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

## M2a-2: schools and search (13 Nov 2026)

| File | What it does |
|---|---|
| `Domain/Enums/CommunityEnums.cs` | `CommunityKind` (School), `CommunityLevel` (Primary, Secondary, Tertiary), `CommunityStatus` (Listed, New) |
| `Domain/Entities/Community.cs` | a school: name, level, town, state, two-letter country, status, who added it (admins only); `SearchKey` = name + town (town once); `AddAlias` skips a name it already has, however it's written |
| `Domain/Entities/CommunityAlias.cs` | another name ("FGCIE") and its key |
| `Domain/Interfaces/ICommunityRepository.cs`, `Shared/Errors/CommunityErrors.cs` | the contract (`Add`, `GetByIdAsync`, `SearchAsync`); `Community.NotFound` |
| `Application/Features/Communities/*` | `SearchCommunitiesQuery` (at most 20; under 2 letters → none; at most 100 characters), `GetCommunityQuery`, `CommunityDto` (`isNew`, `otherNames`; never who added it) |
| `Persistence/Configurations/CommunityConfiguration.cs` | tables `Communities` and `CommunityAliases` (deleted with their school; one school can't have the same other name twice) |
| `Persistence/Repositories/CommunityRepository.cs` | search: every word must start a word of the school's key or an other name's key (`' ' + key LIKE '% word%'`); best first = a key starting with the whole search, then Listed, then A–Z |
| `Persistence/Seeding/CommunitySeeder.cs` | development: six schools and their other names; adds only the missing ones (step 4 of start-up, `SeedData:CommunitiesEnabled`) |
| `Presentation/Endpoints/Communities/CommunityEndpoints.cs` | `GET /api/communities?search=`, `GET /api/communities/{id}`, members only |
| `UnitTests/Domain/CommunityTests.cs` | the town in the key once, other names not repeated, tidy places, bad details refused |

## M2b: adding a school (14 Nov 2026)

| File | What it does |
|---|---|
| `Domain/Constants/NigerianStates.cs` | the 36 states and the FCT; `Find` tidies what was sent ("lagos state" → "Lagos", "FCT"/"Abuja" → the FCT) |
| `Domain/Shared/Errors/CommunityErrors.cs` | + `AlreadyListed`, `LooksLike` (409) |
| `Domain/Interfaces/ICommunityRepository.cs` | + `FindBySearchKeyAsync` (exact name + town) |
| `Application/Features/Communities/Commands/AddCommunityCommand.cs` | 1. same key already there → AlreadyListed (no way round it); 2. unless `addAnyway`, lookalikes (search by the new NAME without the town, then by each other name; up to 5) → LooksLike; 3. add as New with who added it. `AddCommunityOutcome` says which |
| `Application/Features/Communities/Validators/CommunityValidators.cs` | + `AddCommunityCommandValidator`: name, level, town, two-letter country; Nigeria → one of the states; up to 5 other names |
| `Presentation/Endpoints/Communities/CommunityEndpoints.cs` | + `POST /api/communities`: 201 with `Location`, or 409 ProblemDetails with `suggestions` |
| `Presentation/Extensions/ResultExtensions.cs` | `ToProblem` can add fields to the body (`suggestions`) |
| `Presentation/Settings/RateLimiting/RateLimitingSetup.cs` | + `add-community`: 20 tries a day per member |
| `UnitTests/Domain/NigerianStatesTests.cs`, `UnitTests/Application/AddCommunityCommandValidatorTests.cs` | the state list; what the form must send |

## M3a-1: joining a school (14 Nov 2026)

**School first, then the year** (decided 14 Nov 2026; see the roadmap): a member belongs to a school ("I was a
student here", with the years), then to a year (set, M3b). Friendships, events and memories can cross years; only
people from the school ever see its people (once confirmed, M3c).

| File | What it does |
|---|---|
| `Domain/Entities/CommunityMembership.cs` | member + school + `FromYear` / `ToYear` + `Finished` + `Status`; `YearsProblem`: 1940 to this year, start ≤ end, at most 12 years |
| `Domain/Enums/CommunityEnums.cs` | + `CommunityMembershipStatus` (Joined; Confirmed comes in M3c) |
| `Domain/Interfaces/ICommunityMembershipRepository.cs`, `Shared/Errors/CommunityErrors.cs` | Add, Remove, Get, ListForMember; + `AlreadyMember` (409), `NotAMember` (404) |
| `Application/Features/Communities/Commands/MembershipCommands.cs` | Join (the school must exist; not twice), ChangeMySchoolYears, Leave (deletes the row); `ISchoolYears` |
| `Application/Features/Communities/Queries/MembershipQueries.cs` | GetMySchools, GetMyMembership |
| `Application/Features/Communities/Validators/MembershipValidators.cs` | `SchoolYearsValidator<T>`: the year rules for the form, "this year" from `TimeProvider` |
| `Application/Features/Communities/Dtos/CommunityDtos.cs` | + `MembershipDto` (school, years, finished, status) |
| `Persistence/Configurations/CommunityMembershipConfiguration.cs` | table `CommunityMemberships` (migration `SchoolMemberships`): one per member per school (unique), deleted with the member or the school, a check constraint on the years, index by school + leaving year |
| `Persistence/Repositories/CommunityMembershipRepository.cs` | loads the school and its other names with each membership; My Schools most recent first |
| `Presentation/Endpoints/Communities/CommunityEndpoints.cs` | + `GET` / `POST` / `PUT` / `DELETE /api/communities/{id}/membership` (`SchoolYearsBody`) |
| `Presentation/Endpoints/Members/MemberEndpoints.cs` | + `GET /api/me/schools` |
| `UnitTests/Domain/CommunityMembershipTests.cs`, `UnitTests/Application/SchoolYearsValidatorTests.cs` | the year rules (clock fixed in 2026) |

## Coming next

- **M3a-2, web:** "Were you a student here?" on the school's page, Change / Leave, My Schools.
- **M3b:** sets inside a school (home set from `Finished` + `ToYear`, house, class arm, department).
- **M3c:** vouching ("I know them"), Confirmed, limited view until then.
- **M3d:** Find People within your schools.

- **M2c, web:** Find your school (live search, results, the school's page).
- **M2d, web:** Add your school (the form, "Did you mean…?"); Find Classmates / My Schools stop being "coming soon".
- **Later:** lookalikes that also catch spelling mistakes ("Umuhia"); admin review of new schools (M7).

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
