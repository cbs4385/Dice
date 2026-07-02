# QUINTESSENCE — Agent Build Plan
### Operating spec for an autonomous coding agent

*Version 0.2 · Companion to the Game Design Document (v0.2) and the tabletop rulebook (v0.1), which are canonical for game rules and product intent.*

*Stack revised 2026-07-02: the supervisor selected Unity/C# over the originally-drafted TypeScript/pnpm/Tauri stack (§3 v0.1), because the repo already exists as a Unity project with UnityMCP tooling in place. This version supersedes §3–§5 wording of v0.1; the rules, milestones, and human-gated boundaries are unchanged.*

---

## 0. How to use this document

You are an autonomous coding agent (with a human supervisor) building the PC game Quintessence. This document is your operating context and your plan. It is deliberately opinionated: where the GDD offered choices, this document has already **decided**, because ambiguity is where autonomous work goes wrong.

Read this whole document before writing code. It lives in the repo as `docs/agent-build-plan.md`, with a short `AGENTS.md` at the root pointing to it and restating the operating principles in §2.

**Two rules override everything else in this document:**
1. **Engine before pixels, tests before engine.** Never build presentation for a system whose logic isn't implemented and tested.
2. **You do not sign off on subjective quality.** Feel, art, audio, and balance values are human-gated (§6). You may scaffold them; you may not decide they are "good" and move on.

---

## 1. Product context (brief)

Quintessence is a turn-based dice-placement game for Steam (Windows/Linux/Steam Deck). Players draft the five Platonic-solid dice — each an element with its own value range — and place them into a 3×4 board so their faces land in the right value bands. Full rules are in the rulebook; product scope, modes, and UX are in the GDD. **Do not re-derive the rules from prose in this file — the rulebook is the source of truth, and §5 below restates the core as testable contracts.**

---

## 2. Operating principles (your guardrails)

- **Work in small, independently verifiable increments.** One task = one focused change + its tests + a green compile/test pass. Prefer many small commits over large ones.
- **The engine is pure.** No `UnityEngine.Random`, no `System.Random`, no `DateTime.Now`, no `Time.*`, no file/network I/O, and no reference to `UnityEngine` at all, in `Quintessence.Engine` or `Quintessence.Game`. Randomness and time are injected. This is enforced by `"noEngineReferences": true` in both assemblies' `.asmdef` files — a compile error if violated, not just a lint warning.
- **Tests encode the rules.** A failing rules test means the code is wrong, not the test. You may not weaken or delete a rules test to make it pass. Changing a rules test requires human approval and a matching rulebook update.
- **Determinism is a hard requirement.** Given the same seed and the same action sequence, the game must produce byte-identical results. Daily mode, replays, and leaderboards depend on this.
- **Integers only in scoring.** No floating-point anywhere a score is computed or compared.
- **Stay in scope.** Build what the current milestone specifies. Do not add features, modes, or polish from later milestones.
- **Ask before:** adding any new dependency (NuGet package or Unity package), changing the architecture or the public engine API, resolving a rules ambiguity, or making any decision this document leaves open. See §7 for the escalation format.
- **Don't thrash.** If a task fails verification three times, stop and escalate with what you tried and what you observed. Do not keep retrying blindly.
- **Report as you go.** Maintain `docs/progress.md`: current milestone, tasks done, tasks blocked, open questions. Update it every work session.

---

## 3. Technology decisions (decided — do not revisit without approval)

