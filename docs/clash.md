# QUINTESSENCE — Clash Mode
### Feature implementation spec for the coding agent

*Version 0.1 · A feature package layered on the core game. Read `AGENTS.md` and `docs/agent-build-plan.md` first — every operating rule there applies here unchanged. This document is authoritative for Clash mode's design and its implementation boundaries.*

---

## 0. Before you start

- **Dependency:** this feature builds on a working core. Do not begin until milestones **M1 (engine core), M2 (game loop), and M3 (AI)** from the build plan are complete and green. If they are not, stop and say so.
- **All core rules still apply:** engine before pixels, tests before engine; the engine and game packages stay pure (no `Math.random`/`Date`, injected seeded RNG); scoring and all counters are integers; determinism is mandatory; you do not sign off on subjective quality; small verified commits; update `docs/progress.md`; escalate using the BLOCKED format.
- **The prime directive of this feature:** Clash must be **modular**. Standard, Daily, Endless, Puzzle, and Solo modes must behave **byte-identically** before and after this feature lands. A regression test proving that is part of the definition of done.

---

## 1. What Clash mode is

Clash is a dedicated high-interaction competitive mode (2–4 players) played over the standard six-round match. Everything from the base game applies; Clash adds two things on top:

- a **Storm** meter each player charges by playing well, and
- **Cosmic Interventions** — rare, elemental, always-counterable sabotage actions paid for with Storm.

The design intent is *streamable, self-correcting conflict*: interventions are scarce (roughly one or two per game), each is a single legible elemental effect, hitting someone hands them a defensive resource (so it rubber-bands instead of kingmakes), and no intervention can ever stop a player from scoring. Do not "improve" the game by making interference cheaper, more frequent, or unavoidable — those properties are load-bearing.

---

## 2. Design spec

### 2.1 Storm meter

- Each player has an integer `storm`, starting at `0`.
- **Charge:** `+stormPerAttune` each time the player attunes a band cell (places an in-band die on a band cell), capped at `stormCap`.
- **Spend:** declaring an Intervention costs `interventionCost` Storm. The cost is paid whether or not the intervention is warded.

### 2.2 Cosmic Interventions (base set of five)

At match start, deal each match a random subset of `interventionsPerMatch` of the five (default 3). Every player may use any intervention in that match's subset, as often as Storm allows. Each is a bounded, deterministic transform:

| Element | Name | Effect (precise) |
|---|---|---|
| Fire | **Scorch** | Choose an opponent's placed die. Reduce its face by up to `scorchMaxPips` (clamped to a minimum face of 1). Re-evaluate that cell's attunement. |
| Water | **Riptide** | Move one chosen die from a target opponent's Firmament to your Firmament. Illegal to declare if the target's Firmament is empty. |
| Air | **Gust** | A reaction during the draft: take one die from the current pool out of turn order, before the player whose pick it is. Total picks in the round are unchanged. |
| Earth | **Petrify** | Place a Petrify token on one empty cell of a target opponent's board. That cell cannot be used until `petrifyDurationRounds` later. The owner may **shatter** it on their turn by spending 1 favor. |
| Aether | **Eclipse** | Choose one: **(a)** nullify one target band cell so it scores 0 at game end, or **(b)** as a reaction, cancel an intervention currently being declared. |

**Scope decisions (do not deviate without approval):** Riptide targets the **Firmament only**, never placed board dice, in this version. A board-theft variant is an explicit human decision — do not implement it. Eclipse is limited to exactly the two options above.

### 2.3 Counterplay economy (the safeguards — non-negotiable)

When a player T is validly targeted by an intervention, resolution is strictly ordered:

