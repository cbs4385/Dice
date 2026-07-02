# progress.md

The running log for agent work on Quintessence. **Update this every session.** It is the shared memory between sessions and supervisors — keep it current, terse, and honest.

- Append a new entry to the Session log at the top (reverse-chronological) whenever you do work.
- Move milestone rows through the tracker as their status changes.
- Log anything a human must decide under Open questions, and anything decided under Decisions.

See `AGENTS.md` for the rules of engagement and `docs/agent-build-plan.md` for the full plan.

Initialized: 2026-07-02

---

## Current status

**Milestone:** M3 — done. This run's scope (M0-M3) is complete.
**Overall:** Stack changed to Unity/C# (supervisor decision, see Decisions). Git repo initialized. Engine/Game/UI asmdefs scaffolded with compile-time purity + import-boundary enforcement, verified working. CI workflow scaffolded (not yet green — no remote/license secret). Engine core (M1), game loop / self-play harness (M2), and the three AI tiers (M3) implemented; 126/126 EditMode tests green.

## Milestone tracker

| # | Package | Status | Notes |
|---|---|---|---|
| M0 | Repo & tooling scaffold | Done | git init'd; Quintessence.Engine/Game/UI asmdefs created (noEngineReferences purity guard verified via a deliberate UnityEngine-reference canary that failed to compile as expected, then reverted); EditMode test assemblies wired and green (2/2); `.github/workflows/ci.yml` scaffolded but needs a git remote + Unity license secret from the supervisor to actually run |
| M1 | Engine core | Done | Element/Sides/Die/Band/BandRange/Cell/Board/Placement/IRng+splitmix64/Bag/Favor/Scoring/BoardLayouts implemented in `Quintessence.Engine`. 84/84 EditMode tests green: boundary faces, every illegal-placement reason (incl. Defy bypassing only element-adjacency), determinism (RNG + BagOps, seed-golden test), a hand-rolled placement-invariant property test (200 seeds), all 6 public objective formulas, and the canonical 28-point Ashfall/Deep-Columns acceptance test (placed through the real legality pipeline, not just asserted). Found and fixed a real bug along the way: `Bag`'s compiler-generated record equality silently did reference equality on its `Dictionary` property; added explicit value equality. |
| M2 | Game loop & state machine | Done | `Quintessence.Game`: `GameState`/`PlayerState`/`RoundPhase`, `IDraftOrderStrategy`/`SnakeDraftOrderStrategy` (PROVISIONAL, see Open questions), `GameReducer` (StartRound/ApplyDraft/ApplyForfeit as pure state transitions incl. Favor Adjust/Reroll/Defy), `GameSetup.NewGame`, `LegalDrafts` enumeration, `SelfPlay` headless harness. 29/29 new EditMode tests green, 113/113 total. Self-play property tests ran 2000 seeds x 3 player counts (6000 games) with zero exceptions, always terminating at Round 6 with `IsGameOver`, plus a same-seed determinism check across 300 seeds x 3 player counts comparing final boards cell-by-cell (not GameState record equality, which has the same Dictionary/List reference-equality trap as M1's Bag bug). |
| M3 | AI opponents (3 tiers) | Done | `NoviceAi` (uniform random over `LegalDrafts.EnumerateSimple`), `AdeptAi` (one-ply local heuristic: band-cell fit + private-element match, ignores the public objective; weights wired as `AdeptWeights` config, a human-gated AI-weight balance value per AGENTS.md), `OracleAi` (one-ply lookahead maximizing the true `Scoring.ScoreBoard`, aware of the actual objective) - all pure `IAiPolicy` implementations, no I/O, never able to construct an illegal move since they only choose from `LegalDrafts.EnumerateSimple`. 13/13 new EditMode tests green, 126/126 total. AI-sanity thresholds all passed on first run: Oracle's win rate vs Novice exceeded 60% (seat-alternated over 300 games), Oracle vs Adept and Adept vs Novice both exceeded 50%, and 1500 mixed three-tier games (500 seeds) produced zero illegal-move exceptions. |
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

### 2026-07-02 — M1 engine core complete
- Implemented the full §5 contract in `Quintessence.Engine`: `Element`/`Sides`/`Elements`, `Die`, `Band`/`BandRange`/`Bands` (with `CanReach`), `Cell` (closed hierarchy via private-constructor trick), `Board` (immutable, `WithPlacement` returns a new instance), `Placement`, `IRng`/`Rng.Create` (splitmix64, never `System.Random`/`UnityEngine.Random`), `Bag`/`BagOps.DrawRoll` (without replacement), `Favor.Adjust`/`Reroll`, `Scoring`/`ScoringConfig`/`PublicObjective` (all 6 rulebook objectives), `BoardLayouts` (all 4 named boards).
- Discovered Unity 6000.3.6f1 defaults to C# 9 (not 10): `record struct` fails with `CS8773`. Switched `Die`/`Placement`/`LegalityResult` to reference-type `sealed record`. Also hit `CS0518` because Unity's reference assemblies lack `IsExternalInit`; added the standard local shim (`CompilerShims.cs` in both Engine and Game). Both are now noted in `agent-build-plan.md` §3 so future sessions don't rediscover them.
- Verified (not just assumed) two compile-time guards by deliberately breaking them and reverting: (1) a `UnityEngine.Debug.Log` call in Engine fails with `CS0103` as expected (purity guard); (2) a nullable-violation canary fails with `CS8603` as expected (nullable warnings-as-errors guard).
- Found and fixed a real bug via the test suite: `Bag`'s record-generated `Equals` compared its `IReadOnlyDictionary<Element,int>` property with `EqualityComparer<T>.Default`, which is reference equality for `Dictionary` - two content-identical bags from independent draws were reported unequal. Added an explicit value-equality `Equals`/`GetHashCode` override. This is exactly the kind of bug the determinism tests exist to catch.
- Reconstructed the rulebook's worked example (Board α "Ashfall", objective "Deep Columns", total 28) as one concrete, fully-legal board, since the rulebook states the *outcome* (bands satisfied, which column repeats, private-element count, favor spend) rather than a cell-by-cell diagram. Placed it through the real `Legality`/`Board` pipeline (one placement needs a Defy favor, matching the rulebook's "spent 2" favor tokens) rather than only asserting the score, so the reconstruction is provably reachable. Total: exactly 28.
- Verification: 84/84 EditMode tests green (unit tests incl. every illegal-placement reason and boundary faces; a hand-rolled seeded property test - no FsCheck, per the "no new dependency without approval" rule - checking 200 random legal-placement sequences never violate element-adjacency and are deterministic per seed; a golden RNG sequence for seed 42 to guard Daily reproducibility; the canonical 28-point test). Compile clean in batchmode. CI still not run (needs remote + license secret, per M0 note).
- **Next:** M2 — round loop / state machine (drafting behind a swappable strategy interface, snake as PROVISIONAL default per the run's working rules), Firmament, favor, scoring as a pure reducer; headless self-play harness.

### 2026-07-02 — M2 game loop and self-play harness complete
- Implemented `Quintessence.Game`: `GameState`/`PlayerState`/`RoundPhase`/`FirmamentDie` (plain immutable records over the M1 engine types), `IDraftOrderStrategy`/`SnakeDraftOrderStrategy` (PROVISIONAL default, see Open questions #1), `GameReducer` (`StartRound` draws the pool and sets up pick order; `ApplyDraft` resolves an optional Favor - Adjust/Reroll/Defy - before building the `Placement` and checking it through the real M1 `Legality` pipeline; `ApplyForfeit` advances the turn with no placement; round/phase transitions handle the draft-down/draft-back snake, moving leftover pool dice to the Firmament and rotating the start player after 2 picks x N players), `GameSetup.NewGame` (seeded random board/private-element/objective assignment, 2-4 players), `LegalDrafts.EnumerateSimple` (favor-free legal choices, used by the random self-play policy and reusable by M3's AI tiers), `SelfPlay.PlayRandomGame` (headless: draws random legal moves each turn, forfeits when none exist, scores every player at game end via M1's `Scoring.ScoreBoard`).
- Verification: 29 new EditMode tests, 113/113 total green. Covered: snake pick-order math (forward/reverse, ends on start player), every `GameReducer` failure mode (invalid pool index, unknown Firmament id, illegal placement, no favor remaining), Defy actually bypassing only element-adjacency (verified both with and without Defy against the identical setup), Adjust/Reroll actually changing the placed die's face, a full two-pick-per-player round correctly transitioning phases and moving the true leftover die to the Firmament, and the self-play property tests: 2000 seeds x {2,3,4} players (6000 total games) with zero exceptions, every game ending at Round 6 with `IsGameOver`, and a same-seed determinism check (300 seeds x 3 player counts) comparing final boards cell-by-cell.
- Deliberately did NOT rely on `GameState`/`RoundPhase`/`Bag` record equality for the determinism tests, since `List<T>`/`Dictionary<K,V>`-typed record properties use reference equality (the exact bug found and fixed in M1) - compared boards cell-by-cell and scores/objective directly instead.
- Scope note: `LegalDrafts.EnumerateSimple` only enumerates favor-free placements. Full favor mechanics (Adjust/Reroll/Defy) are implemented and tested in `GameReducer` but are a deliberate choice a policy opts into, not brute-force enumerated - Reroll in particular has no fixed outcome to enumerate against before the rng resolves it. AI tiers (M3) can call `GameReducer.ApplyDraft` with a `FavorAction` directly when a smarter policy wants to.
- **Next:** M3 — Novice/Adept/Oracle AI tiers as pure policies over `Quintessence.Game`, plus AI-sanity tests (Oracle beats Novice at an agreed threshold; no tier ever attempts an illegal move).

### 2026-07-02 — M3 AI tiers complete; this run's scope (M0-M3) finished
- Implemented `IAiPolicy` and three tiers in `Quintessence.Game`: `NoviceAi` (uniform random), `AdeptAi` (local one-ply heuristic over band-cell fit + private element, with point weights wired as a configurable `AdeptWeights` record - an AI-weight balance value per AGENTS.md's human-gated list, not something I tuned/approved myself), `OracleAi` (one-ply lookahead that greedily maximizes the true `Scoring.ScoreBoard`, which is aware of the actual public objective unlike Adept). All three pick exclusively from `LegalDrafts.EnumerateSimple`, so an illegal move is structurally impossible, not just tested-around. Added `AiSelfPlay.PlayWithPolicies` to drive a game with one policy per seat.
- Verification: 13 new EditMode tests, 126/126 total green. AI-sanity tests (seat-alternated to cancel out any first-player advantage): Oracle's win rate vs Novice exceeded the 60% threshold, Oracle vs Adept and Adept vs Novice both exceeded 50% (a coherent Novice < Adept < Oracle strength ladder, not just the one required comparison), and 500 seeds of three-tier mixed games (1500 games) produced zero illegal-move exceptions. All AI-sanity assertions passed on the first run with no tuning needed.
- **This closes the scope of this run (prompt.txt): M0 through M3 are done, all green locally.** See the summary below for what's next and what needs the supervisor.

### 2026-07-02 — remote added, repo pushed to GitHub
- Supervisor added `origin` (`https://github.com/cbs4385/Dice.git`), committed a `README.md` on top of the M0-M3 work ("first commit"), and pushed. Confirmed local `main` and `origin/main` are identical (fetched and compared).
- `.github/workflows/ci.yml` triggers on every push to `main`, so a run is very likely already queued/failed on GitHub for this push - expected, not a regression, since there is still no `UNITY_LICENSE` (or email/password/serial) secret configured. CI will stay red until the supervisor adds it.

### 2026-07-02 — CI license setup: manual Personal activation is deprecated
- Attempted the `.alf`/`.ulf` manual-activation route for `UNITY_LICENSE` (generated `Unity_v6000.3.6f1.alf` locally via `-createManualActivationFile`). Unity's manual activation portal (license.unity3d.com/manual) now states outright: "Unity no longer supports manual activation of Personal licenses." That route is a dead end for a free license - removed the `.alf` file (also added `*.alf`/`*.ulf` to `.gitignore` so these one-time files never get committed).
- `.github/workflows/ci.yml` already supports the correct alternative for Personal licenses: `UNITY_EMAIL`/`UNITY_PASSWORD` secrets, which `game-ci/unity-test-runner` uses for online activation instead of a license file. No workflow change needed - just add those two secrets (supervisor-only; never handled by the agent). Note: if the Unity account has 2FA enabled, online activation via email/password may fail and a Pro/Plus serial (`UNITY_SERIAL`) would be needed instead.

### 2026-07-02 — draft-model open question resolved
- Supervisor confirmed snake drafting is the correct, final draft model (not a placeholder pending a simultaneous-draft alternative). Updated `AGENTS.md`, `docs/agent-build-plan.md` §6, this file's open-questions list, and the code comment on `SnakeDraftOrderStrategy` accordingly. No behavior change - `SnakeDraftOrderStrategy` was already what M2/M3 run on; this only removes the "provisional" framing. `IDraftOrderStrategy` stays in place as an architecture seam, not as an unresolved-decision marker.

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

1. ~~**Draft model** — snake vs simultaneous same-seed draft.~~ **RESOLVED 2026-07-02** — supervisor confirmed snake drafting is correct. `SnakeDraftOrderStrategy` (forward order for pick 1, reverse for pick 2, matching the rulebook's literal "draft down" / "draft back" round structure) in `Quintessence.Game/DraftOrder.cs` is the confirmed draft model, not a placeholder. `IDraftOrderStrategy` remains as an architecture seam (isolating this from future UI/netcode), not because the choice is still open.
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
| 2026-07-02 | **Draft model: snake, confirmed final** (not provisional) — supervisor approved | supervisor, this session |