- **Engine:** Unity **6000.3.6f1** (pinned in `ProjectSettings/ProjectVersion.txt`). Do not upgrade without approval.
- **Language:** C# 9 (Unity 6000.3.6f1's default `langversion`; confirmed by hitting `CS8773` on `record struct`, which needs C# 10). Use reference-type `record`/`sealed record`, not `record struct`. Nullable reference types are enabled and treated as warnings-as-errors in `Quintessence.Engine` and `Quintessence.Game` via a `csc.rsp` in each folder (`-nullable:enable -warnaserror+:CS8600,...`) - verified live by temporarily introducing a nullable violation and confirming it fails compile. Unity's reference assemblies also predate `System.Runtime.CompilerServices.IsExternalInit` (needed for record `init` accessors); each assembly that uses records carries a small local shim type of that name (see `CompilerShims.cs`) rather than pulling in a package for it.
- **Modularity:** Unity Assembly Definitions (`.asmdef`), not a package-manager monorepo: `Quintessence.Engine`, `Quintessence.Game`, `Quintessence.UI` (+ mirrored `.Tests` assemblies). See §4.
- **Purity/import-boundary enforcement:** `Quintessence.Engine.asmdef` has `"noEngineReferences": true` and `"references": []`. `Quintessence.Game.asmdef` has `"noEngineReferences": true` and `"references": ["Quintessence.Engine"]`. Neither can physically reference `UnityEngine` or `Quintessence.UI` — the compiler enforces the boundary that a lint rule would enforce in a JS/TS repo.
- **Rendering (later, M4+):** `Quintessence.UI`, uGUI and/or UI Toolkit (already present via `com.unity.ugui`/`com.unity.modules.uielements`). Depends on `Game` and `Engine`; **`Engine` and `Game` depend on nothing in `UI`**.
- **Desktop packaging (later, M7):** Unity Player build (Windows/Linux standalone), Steam Deck Verified pass. Steamworks integration via a wrapper TBD (Steamworks.NET or Facepunch.Steamworks) behind an interface, isolated so the game runs without Steam in the editor/CI. Wrapper choice is a human-gated dependency decision (§6).
- **Test runner:** Unity Test Framework (`com.unity.test-framework`, already installed), NUnit-based, EditMode tests (no scene/Play mode needed for `Engine`/`Game`, which have no `UnityEngine` dependency at all). Property-style invariant tests are hand-rolled as loops over many seeds rather than pulling in a property-testing library (e.g. FsCheck) — adding that library is a new dependency and needs approval first; note it in `progress.md` as a future option rather than adding it silently.
- **Tooling:** Unity batchmode for CI-style compile/test runs; `.editorconfig` for C# style. Conventional Commits, feature branches, small PRs.
- **CI:** GitHub Actions running Unity in batchmode (e.g. `game-ci/unity-test-runner`) requires a Unity license activation secret (`UNITY_LICENSE` or email/password/serial) that only the supervisor can provide, plus a git remote to push to. The agent scaffolds the workflow file but cannot make it green end-to-end alone — note this explicitly in `progress.md` rather than claiming CI is green when it hasn't actually run.
- **RNG:** a small, seeded, deterministic PRNG hand-written in `Quintessence.Engine` (e.g. splitmix64/xoshiro256**, with a fixed, tested algorithm — not `System.Random`, whose output is not guaranteed stable across .NET versions/platforms, and never `UnityEngine.Random`).

Rationale (for the supervisor, not a decision to reopen): the original plan (v0.1) chose TypeScript/pnpm/Tauri to maximize agent-provable correctness in a headless stack. That rationale still holds — it's why `Engine`/`Game` are built as `UnityEngine`-free assemblies here too, just inside Unity's `.asmdef` system instead of npm workspaces. The change to Unity/C# reflects the repo's actual starting state (a scaffolded Unity project with UnityMCP already connected) rather than a re-evaluation of the original goals.

---

## 4. Project structure

```
DiceGame/                              # Unity project root
├─ AGENTS.md                           # operating principles, points here
├─ docs/
│  ├─ agent-build-plan.md              # this file
│  ├─ rulebook.md                      # canonical rules
│  ├─ gdd.md                           # canonical product scope
│  └─ progress.md                      # you maintain this
├─ Assets/
│  ├─ Scripts/
│  │  ├─ Engine/                       # PURE. types, bands, board, legality, scoring, rng, bag
│  │  │  └─ Quintessence.Engine.asmdef
│  │  ├─ Game/                         # round loop, state machine, firmament, self-play, AI tiers
│  │  │  └─ Quintessence.Game.asmdef
│  │  └─ UI/                           # rendering, input, feel (human-gated, M4+)
│  │     └─ Quintessence.UI.asmdef
│  └─ Tests/
│     └─ EditMode/
│        ├─ Engine/  Quintessence.Engine.Tests.asmdef
│        └─ Game/    Quintessence.Game.Tests.asmdef
├─ ProjectSettings/, Packages/, Library/, ...   # standard Unity project files
└─ .github/workflows/ci.yml             # scaffolded; needs a remote + Unity license secret to run
```

---

## 5. The deterministic core (implementation contract)

This is the unambiguous target for `Quintessence.Engine`. The C# below is illustrative; the *contracts* are binding. The rulebook is authoritative if anything here conflicts with it — if you find a conflict, **stop and escalate** rather than guessing.

