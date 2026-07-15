# Spec 0001 — AllocationHub MVP

> Estado: doing
> Goal: a runnable, demo-worthy staffing-allocation app (Angular + .NET) that models a software
> house's core problem, built spec-first. Buildable in ~7h; scope is deliberately cut.

## In scope

- JWT login (single seeded admin), route guard, "who am I".
- Dashboard: counters (consultants, available, allocated, open demands) + top open demands + available
  consultants.
- CRUD: Consultants, Clients, Demands.
- Matching: `GET /api/demands/{id}/matches` → ranked consultants with score + matched/missing skills +
  a short explanation.
- Allocations: list, create (from a match), end.

## Out of scope (cut on purpose)

Multi-tenant · signup · password reset · refresh tokens · complex RBAC · normalized Skill entity ·
calendar · timesheet · Kanban · comments · file upload · CSV import · Kafka · full CQRS/MediatR · event
sourcing · mandatory Docker · real vector DB · LangChain · chatbot · full E2E · custom design system.

## Entities

| Entity | Fields |
|---|---|
| `User` | Id, Name, Email, PasswordHash, Role (Admin) |
| `Consultant` | Id, Name, Email, Seniority (Junior/Mid/Senior/Lead), Location, Availability (Available/Allocated/Unavailable), HourlyRate, Skills (list) |
| `Client` | Id, Name, Industry, ContactName |
| `Demand` | Id, ClientId, Title, Description, RequiredSeniority, RequiredSkills (list), Status (Open/Allocated/Closed) |
| `Allocation` | Id, DemandId, ConsultantId, StartDate, EndDate, Status (Active/Ended) |

Skills are stored as a normalized string list on the entity (no separate `Skill` table in the MVP).

## Screens

Login · Dashboard · Consultants (list + create/edit/delete + filter) · Clients (list + CRUD) ·
Demands (list + CRUD + "view matches") · Match (ranking + score + explanation + allocate) ·
Allocations (list active + end).

## Endpoints

```
POST   /api/auth/login
GET    /api/auth/me
GET    /api/dashboard/summary
GET    /api/consultants            GET /api/consultants/{id}
POST   /api/consultants            PUT /api/consultants/{id}    DELETE /api/consultants/{id}
GET    /api/clients               GET /api/clients/{id}
POST   /api/clients               PUT /api/clients/{id}         DELETE /api/clients/{id}
GET    /api/demands               GET /api/demands/{id}
POST   /api/demands               PUT /api/demands/{id}         DELETE /api/demands/{id}
GET    /api/demands/{id}/matches
GET    /api/allocations           POST /api/allocations         POST /api/allocations/{id}/end
```

## Architecture

Clean Architecture, 3 projects + tests:
- `AllocationHub.Core` — entities, enums, DTOs, interfaces, **MatchingService** (pure, unit-tested).
- `AllocationHub.Infrastructure` — EF Core (SQLite), seed, password hashing, JWT, match-explanation impls.
- `AllocationHub.Api` — controllers, auth, DI, Swagger.
- `AllocationHub.Tests` — unit tests for the matching rule.

Dependency rule: `Api → Infrastructure → Core`; `Core` depends on nothing.

## Definition of done

Runs with one command · full flow works (login → dashboard → demand → match → allocate → dashboard
updates) · matching unit test green · Swagger opens · seed looks real.
