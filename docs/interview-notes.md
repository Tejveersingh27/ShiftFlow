# ShiftFlow — Interview Notes

A running study sheet. One section per slice we build. The goal: be able to
explain and defend every part of this project without hand-waving.

---

## Slice 0 — Foundation: project, database, authentication

### What I built

An ASP.NET Core MVC application (.NET 10) with PostgreSQL persistence via
Entity Framework Core, and user accounts / login via ASP.NET Core Identity.
No business features yet — this slice just establishes a runnable foundation.

### The stack and why

| Choice | Why |
| --- | --- |
| ASP.NET Core MVC + Razor | Server-rendered pages; fast to build, no separate frontend to maintain for V1. |
| PostgreSQL | Free, production-grade relational DB; the domain (schedules, shifts, constraints) is highly relational. |
| Entity Framework Core | ORM — maps C# classes to tables, generates SQL, manages schema via migrations. |
| Npgsql | The EF Core "provider" (driver) that makes EF speak PostgreSQL specifically. |
| ASP.NET Core Identity | Battle-tested auth: user store, password hashing (PBKDF2), login/session cookies, pre-built account pages. Never roll your own auth. |

### Likely interview questions

**Q: Walk me through what happens when the app starts up.**
`Program.cs` runs. It has two phases separated by `builder.Build()`:
1. **Service registration** (`builder.Services.Add...`) — I build a catalogue of
   the dependencies the app can use: the `DbContext`, Identity, MVC, Razor Pages.
2. **The middleware pipeline** (`app.Use...`) — I define the ordered list of steps
   every HTTP request passes through before it reaches my code: HTTPS redirect →
   routing → authentication → authorization → the endpoint.

**Q: What is dependency injection and where do you use it?**
It's a pattern where a class declares what it needs as constructor parameters, and
the framework supplies those objects from the service catalogue built at startup.
I never write `new ApplicationDbContext(...)` in a controller — I add
`ApplicationDbContext db` to the constructor and the framework injects a
request-scoped instance. This keeps classes decoupled from construction details
and makes them testable (I can pass a fake in a unit test). Same concept as
Spring's constructor injection.

**Q: What's a DbContext?**
It's EF Core's unit-of-work / session object. It exposes `DbSet<T>` properties
(one per table), tracks changes to the entities I load, and translates my LINQ
queries into SQL. Mine is `ApplicationDbContext`, and it inherits from
`IdentityDbContext<ApplicationUser>` so it also owns Identity's tables.

**Q: What are migrations and why not just auto-create the schema?**
A migration is a generated, timestamped C# file describing a schema change
(`CREATE TABLE`, `ADD COLUMN`, ...). `dotnet ef migrations add` writes the file by
diffing my entity classes against the last migration; `dotnet ef database update`
applies pending ones. It's version control for the database schema — the same
ordered changes can be replayed on every developer's DB and on production without
data loss. EF records applied migrations in the `__EFMigrationsHistory` table.
Auto-create is fine for a throwaway SQLite prototype but loses history and can't
safely evolve a populated production database.

**Q: How are passwords stored?**
I never store or see them. Identity hashes each password with PBKDF2 (a
deliberately slow, salted, one-way function) and stores only the hash in
`AspNetUsers.PasswordHash`. At login it hashes the submitted password the same
way and compares hashes. There is no way to recover the original password from
the database.

**Q: Why `ApplicationUser : IdentityUser` instead of using `IdentityUser` directly?**
So I can add app-specific fields later (e.g. `FirstName`) without fighting the
framework. Subclassing from day one — even when empty — avoids a painful
migration later.

**Q: Authentication vs authorization?**
Authentication = establishing *who* the user is (Identity reads the auth cookie,
populates `HttpContext.User`). Authorization = deciding *what* that user may do
(`[Authorize]`, role/policy checks). In the pipeline `UseAuthentication()` must
come before `UseAuthorization()` — you can't check permissions for someone you
haven't identified yet.

**Q: Where does the connection string live and how do you keep it out of source control?**
In `appsettings.json` for local dev (currently no password — local `trust` auth).
For anything sensitive, ASP.NET's layered configuration lets an environment
variable or the .NET user-secrets store override the file value with no code
change. Production secrets never go in committed files.

### What I should be able to do live

- Point to the two halves of `Program.cs` and name what each does.
- Add a `DbSet<T>`, create a migration, apply it, show the new table in `psql`.
- Explain why a controller constructor takes `ApplicationDbContext`.

### Honest limitations of this slice

- Email confirmation is disabled (no email service yet) — `RequireConfirmedAccount = false`.
- No tests yet: nothing here is my logic to test; it's framework configuration.
  Real tests start with the first business rule.