```csharp
public enum Element { Fire, Earth, Air, Aether, Water }

public static class Sides
{
    // Fire=4, Earth=6, Air=8, Aether=12, Water=20
    public static int Of(Element element);
}

public sealed record Die(Element Element, int Face); // 1..Sides.Of(Element)

public enum Band { Low, Mid, High, Celestial }

public static class BandRange
{
    // Low:[1,4], Mid:[5,8], High:[9,12], Celestial:[13,20]
    public static (int Min, int Max) Of(Band band);
}

public abstract record Cell
{
    public sealed record ElementCell(Element Element) : Cell;
    public sealed record BandCell(Band Band) : Cell;
    public sealed record WildCell : Cell;
}

public sealed class Board
{
    public required Cell[,] Cells { get; init; }   // 3 rows x 4 cols
    public required Die?[,] Dice { get; init; }
}

public sealed record Placement(int Row, int Col, Die Die);

public interface IRng { int NextInt(int maxExclusive); }
public static class Rng { public static IRng Create(long seed); }   // deterministic

public sealed record Bag(IReadOnlyDictionary<Element, int> Remaining);
public static class BagOps
{
    public static (IReadOnlyList<Die> Dice, Bag Bag) DrawRoll(Bag bag, IRng rng, int count);
}
```

**Binding contracts (each becomes a test):**

- `BandOf(face)` maps 1–4→Low, 5–8→Mid, 9–12→High, 13–20→Celestial. A die can occupy a band cell only if it *can roll* into that range; a die "attunes" (scores) only if its current face is in range.
- `IsLegalPlacement(board, placement)` returns ok only if: the cell is empty; after the first die, the target is orthogonally or diagonally adjacent to an existing die; the die is **not** orthogonally adjacent to another die of the same element (diagonal is allowed); and the cell lock is respected (element cell → matching element; band cell → any die; wild → any die).
- `ScoreBoard(...)` returns an **integer**: +4 per band cell whose die is in-band; the public objective as specified; +2 per die of the private element; +1 per unspent favor; −2 per empty cell.
- Favor actions: `Adjust ±1` (clamped to 1..Sides, no wrap), `Reroll` (uses injected RNG), `Defy` (ignore element-adjacency once). Each spent favor decrements the count.
- The bag draws **without replacement**; `DrawRoll` rolls each drawn die's face via the injected RNG.
- The full game is a pure reducer: `Reduce(state, action, rng) -> state`. Same inputs → identical outputs, always.

**Canonical acceptance test (must pass):** encode the rulebook's worked example — Board α (Ashfall), objective "Deep Columns", the described placements — and assert the total is exactly **28**. If your implementation disagrees with the rulebook, the rulebook wins; escalate the discrepancy.

---

## 6. Human-gated checkpoints (you scaffold, a human decides)

You may build structure and wiring for all of these, but a human must review and approve before they are considered done:

- **Feel / juice** — dice roll animations, the "attune" flash and sound, timing, screen feedback. You cannot evaluate whether it feels good. Build it behind flags, then request review.
- **Art and audio direction** — palettes, models/sprites, music. Use clearly-labeled placeholders; never invent final assets.
- **Balance values** — bag composition, band-cell point value, empty-cell penalty, favor count, AI weightings. Wire them as data (e.g. a `ScriptableObject` config or plain data file) with sensible defaults; a human sets and playtest-approves the numbers.
- **Undrafted design forks** — the draft model (snake vs simultaneous) and the default information depth (bag counts vs counts + odds) are **decisions this plan does not make**. Do not pick one silently. Implement against whichever the supervisor specifies; if unspecified, escalate before building anything that depends on it. (Structure the code so the draft model is swappable.)
- **Steam integration** — Steamworks, store setup, keys, pricing, cloud-save configuration, and which Unity Steamworks wrapper to adopt. Human-led; you implement against the isolated bridge interface using mocks, never real credentials.
- **Accessibility acceptance** — you implement colorblind-safe encoding (shape + symbol + color), scaling, remappable input, reduced-motion; a human verifies against real assistive tech.

---

## 7. Escalation format

When you must stop and ask, write a single, decision-ready message:

```
BLOCKED: <one-line summary>
Milestone/Task: <id>
What I need: <the specific decision or approval>
Options I see: <A / B / C, with the tradeoff of each>
My recommendation: <one, with a one-sentence reason>
What I've done so far: <links to commits / branch>
```

Escalate for: rules ambiguity, a rules contradiction surfaced by a test, an undrafted design fork, a new dependency, an architecture change, or three failed verification attempts on the same task.

---

## 8. Verification strategy (the backbone)

Every milestone is "done" only when its verification passes. Verification tiers:

- **Unit tests** — every engine function against its contract, including boundary faces (1, 4, 5, 8, 9, 12, 13, 20) and every illegal-placement reason. NUnit, EditMode.
- **Property-style tests** (hand-rolled, looping over many seeds — no new dependency without approval) — invariants that must hold for *all* inputs: any legal placement sequence yields a board where all placement invariants still hold; a full self-play game with only legal random moves always terminates in 6 rounds, throws nothing, and yields an integer score; same seed + same actions ⇒ identical final state (determinism).
- **Golden/seed tests** — fixed seed ⇒ snapshotted roll sequence (guards Daily reproducibility).
- **The canonical 28-point test** (§5).
- **AI sanity tests** — over many seeded self-play games, Oracle beats Novice at a rate above an agreed threshold; no AI tier ever attempts an illegal move.
- **UI (where feasible)** — component render/interaction tests. Feel is *not* auto-verifiable; it goes to human review.

