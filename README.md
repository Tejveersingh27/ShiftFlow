# ShiftFlow

A multi-tenant workforce scheduling and shift-management platform for businesses
that schedule hourly staff — retail, restaurants, warehouses, clinics, coffee shops.

> **Status:** in active development. This README describes the full V1 vision; the
> [Feature status](#feature-status) table below shows what is actually built today.

---

## The problem

Managers build schedules while juggling employee availability, roles, approved
time off, existing shifts, weekly-hour limits, and staffing needs. Employees need
an easy way to see their schedule, submit availability, request time off, and swap
shifts.

ShiftFlow is deliberately **not** a basic CRUD scheduler. The engineering focus is
on business rules, authorization, multi-tenancy, conflict detection, scheduling
constraints, and testing.

---

## Roles

Roles are **per organization** — the same account can be an Owner of one business
and an Employee at another.

| Role | Can do |
| --- | --- |
| **Owner** | Everything a Manager can, plus manage organization settings, manage managers, create departments, view reports/audit history. |
| **Manager** | View employees, create schedules and shifts, assign employees, see conflicts, publish schedules, approve/reject time-off and swaps, see workforce stats. |
| **Employee** | View own schedule, set weekly availability, request time off, request/respond to shift swaps, view notifications and the team schedule. |

Authorization is enforced **server-side**, not by hiding UI. One organization can
never read or modify another organization's data.

---

## Core domain

| Entity | Purpose |
| --- | --- |
| `ApplicationUser` | Authenticated user (ASP.NET Core Identity). |
| `Organization` | One business using ShiftFlow. |
| `OrganizationMember` | Links a user to an organization; holds role, and later department, max weekly hours, active status. |
| `Department` | Organizational unit (Kitchen, Front End, Warehouse…). |
| `Availability` | Recurring weekly availability for an employee. |
| `TimeOffRequest` | A request for dates off, with an approval workflow. |
| `Schedule` | A scheduling period (one week), with a DRAFT → PUBLISHED lifecycle. |
| `Shift` | A work shift within a schedule; may be assigned to an employee. |
| `ShiftSwapRequest` | A request to transfer a shift between employees, with workflow state. |
| `Notification` | In-app notification. |
| `AuditLog` | Record of important business operations. |

Entities are introduced slice by slice as features are built, not all up front.

---

## Scheduling rules

The system enforces these rules in the **service layer**, not in controllers or
views:

- An employee cannot be assigned to overlapping shifts.
- An employee is not assigned outside their declared availability (unless a
  manager explicitly overrides).
- An employee with approved time off is not assigned during that period.
- An employee must belong to the same organization as the schedule.
- An employee must satisfy the role a shift requires.
- An assignment must respect the employee's maximum weekly hours.
- Unauthorized users cannot create, modify, publish, approve, or delete resources
  outside their permissions.

### Conflict detection

A dedicated `SchedulingRulesService` is responsible for evaluating assignments:

`CheckForOverlappingShift` · `CheckAvailability` · `CheckApprovedTimeOff` ·
`CheckWeeklyHourLimit` · `CheckRequiredRole` · `CanAssignEmployee` ·
`GetEligibleEmployees`

The algorithm is deterministic and explainable.

---

## Schedule lifecycle

```
DRAFT ──(manager/owner publishes)──▶ PUBLISHED
```

- **DRAFT** — managers create and modify shifts freely.
- **PUBLISHED** — the schedule becomes the official one visible to employees.

Important modifications create audit records and notifications.

---

## Workflows

### Time off
`PENDING` → manager/owner reviews → `APPROVED` or `REJECTED`. Employees may cancel
an eligible pending request. Approved time off feeds conflict detection.

### Shift swap
`PENDING_EMPLOYEE` (recipient decides) → `PENDING_MANAGER` (manager decides) →
`APPROVED`. The assigned employee changes only after all validation rules pass.
Other states: `REJECTED`, `CANCELLED`, `EXPIRED`.

---

## Architecture

A **modular monolith** — no microservices, no message brokers, no CQRS/MediatR.
Understandable architecture over architecture theatre.

```
Browser
   │  HTTP
   ▼
Controllers/     HTTP concerns only — parse request, call a service, return View/JSON
   │
   ▼
Services/        business logic and rules (the tested core)
   │
   ▼
Data/            EF Core DbContext — LINQ to SQL
   │
   ▼
PostgreSQL
```

Supporting folders: `Models/Entities`, `ViewModels`, `Views`, `DTOs`,
`Authorization`, `Middleware`. Dependency injection throughout. Repository
abstractions are added only where they earn their place.

---

## Tech stack

| Area | Choice |
| --- | --- |
| Language / runtime | C#, .NET 10 |
| Web | ASP.NET Core MVC, Razor views |
| Auth | ASP.NET Core Identity (PBKDF2 hashing, cookie auth) |
| Data | Entity Framework Core, PostgreSQL, code-first migrations |
| Logging | Serilog (structured) |
| Testing | xUnit, Moq where appropriate |
| API docs | ASP.NET Core OpenAPI |
| Ops | Docker, GitHub Actions (build + test) |

Frontend is server-rendered (Razor + Bootstrap/custom CSS), styled to look like a
modern SaaS product — sidebar nav, dashboard cards, weekly calendar grid, status
badges, conflict indicators.

---

## Feature status

| Feature | Status |
| --- | --- |
| Authentication (register / login) | ✅ Done |
| Organizations + per-org roles | ✅ Done |
| Organization list / create | ✅ Done |
| Members — team list, add people, role-based authorization | 🔨 In progress |
| Departments | ⬜ Planned |
| Employee availability | ⬜ Planned |
| Weekly schedules (DRAFT → PUBLISHED) | ⬜ Planned |
| Shifts — create & assign | ⬜ Planned |
| Conflict-detection engine | ⬜ Planned (centerpiece) |
| Manager / employee dashboards | ⬜ Planned |
| Unit tests for scheduling rules | ⬜ Planned |
| Global error handling + Serilog | ⬜ Planned |
| JSON API endpoints + OpenAPI | ⬜ Planned |
| Docker + GitHub Actions + deploy | ⬜ Planned |
| Time-off (request + approve) | ⬜ Stretch |
| Shift swaps, notifications, audit log | ⬜ Future |

---

## Getting started

### Prerequisites
- .NET 10 SDK
- PostgreSQL running locally

### Setup

```bash
# 1. Configure the connection string
#    appsettings.json -> ConnectionStrings:DefaultConnection
#    default: Host=localhost;Port=5433;Database=shiftflow_dev;Username=<you>

# 2. Create the database schema
dotnet ef database update

# 3. Run
dotnet run
```

The app starts on `https://localhost:7173`. Register an account, then create an
organization.

### Tests

```bash
dotnet test
```

---

## Project structure

```
Controllers/       one class per resource; HTTP only
Services/          business logic (OrganizationService, MemberService, …)
Models/Entities/   database entities
Data/              ApplicationDbContext
Migrations/        EF Core migrations (generated)
ViewModels/        per-screen form models with validation
Views/             Razor views
docs/              architecture map, interview notes
```

---

## Notes

- `docs/interview-notes.md` — a per-slice study sheet explaining and defending each
  part of the build.
- Built incrementally in vertical slices; `main` stays runnable.
