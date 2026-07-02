# progress.md

The running log for agent work on Quintessence. **Update this every session.** It is the shared memory between sessions and supervisors — keep it current, terse, and honest.

- Append a new entry to the Session log at the top (reverse-chronological) whenever you do work.
- Move milestone rows through the tracker as their status changes.
- Log anything a human must decide under Open questions, and anything decided under Decisions.

See `AGENTS.md` for the rules of engagement and `docs/agent-build-plan.md` for the full plan.

Initialized: 2026-07-02

---

## Current status

**Milestone:** M0 — not started.
**Overall:** Design docs authored; no code yet. Repo scaffold is the next action.

## Milestone tracker

| # | Package | Status | Notes |
|---|---|---|---|
| M0 | Repo & tooling scaffold | Not started | Next up |
| M1 | Engine core | Not started | Blocked on M0. Canonical 28-point test must pass. |
| M2 | Game loop & state machine | Not started | Blocked on M1 |
| M3 | AI opponents (3 tiers) | Not started | Blocked on M2 |
| M4 | Presentation & input | Not started | Human-gated on feel/visuals. Blocked on draft-model decision. |
| M5 | Vertical slice | Not started | Human playtest sign-off is the gate |
| M6 | v1.0 content & systems | Not started | Human accepts accessibility & balance |
| M7 | Steam & shipping | Not started | Human-led; agent implements against mocks |

Status values: `Not started` · `In progress` · `Blocked` · `In review` (human-gated) · `Done`.

## Session log

### 2026-07-02 — project initialized
- Authored `docs/rulebook.md`, `docs/gdd.md`, `docs/agent-build-plan.md`, `AGENTS.md`, and this file.
- No source code exists yet.
- **Next:** M0 — create pnpm workspaces (`engine`, `game`, `ui`, `desktop`); wire lint (incl. import-boundary + engine-purity rules), typecheck, Vitest, build, and CI; get CI green on an empty scaffold.

**Entry template** (copy for each new session):

```
### YYYY-MM-DD — <short title>
- What I did:
- Verification (lint/typecheck/test/CI status):
- What's next:
- Blocked on (if anything):
```

## Open questions (need a human)

These block or shape work and must not be resolved by the agent:

1. **Draft model** — snake vs simultaneous same-seed draft. Blocks M4 UI and any netcode. Keep the draft model swappable until decided.
2. **Default information depth** — bag counts only, or bag counts + per-die odds (with the Oracle overlay available either way).
3. **Doc placement (housekeeping)** — the three companion docs were delivered as `quintessence-rulebook.md`, `quintessence-gdd.md`, and `quintessence-agent-build-plan.md`. Before an agent runs, move them into `docs/` and rename to the paths `AGENTS.md` expects (`docs/rulebook.md`, `docs/gdd.md`, `docs/agent-build-plan.md`). **Resolved 2026-07-02** — docs already present at the expected paths.
4. **STACK CONFLICT — RESOLVED 2026-07-02.** `agent-build-plan.md` v0.1 §3 mandated TypeScript/pnpm/Tauri, but the repo root is an existing Unity project (`Assets/`, `Packages/`, UnityMCP connected). Supervisor decision: **Unity/C#** (option B). `AGENTS.md` and `docs/agent-build-plan.md` have been rewritten (build plan bumped to v0.2) to target Unity 6000.3.6f1, C# with nullable reference types, and `.asmdef`-based `Engine`/`Game`/`UI` assemblies with `noEngineReferences: true` on `Engine`/`Game` as the compile-time equivalent of the purity + import-boundary lint rules. See Decisions ledger below.

## Decisions

Ledger of decisions and approvals. Record new ones with a date and, for human-gated items, who approved.

| Date | Decision | Source |
|---|---|---|
| 2026-07-02 | ~~Stack: TypeScript (strict) monorepo; `ui` = React + PixiJS; `desktop` = Tauri~~ — **superseded**, see below | agent-build-plan.md v0.1 §3 |
| 2026-07-02 | ~~Package manager: `pnpm` with workspaces~~ — **superseded**, see below | AGENTS.md v0.1 |
| 2026-07-02 | **Stack: Unity 6000.3.6f1 / C#**, supervisor decision, overriding the original TS/pnpm/Tauri plan (repo was already a Unity project with UnityMCP connected) | supervisor, this session |
| 2026-07-02 | Modularity: `.asmdef`-based `Quintessence.Engine`/`Quintessence.Game`/`Quintessence.UI` (+ `.Tests` assemblies) in place of npm workspaces; `noEngineReferences: true` + `references` lists on `Engine`/`Game` are the compile-time equivalent of the purity + import-boundary lint rules | agent-build-plan.md v0.2 §3–§4, AGENTS.md |
| 2026-07-02 | Testing: Unity Test Framework (NUnit, EditMode) in place of Vitest; property-style tests hand-rolled as seeded loops in place of fast-check (adding a property-testing NuGet lib would be a new dependency — not done without approval); determinism, purity, and the 28-point example remain required tests | agent-build-plan.md v0.2 §5, §8 |
| 2026-07-02 | Custom seeded PRNG in `Engine`; `UnityEngine.Random`/`System.Random`/`DateTime.*`/`Time.*` never used in `Engine`/`Game` | agent-build-plan.md v0.2 §3 |
| 2026-07-02 | CI: GitHub Actions Unity batchmode workflow will be scaffolded, but cannot go green without the supervisor providing a Unity license secret and a git remote — agent verifies locally in the meantime and will say so explicitly rather than imply CI ran | agent-build-plan.md v0.2 §3, §8 |