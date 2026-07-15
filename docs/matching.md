# Matching — how a consultant is scored for a demand

The match is **deterministic and explainable** on purpose: in a hiring/allocation context, a
recommendation you can't justify is worthless. Every point in the score maps to a reason we can show
the user. An optional LLM layer only rewrites the explanation in nicer prose — it never changes the
decision.

## Score formula

The weights below are the **defaults**, and they are **administered from the Matching Settings
screen** (persisted, audited). The rule itself stays pure: it receives the weights as input, so the
operation can retune the algorithm without a code change.

Starting from `0`, for each candidate consultant against a demand:

| Rule | Points | Why |
|---|---|---|
| Consultant is `Available` | **+50** | Availability is the hardest constraint — an allocated person can't take the demand now. |
| Each required skill the consultant has | **+10** each | Direct technical fit. |
| Seniority meets or exceeds the required level | **+20** | Senior-enough is a gate; over-qualified still passes. |
| Consultant is already `Allocated` | **−30** | Strong penalty, but not a hard exclusion (they may free up). |

Consultants are returned **ranked by score, highest first**. `matchedSkills` and `missingSkills` are
listed so the user sees the gap, not just the number.

Seniority order for comparison: `Junior < Mid < Senior < Lead`.

## Example

Demand: *Senior .NET integration engineer* — required skills `[.NET, Angular, Kafka]`, required
seniority `Senior`.

Consultant *Ana* — `Senior`, `Available`, skills `[.NET, Angular, SQL Server]`:
- Available `+50`, meets seniority `+20`, has `.NET` `+10`, has `Angular` `+10` → **score 90**
- `matchedSkills: [.NET, Angular]`, `missingSkills: [Kafka]`

```json
{
  "consultantId": 3,
  "name": "Ana Souza",
  "score": 90,
  "matchedSkills": [".NET", "Angular"],
  "missingSkills": ["Kafka"],
  "explanation": "Strong technical fit, seniority matches, and available now. Missing: Kafka."
}
```

## Known limits (say these out loud — it reads as senior)

- No weighting of skills by importance (a missing core skill counts the same as a missing nice-to-have).
- No history/performance signal; no rate-vs-budget optimization.
- Skills matched by normalized string equality, not a taxonomy/synonyms ("React" ≠ "ReactJS").
- These are deliberate MVP cuts. The rule lives behind a service and is unit-tested, so evolving it
  (weights, embeddings-based similarity) is a localized change — not a rewrite.
