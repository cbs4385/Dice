# progress.md

The running log for agent work on Quintessence. **Update this every session.** It is the shared memory between sessions and supervisors — keep it current, terse, and honest.

- Append a new entry to the Session log at the top (reverse-chronological) whenever you do work.
- Move milestone rows through the tracker as their status changes.
- Log anything a human must decide under Open questions, and anything decided under Decisions.

See `AGENTS.md` for the rules of engagement and `docs/agent-build-plan.md` for the full plan.

Initialized: 2026-07-02

---

## Current status

**Milestone:** M0 — done. Starting M1 next.
**Overall:** Stack changed to Unity/C# (supervisor decision, see Decisions). Git repo initialized. Engine/Game/UI asmdefs scaffolded with compile-time purity + import-boundary enforcement, verified working. CI workflow scaffolded (not yet green — no remote/license secret).

## Milestone tracker

| # | Package | Status | Notes |
|---|---|---|---|
| M0 | Repo & tooling scaffold | Done | git init'd; Quintessence.Engine/Game/UI asmdefs created (noEngineReferences purity guard verified via a deliberate UnityEngine-reference canary that failed to compile as expected, then reverted); EditMode test assemblies wired and green (2/2); `.github/workflows/ci.yml` scaffolded but needs a git remote + Unity license secret from the supervisor to actually run |
| M1 | Engine core | Not started | Blocked on M0 (now clear). Canonical 28-point test must pass. |
| M2 | Game loop & state machine | Not started | Blocked on M1 |
| M3 | AI opponents (3 tiers) | Not started | Blocked on M2 |
| M4 | Presentation & input | Not started | Human-gated on feel/visuals. Blocked on draft-model decision. |
| M5 | Vertical slice | Not started | Human playtest sign-off is the gate |
| M6 | v1.0 content & systems | Not started | Human accepts accessibility & balance |
| M7 | Steam & shipping | Not started | Human-led; agent implements against mocks |

Status values: `Not started` · `In progress` · `Blocked` · `In review` (human-gated) · `Done`.

## Session log

### 2026-07-02 — stack changed to Unity/C#; M0 complete
- Discovered the repo is a Unity project (not the TS/pnpm scaffold the docs assumed) with UnityMCP connected; escalated per AGENTS.md ("architecture change"), supervisor chose Unity/C#.
- Rewrote `AGENTS.md` and `docs/agent-build-plan.md` (bumped to v0.2) for Unity/C#: `.asmdef`-based `Quintessence.Engine`/`Game`/`UI`, `noEngineReferences: true` + `references` lists as the compile-time purity/import-boundary guard, Unity Test Framework (NUnit) in place of Vitest/fast-check.
- `git init`'d the repo (none existed before), added a standard Unity `.gitignore`, committed the docs + existing Unity scaffold.
- M0: created `Quintessence.Engine`, `Quintessence.Game`, `Quintessence.UI` asmdefs and `Quintessence.Engine.Tests`/`Quintessence.Game.Tests` EditMode test asmdefs. Added `csc.rsp` (`-nullable:enable`, warnaserror on nullability codes) to Engine/Game.
- Verified the purity guard is real, not assumed: temporarily added a `UnityEngine.Debug.Log` call to the Engine assembly, confirmed it fails compile (`CS0103: The name 'UnityEngine' does not exist in the current context`), then reverted. Confirmed a clean compile afterward and both placeholder EditMode tests passing (2/2) via UnityMCP `run_tests`.
- Scaffolded `.github/workflows/ci.yml` (game-ci Unity EditMode test runner). It cannot go green yet — no git remote and no `UNITY_LICENSE` secret; that setup is on the supervisor.
- Verification: compile clean (batchmode via UnityMCP `refresh_unity`/`read_console`, zero errors/warnings), EditMode tests green (2/2). CI not yet run (see above — this is expected, not a failure).
- **Next:** M1 — implement the engine core per build-plan §5, including the canonical 28-point acceptance test.

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