All non-UI tiers should run in Unity EditMode in batchmode (no scene, no window) and pass locally before every commit. CI (GitHub Actions + a Unity license secret) automates this once the supervisor sets up the remote and secret — until then, the agent verifies locally (Editor batchmode or the UnityMCP `run_tests` tool) and says so plainly in `progress.md` rather than claiming CI coverage that hasn't actually run.

---

## 9. Milestones (agent work packages)

Each package lists its goal, its definition of done (DoD), and whether it is agent-autonomous or human-gated. Build in order; do not start a package until the prior one's DoD is met.

| # | Package | Definition of done | Ownership |
|---|---|---|---|
| M0 | Repo & tooling scaffold | `Quintessence.Engine`/`Game`/`UI` asmdefs created with correct `noEngineReferences`/`references` boundaries; EditMode test assemblies wired; project compiles clean in batchmode; CI workflow file scaffolded (pending remote + license secret); `progress.md` started | Agent |
| M1 | Engine core | All §5 contracts implemented; unit + property-style tests; the 28-point test passes; determinism and purity tests pass; the no-`UnityEngine.Random`/no-`DateTime.Now` guard holds via `noEngineReferences` | Agent |
| M2 | Game loop & state machine | Six-round flow, drafting, Firmament, favor, end-of-game scoring as a pure reducer; headless self-play harness (EditMode test) runs thousands of seeded games with zero exceptions | Agent |
| M3 | AI opponents (3 tiers) | Novice/Adept/Oracle as pure policies over the engine; AI-sanity tests pass; turns resolve without I/O | Agent |
| M4 | Presentation & input | The main play screen (per the GDD wireframe): board, roll, Firmament, bag rail; full keyboard + controller input; placeholder art; reduced-motion respected; render/interaction tests pass | Agent scaffolds → **human-gated** on feel/visuals |
| M5 | Vertical slice | One mode end-to-end (supervisor picks vs-AI or Daily) playable start→finish on the chosen draft model; save/resume works; **human playtest sign-off is the gate** | **Human-gated** |
| M6 | v1.0 content & systems | Remaining modes (Campaign, Daily, Endless), progression/unlocks, full accessibility pass, Workshop validator | Agent builds, human accepts accessibility & balance |
| M7 | Steam & shipping | Unity Player build packaging; Steamworks bridge (achievements, cloud saves, leaderboards); Steam Deck Verified pass | **Human-led**, agent implements against mocks |

**Cut order if time is short** (from the GDD): online → Workshop → Puzzle mode. Never cut: the engine's correctness, accessibility, determinism, or the Daily seed reproducibility.

---

## 10. Known failure modes for agents on this project

Watch yourself for these; they are the predictable ways this goes wrong:

- **Building UI first.** The temptation is to show something visual early. Resist it — an untested engine under a pretty board is worse than no board. Engine and its tests come first.
- **Sneaking in `UnityEngine.Random`/`System.Random`/`DateTime.Now`/`Time.*`.** Breaks determinism silently and passes casual testing. `noEngineReferences: true` on `Engine`/`Game` catches `UnityEngine.*` at compile time — do not disable it, and don't reach for `System.Random`/`DateTime` either, since those are still available to a `UnityEngine`-free assembly.
- **"Fixing" a red rules test by editing the test.** This is the most dangerous shortcut. Tests encode the rulebook. Escalate the discrepancy instead.
- **Floating-point in scoring.** Introduces flaky comparisons and non-reproducible scores. Keep scoring integer-only.
- **Silently resolving a design fork** (e.g., just picking snake draft). If the plan says escalate, escalate.
- **Polishing feel autonomously.** You will not know if it feels good, and you can burn unbounded effort here. Feel is human-gated; scaffold and hand off.
- **Scope creep across milestones.** Finish the current package to its DoD before touching the next.
- **Claiming CI is green when it hasn't run.** Unity CI needs a license secret and a remote the agent doesn't control. Say "verified locally" plainly rather than implying automated CI coverage that doesn't exist yet.

---

## 11. Definition of done — vertical slice (the first real gate)

The slice (M0–M5) is complete when: the engine passes all verification tiers including the 28-point test and determinism properties; a human can play one full six-round match against an AI (or the Daily) from launch to a correct final score; the same Daily seed reproduces identically on a second run; input works on keyboard and controller; and a human has playtested it and signed off on the core loop being fun. Everything after that is content and shipping on top of a proven core.
