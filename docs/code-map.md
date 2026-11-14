# Msanga API: code map

Where everything lives, and why. Kept up to date with every milestone.

_Last updated: M3d-1 Find People (14 Nov 2026)_

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

## M3b-1: your set (14 Nov 2026)

Decided 14 Nov 2026: **each school chooses how its sets are named** (by leaving year, the default, or by entry year);
sets are shown as **"1998 Set"**; house, class arm, hostel and department are **free text**, to be tidied later with
AI (merges suggested per school, approved by an admin). A set is a school + a year: no set table yet (it comes with
the first thing that hangs off a set, e.g. its chat).

| File | What it does |
|---|---|
| `Domain/Enums/CommunityEnums.cs` | + `SetNaming` (ByLeavingYear, ByEntryYear) |
| `Domain/Entities/Community.cs` | + `SetNaming` (default by leaving year), `ChangeSetNaming`, `SetName(year)` = "1998 Set" |
| `Domain/Entities/CommunityMembership.cs` | + `SetYear` (null until you choose), `House`, `ClassArm`, `Hostel`, `Department`; `SuggestedSetYear` (leaving: the year you left if you finished; entry: the year you came), `SetYearRange` (leaving: first year … left + 6; entry: came − 6 … left), `SetYearProblem`, `ChooseSet`; `ChangeYears` clears the set if it no longer fits |
| `Domain/Shared/Errors/CommunityErrors.cs` | + `MembersOnly` (403), `SetOutOfRange(message)` (400) |
| `Application/…/Commands/MembershipCommands.cs` | + `ChooseMySetCommand` |
| `Application/…/Queries/MembershipQueries.cs` | + `GetSetQuery` (school members only) |
| `Application/…/Validators/MembershipValidators.cs` | + details ≤ 60; set pages from 1940 to 6 years from now |
| `Application/…/Dtos/CommunityDtos.cs` | `CommunityDto` + `setNaming`; `MembershipDto` + `setYear`, `setName`, `suggestedSetYear`, `setYearChoices`, the four details; + `SetDto` |
| `Persistence/Configurations/*` | migration `Sets`: `Communities.SetNaming` (existing schools: by leaving year), the membership's set and details, index by school + set |
| `Presentation/Endpoints/Communities/CommunityEndpoints.cs` | + `PUT …/membership/set` (`MySetBody`), `GET /api/communities/{id}/sets/{year}` |
| `UnitTests/Domain/SetTests.cs`, `UnitTests/Application/SchoolYearsValidatorTests.cs` | suggestions, ranges, choosing, years changed, details, set-page years |

## M3c-1: vouching (14 Nov 2026)

Decided 14 Nov 2026: **2 vouches** ("I know them") from confirmed members of the school, any year, confirm someone;
in a school's **early days** (fewer than 3 confirmed) joined members may vouch too; an **admin** can confirm directly;
**who vouched is shown** ("Confirmed by Nse U. and Utibe E."); confirmed members see people waiting: **name, set,
years, house / department** (no photo, email or other schools). Every vouch is kept (tracing fakes, M7).

