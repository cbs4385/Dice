# AGENTS.md

Operating rules for coding agents working in this repository. Read this every session. It is the quick reference; **`docs/agent-build-plan.md` is the full plan and is authoritative.** If anything here is unclear or seems to conflict with the canonical docs, stop and ask (see Escalation).

## What this is

Quintessence is a turn-based dice-placement game for PC (Steam), built as a **Unity project (C#)**. Players draft the five Platonic-solid dice and place them into a 3×4 board so their faces land in the right value bands.

**Stack decision (2026-07-02):** the project was originally drafted for a TypeScript/pnpm/Tauri monorepo (see `docs/agent-build-plan.md` history). The supervisor overrode this — the repo is a Unity project with UnityMCP already wired up, and Unity/C# is the actual target. This file and the build plan reflect that decision. Do not revisit it without approval.

**Canonical docs — do not re-derive their contents from memory:**
- `docs/rulebook.md` — the game rules. Source of truth for all mechanics and scoring. (Stack-agnostic; unaffected by the Unity decision.)
- `docs/gdd.md` — product scope, modes, UX. (Stack-agnostic.)
- `docs/agent-build-plan.md` — the build plan: milestones, the engine implementation contract, verification strategy, human-gated checkpoints.
- `docs/progress.md` — the running log. **You maintain this every session.**

## Two rules that override everything

1. **Engine before pixels, tests before engine.** Never build presentation for logic that isn't implemented and tested.
2. **You do not sign off on subjective quality.** Feel, art, audio, and balance values are human-gated. You may scaffold them; you may not decide they are good and move on.

## Golden rules

- **Small, verified increments.** One task = one focused change + its tests + green checks. Many small commits beat one large one.
- **The engine is pure.** No `UnityEngine.Random`, no `System.Random`, no `DateTime.Now`/`UtcNow`, no `Time.*`, no file/network I/O, and **no reference to `UnityEngine` at all**, in the `Quintessence.Engine` or `Quintessence.Game` assemblies. Randomness and time are injected. This is enforced at *compile time* via `"noEngineReferences": true` in their `.asmdef` files — do not remove that flag.
- **Determinism is required.** Same seed + same action sequence ⇒ byte-identical result. Daily mode, replays, and leaderboards depend on it.
- **Integers only in scoring.** No floating-point where a score is computed or compared.
- **Tests encode the rules.** A red rules test means the code is wrong, not the test. Never weaken or delete a rules test to go green — escalate the discrepancy.
- **Stay in scope.** Build only the current milestone. No features or polish from later milestones.
- **Report as you go.** Update `docs/progress.md`: current milestone, tasks done, tasks blocked, open questions.

## Commands

Unity Editor **6000.3.6f1** is pinned (`ProjectSettings/ProjectVersion.txt`) — do not upgrade without approval. There is no package manager / CLI build tool the way `pnpm` would provide one; the equivalents are:

```
Open the project in Unity Editor 6000.3.6f1     # normal dev loop; Play mode not needed until M4+

# Compile check ("typecheck" equivalent — C# is compiled, so a clean compile IS the type check)
Unity.exe -batchmode -quit -projectPath . -logFile -
  (or, in an agent session: UnityMCP `refresh_unity` + `read_console` to confirm zero compiler errors)

# Tests ("pnpm test" equivalent — Unity Test Framework / NUnit, EditMode)
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults.xml -logFile -
  (or, in an agent session: UnityMCP `run_tests` tool)

# Purity + import-boundary ("pnpm lint" equivalent)
Enforced at compile time, not by a separate linter:
  - Quintessence.Engine.asmdef: "noEngineReferences": true, "references": []
  - Quintessence.Game.asmdef:   "noEngineReferences": true, "references": ["Quintessence.Engine"]
  A compiler error (unresolved UnityEngine symbol, or unresolved cross-assembly type) IS the lint failure.

# Build (meaningful once there is a Player to build, M4+)
Unity.exe -batchmode -quit -projectPath . -executeMethod BuildScript.Build -logFile -
```

**Before every commit, this must pass:** a clean batchmode compile + all EditMode tests green. Do not commit or merge red.

## Repo map

```
Assets/Scripts/Engine   PURE rules core: types, bands, board, legality, scoring, seeded RNG, bag
                        asmdef: Quintessence.Engine  (noEngineReferences: true, references: [])
Assets/Scripts/Game     round loop, state machine, Firmament, self-play harness, AI tiers
                        asmdef: Quintessence.Game    (noEngineReferences: true, references: [Quintessence.Engine])
Assets/Scripts/UI       MonoBehaviours, uGUI/UI Toolkit, rendering, input, feel  (human-gated, M4+)
                        asmdef: Quintessence.UI      (references: [Quintessence.Game, Quintessence.Engine])
Assets/Tests/EditMode   NUnit test assemblies, mirroring Engine/Game (Quintessence.Engine.Tests, Quintessence.Game.Tests)
```

Dependency direction is one-way: `UI` → `Game` → `Engine`. **`Engine` and `Game` must not reference `UnityEngine`, or each other in reverse.** Enforced by `asmdef` config, not a lint plugin.

## Hard constraints

- C# with nullable reference types enabled (warnings-as-errors) in `Engine` and `Game`.
- Seeded PRNG only; never the platform RNG (`UnityEngine.Random`, `System.Random`, `Guid.NewGuid()`). RNG and clock are injected, never called directly in `Engine`/`Game`.
- Scoring returns integers (`int`/`long`; never `float`/`double` on a scoring path).
- No new runtime dependency (NuGet package or Unity package in `Packages/manifest.json`) without approval.
- No architecture or public-engine-API change without approval.
- Conventional Commits; feature branches; small PRs scoped to one task.

## Do not decide these yourself (human-gated)

Scaffold, then request review — never silently choose:

- **Feel / juice** (animations, timing, attune flash + sound), **art**, **audio**.
- **Balance values** (bag composition, band-cell points, empty-cell penalty, favor count, AI weights) — wire as config with defaults; a human sets and playtest-approves the numbers.
- **The remaining open design fork:** default information depth (bag counts vs counts + odds). The draft model was resolved 2026-07-02 — snake is the confirmed choice, not just a provisional default (see `docs/progress.md`). If the supervisor hasn't specified information depth, escalate before building anything that depends on it.
- **Steam integration** — which Unity Steamworks wrapper to use (e.g. Steamworks.NET vs Facepunch.Steamworks) is an undecided, human-gated dependency choice. Implement against a mocked bridge interface; never touch real credentials.

## Definition of done (per task)

Contract implemented + tests covering it (incl. boundaries and every failure case) + clean batchmode compile + all EditMode tests green + `progress.md` updated. For engine work, the canonical **28-point acceptance test** from the rulebook worked example must remain passing.

## Escalation

Stop and ask on: rules ambiguity, a rules contradiction surfaced by a test, an undrafted design fork, a new dependency, an architecture change, or three failed verification attempts on the same task. Use:

```
BLOCKED: <one-line summary>
Milestone/Task: <id>
What I need: <the specific decision or approval>
Options: <A / B / C, with the tradeoff of each>
Recommendation: <one, with a one-sentence reason>
Done so far: <links to commits / branch>
```

Do not thrash. When in doubt, ask.