1. **Backlash:** T immediately gains `backlashFavor` favor.
2. **Ward window:** T may spend `wardCost` favor to **Ward** — the intervention is negated (the attacker's Storm is still spent). Or T declines and the effect applies (keeping the backlash favor).

So a warded player nets zero favor but negates the hit; an unwarded player eats the hit but banks a favor. This is what makes targeting the leader legitimate yet self-correcting.

**Hard invariants (must be enforced and tested):**
- No intervention may leave a player's board unplaceable or skip/deny a player's turn.
- Petrify only ever targets **empty** cells, is escapable (shatter) and temporary.
- `storm`, `favor`, and all scores remain integers ≥ 0 at all times.
- Interventions are **legal-by-construction**: an intervention that cannot be applied legally cannot be declared.

### 2.4 Balance values (human-gated — use these provisional defaults)

Wire all of these as a `ClashConfig` data object; do not hardcode them, and do not treat these numbers as final — a human sets them via playtest.

```
stormPerAttune: 1        stormCap: 5           interventionCost: 4
backlashFavor: 1         wardCost: 1           scorchMaxPips: 3
petrifyDurationRounds: 1 interventionsPerMatch: 3
```

---

## 3. Architecture & implementation guidance

Clash must extend the core, not fork it.

- **Mode flag:** add `'clash'` to the game-mode type. Clash-only state lives in optional fields that are **absent** in every other mode.
- **State additions (only populated when `mode === 'clash'`):** per-player `storm` and dealt `interventionsAvailable`; a match-level list of active `PetrifyToken`s; an append-only `interventionLog` (needed for deterministic replays and later spectator features).
- **Reducer:** extend the existing pure reducer with the new action variants below. The reducer must ignore/reject Clash actions when `mode !== 'clash'`. Do not branch the core placement/scoring logic; compose on top of it.
- **Determinism & purity:** any randomness inside an intervention (none in the base five, but keep the door closed) uses the injected seeded RNG. All intervention resolution is pure.
- **Isolation:** put Clash logic in its own module within `packages/game` (e.g. `game/clash/`), importing the engine but adding no dependency the base modes carry at runtime.

### 3.1 Engine/game contract additions (illustrative types, binding contracts)

```ts
export type GameMode = 'standard' | 'daily' | 'endless' | 'puzzle' | 'solo' | 'clash';

export type InterventionKind = 'scorch' | 'riptide' | 'gust' | 'petrify' | 'eclipse';

export interface ClashConfig {
  stormPerAttune: number; stormCap: number; interventionCost: number;
  backlashFavor: number; wardCost: number; scorchMaxPips: number;
  petrifyDurationRounds: number; interventionsPerMatch: number;
}

export interface PetrifyToken { player: PlayerId; row: number; col: number; expiresRound: number; }

// New action variants (valid only when mode === 'clash'):
type ClashAction =
  | { type: 'intervene'; actor: PlayerId; kind: InterventionKind; params: InterventionParams }
  | { type: 'ward'; target: PlayerId }        // spend wardCost favor to negate the pending intervention
  | { type: 'declineWard'; target: PlayerId } // keep backlash favor, apply the effect
  | { type: 'shatter'; owner: PlayerId; row: number; col: number }; // spend 1 favor to clear a Petrify token

// Resolution is pure and deterministic:
export function applyIntervention(
  state: GameState, action: Extract<ClashAction, {type:'intervene'}>, rng: RNG
): GameState; // must throw / be un-declarable if illegal-by-construction
```

Contracts each intervention must satisfy are given in §2.2 and §2.3. Charge Storm on attune (§2.1). Enforce the invariants in §2.3.

---

## 4. Verification

Add these on top of the build plan's existing tiers. Do not merge red.

- **Regression (highest priority):** golden tests proving Standard/Daily/Endless/Puzzle/Solo produce identical results to pre-feature for a fixed seed set. Clash state must be absent in those modes.
- **Unit tests:** each intervention against its contract, including boundaries (Scorch clamping at face 1; Riptide illegal on empty Firmament; Petrify only on empty cells; Eclipse both options; ward/backlash ordering; Storm cap and cost).
- **Property tests (`fast-check`):**
  - No sequence of legal interventions ever produces an unplaceable board, a skipped turn, or a negative counter.
  - A full Clash self-play game (AI using interventions, thousands of seeded runs) terminates in six rounds, throws nothing, and is identical per seed.
  - Determinism holds across the intervention log: same seed + same actions ⇒ identical final state.
- **Canonical Clash acceptance test (must pass):** a fixed seeded scenario — Player A charges to `interventionCost` Storm by attuning cells, plays **Scorch** on Player B's d20 sitting on a Celestial cell, dropping it from 15 to 12 (out of the Celestial band); B receives `backlashFavor`, declines to Ward, and loses that cell's 4 points. Assert the exact resulting Storm, favor, and final scores. Encode this the same way the core's 28-point test is encoded.

---

## 5. Human-gated — scaffold, do not decide

- **All balance values** in `ClashConfig` (§2.4). Wire them; a human tunes them by playtest.
- **Feel / juice / audio** of interventions — the whole point is shareable moments (Scorch's gut-punch, Riptide's theft). Build the hooks behind flags with placeholders; a human judges whether they land.
- **AI intervention *tuning*** (how aggressive to be, when to Ward). The mechanism is yours to build and test (the AI must only ever act legally and must Ward when clearly correct); the *personality/aggressiveness* is human-approved.
- **Base-set composition** — which of the five ship in the default pool, and whether a board-theft Riptide variant is ever added. Human decision; escalate if asked to change the set.
- **Spectator / streamer integration** (Twitch/YouTube chat voting to trigger events) is **out of scope for this package.** It needs platform APIs and credentials and is human-led. Design the `interventionLog` and event surface so it is *possible* later, but build none of it now.

---

## 6. Work plan

Build in order; each package meets its DoD (implemented + tests + `pnpm lint && pnpm typecheck && pnpm test` green + `progress.md` updated) before the next begins.

| # | Package | Definition of done | Ownership |
|---|---|---|---|
| C0 | Clash scaffolding | Mode flag, optional Clash state, `ClashConfig` with defaults, module skeleton; **regression golden tests prove all non-Clash modes are unchanged** | Agent |
| C1 | Storm meter | Charge on attune, cap, spend/cost; unit + property tested | Agent |
| C2 | Interventions engine | The five as pure, deterministic, legal-by-construction transforms; invariants enforced; unit + property tests; **canonical Clash acceptance test passes** | Agent |
| C3 | Counterplay | Backlash → Ward/decline ordering, Shatter; tested, including favor accounting | Agent |
| C4 | AI | Policies use interventions legally and Ward when clearly beneficial; AI-sanity tests (never illegal); aggressiveness tuning left to humans | Agent builds, human tunes |
| C5 | Presentation | Storm meter, intervention pick + targeting, backlash/ward prompts, placeholder feel; reduced-motion respected; interaction tests | Agent scaffolds → **human-gated** on feel |
| C6 | Mode wiring | Clash selectable end-to-end; a full match plays start→finish deterministically; **human playtest sign-off is the gate** | **Human-gated** |

---

## 7. Guardrails & stop-and-ask (feature-specific)

Stop and escalate (BLOCKED format) if:

- A change to Clash would alter any non-Clash mode's behavior (the regression tests should catch this — if they do, that's a stop, not a test to weaken).
- You are asked to make an intervention unavoidable, uncounterable, or capable of preventing a player from scoring — these violate the design invariants.
- You are tempted to add board-theft, a new intervention, or change the base set.
- You reach C5 (presentation) or any feel/balance decision.
- A balance value seems wrong in testing — flag it; do not silently retune the defaults.
- Anything touching spectator/streamer platform integration.

Record in `docs/progress.md`: add C0–C6 to the milestone tracker, and log the two open human decisions (final `ClashConfig` values; base-set composition / board-theft variant) under Open questions.