| File | What it does |
|---|---|
| `Domain/Rules/Vouching.cs` | the rules in one place: `VouchesToConfirm` (2), `EarlyDaysConfirmedMembers` (3), `MaxVouchesPerDay` (20), `MayVouch`, `Confirms`, `InEarlyDays` |
| `Domain/Entities/Vouch.cs` | who vouched, for whom, at which school (never for yourself) |
| `Domain/Entities/CommunityMembership.cs` | + `Member` (navigation), `ConfirmedAt`, `Confirm(now)` (once) |
| `Domain/Interfaces/IVouchRepository.cs`, `ICommunityMembershipRepository.cs` | + vouches (exists, count, voucher names, whom I vouched for); + count confirmed, waiting list, a set's confirmed members; `GetAsync` loads the member too |
| `Domain/Shared/Errors/CommunityErrors.cs` | + `PersonNotFound`, `CannotVouchForYourself`, `NotConfirmedYet` (403), `AlreadyVouched`, `AlreadyConfirmed` (409) |
| `Application/Features/Communities/Commands/VouchCommands.cs` | `VouchCommand` (checks in order, the second vouch confirms + emails), `AdminConfirmMemberCommand` |
| `Application/Features/Communities/Queries/MembershipQueries.cs` | `GetWaitingQuery`; `GetMyMembershipQuery` + `vouchedBy`, `canVouch`; `GetSetQuery` + who's here (confirmed members only) |
| `Application/Features/Communities/ConfirmedEmail.cs` | "You're confirmed at …, confirmed by …" (no links); a failure is logged, never undoes it |
| `Application/Features/Communities/Dtos/CommunityDtos.cs` | + `PersonDto` (what schoolmates see), `VouchOutcomeDto`; `MembershipDto` + `vouchedBy`, `canVouch`; `SetDto` + `canSeeMembers`, `members` |
| `Persistence/Configurations/VouchConfiguration.cs` | table `Vouches` (migration `Vouches`): one per voucher + person + school, never yourself (check constraint); kept if the voucher's account goes (NoAction: account deletion in M8 must handle it) |
| `Persistence/Repositories/VouchRepository.cs` | the queries above |
| `Persistence/Seeding/AdminSeeder.cs`, `DemoMemberSeeder.cs`, `DbSeeder.cs` | step 5: Nse and Utibe joined at FGCIE (development); step 6: `SeedData:AdminEmail` gets the Admin role (any environment) |
| `Presentation/Endpoints/Communities/CommunityEndpoints.cs` | + `GET …/waiting`, `POST …/members/{memberId}/vouch` (rate limit `vouch`) |
| `Presentation/Endpoints/Admin/AdminEndpoints.cs` | `/api/admin` (RequireAdmin): `POST …/confirm` |
| `UnitTests/Domain/VouchingTests.cs` | 2 confirm, early days, not yourself, confirm once, the email |

## M3d-1: Find People (14 Nov 2026)

Decided 14 Nov 2026: search only schools where **you're confirmed**, finding only people **confirmed** there (name,
surname at school, school, set, house, country); **where you live now** is optional (city + country, schoolmates
only); a **person's page** shows name, bio, where they live and the schools you share, and is "not found" for
anyone else; Home's set cards show **a few names, never numbers**.

| File | What it does |
|---|---|
| `Domain/Entities/Member.cs` | + `LivesInCity`, `LivesInCountryCode`, `SetWhereYouLive` (a city needs its country) |
| `Domain/ValueObjects/PeopleFilter.cs`, `Shared/Errors/PeopleErrors.cs` | what Find People looks for; `ConfirmFirst` (403), `NotFound` (404) |
| `Domain/Interfaces/ICommunityMembershipRepository.cs` | + `ConfirmedSchoolIdsAsync`, `FindPeopleAsync`, `ConfirmedAtAsync` |
| `Domain/Interfaces/IAuthService.cs`, `Infrastructure/…/AuthService.cs`, `Application/…/Auth/Dtos/AuthDtos.cs` | `MemberAccount` / `MemberDto` + where you live |
| `Application/Features/Members/*` | `UpdateMyProfileCommand` + `livesInCity`, `livesInCountryCode` (optional) and their rules |
| `Application/Features/People/*` | `FindPeopleQuery` (your confirmed schools only; never you; a school that isn't yours finds nobody), `GetPersonQuery` (shared confirmed schools only, with "Confirmed by"), `PersonCardDto`, `PersonPageDto`, `SharedSchoolDto`, the validator |
| `Application/…/Communities/Queries/MembershipQueries.cs`, `Dtos/CommunityDtos.cs` | My Schools + `setMates` (up to 3 first names of confirmed set-mates) and `moreSetMates` |
| `Persistence/Repositories/CommunityMembershipRepository.cs` | the people queries (A–Z, at most 50 people, then all the schools you share with each) |
| `Persistence/Configurations/MemberConfiguration.cs` | migration `WhereYouLive`: the two columns, index by country |
| `Persistence/Seeding/DemoMemberSeeder.cs` | demo members live in Lagos, Uyo, London (filled in for existing ones too) |
| `Presentation/Endpoints/People/PeopleEndpoints.cs` | `GET /api/people`, `GET /api/people/{memberId}` (rate limit `people`: 60 a minute per member) |
| `UnitTests/Application/FindPeopleTests.cs`, `UnitTests/Domain/WhereYouLiveTests.cs` | who may find whom (with a pretend repository); where you live |

## Coming next

- **M3d-2, web:** Find Classmates, a person's page, where you live on Edit profile, Home's real set cards.
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
