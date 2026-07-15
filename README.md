# AllocationHub

[![CI](https://github.com/guimvolpe-droid/allocation-hub/actions/workflows/ci.yml/badge.svg)](https://github.com/guimvolpe-droid/allocation-hub/actions/workflows/ci.yml)

A staffing/allocation manager for a **software house**: register consultants, clients and client
demands, then **match** the best available consultants to a demand by skills, seniority and
availability — and turn a recommendation into an allocation.

Built to model the core problem an outsourcing / squads-as-a-service company solves every day.

**Stack:** Angular + Angular Material (frontend) · ASP.NET Core / .NET 8 Web API (backend) · EF Core +
SQLite · JWT auth. Architecture: **Clean Architecture** (dependencies point inward: `Api` →
`Infrastructure` → `Core`; `Core` depends on nothing).

## Why this design

- **Clean Architecture** keeps business rules (the matching score) independent of framework and
  database — the same reasoning transfers to any stack (Node/NestJS included). The matching rule lives
  in `Core`, is pure, and is **unit-tested** with no database.
- **The AI hook is honest.** Matching is **deterministic and explainable** (a transparent score). The
  human-readable explanation sits behind an interface (`IMatchExplanationService`) with an optional LLM
  implementation — so AI never sits on the critical path of a demo, and business rules are never coupled
  to an external vendor.
- **Real external sourcing.** From a demand you can source **real developer profiles** from the official
  **GitHub API** (behind `ICandidateSource`), ranked by the same matching rule. LinkedIn has no compliant
  people-search API, so GitHub is the honest real-data source for a developer-staffing tool.
- **Multi-LLM, switchable live.** The explanation can be rewritten by an LLM through a single
  OpenAI-compatible client that serves **Groq, OpenRouter, OpenAI and local Ollama** — chosen per request.
  Every output passes a **guardrail** (no hallucinated skills, no prompt-injection echo, bounded length)
  and falls back to the deterministic baseline on any failure. The guardrail runs in CI as an **eval gate**.

## Run it

### With Docker (whole system)

```bash
docker compose up --build      # then open http://localhost:8080
```

Optional: create a `.env` next to `docker-compose.yml` with `GROQ_API_KEY=...` (enables the live LLM
switch) and `GITHUB_TOKEN=...` (lifts the GitHub rate limit from 60 to 5000 req/h).

### Locally (dev)

```bash
# Backend (http://localhost:5080, Swagger at /swagger)
cd src/AllocationHub.Api
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet run

# Frontend (http://localhost:4200)
cd web
npm install
npm start
```

Demo login: **admin@demo.com** / **admin123** (seeded on startup).

## Demo flow (5 min)

Login as admin → operational dashboard → open a client demand → see the recommended ranking + score
explanation → allocate a consultant → back to the dashboard, utilization updated.
See `docs/demo-script.md`.

## Docs

- `specs/0001-allocationhub-mvp.md` — scope (in/out), entities, screens, endpoints.
- `docs/matching.md` — the score formula, example I/O, known limits.
- `docs/demo-script.md` — the literal presentation script.
