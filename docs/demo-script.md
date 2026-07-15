# Demo script — AllocationHub (~5 min)

The one narrative to tell. Practice it out loud once; it's the difference between "a CRUD app" and
"someone who understood our business."

## Setup (before the call)
```bash
# Terminal 1 — API
cd src/AllocationHub.Api && DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet run
# Terminal 2 — web
cd web && npm start
```
Open http://localhost:4200. (To reset demo data: stop the API, delete `src/AllocationHub.Api/allocationhub.db`, run again.)

## The story (follow the arrow: demand → match → allocate)

1. **Frame it (10s).** "This models the core problem of a software house like yours: matching the
   right available people to a client demand. Let me show the whole loop."

2. **Login.** admin@demo.com / admin123. "JWT auth, route guard on the back-office."

3. **Dashboard.** "Operational view — how many consultants, who's available, who's allocated, open
   demands. These are the numbers a delivery manager lives by."

4. **Open a demand → Matches** (the ".NET integration engineer" one). "The client needs a Senior with
   .NET, Kafka, SQL Server. The system ranks the bench with a **transparent score** — availability,
   seniority fit, matched skills. Gustavo scores 100: full fit, available. Ana scores 90 — same
   seniority, but **missing Kafka**, and it says so. No black box."

5. **The design decision to name (this is the senior signal).** "The score is **deterministic** on
   purpose — a recommendation you can't justify is useless in staffing. The human-readable explanation
   sits behind an interface, so I can swap in an LLM to phrase it more naturally **without touching the
   business rule**. AI adds polish; it never sits on the critical path."

6. **Allocate.** Click Allocate on the top match. "One click turns a recommendation into an allocation
   — the consultant is now busy, the demand is closed."

7. **Back to dashboard.** "Available dropped, allocated went up, the open demand is gone. The loop is
   closed."

## If they go technical
- **Architecture:** "Clean Architecture — the matching rule is in the core, pure, framework- and
  DB-free, and unit-tested. `Api → Infrastructure → Core`; the core depends on nothing. Same reasoning
  transfers to any stack, including Node."
- **Why SQLite:** "Zero-setup for a demo; the EF Core model is provider-agnostic — point it at
  SQL Server or Postgres by changing one line."
- **What I cut and why:** "No Kafka, no CQRS, no microservices here — they'd be over-engineering for
  this scope. I'd add them when the pain justifies it, not before."
- **What's next:** "Weight skills by importance; embeddings-based skill similarity (React ≈ ReactJS);
  the LLM explanation implementation. All localized changes because the rule is isolated."
