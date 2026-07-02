# QUINTESSENCE — Game Design Document
### A cozy-strategic dice-placement game for PC (Steam)

*Version 0.2 (digital) — supersedes the tabletop rulebook v0.1*

**Genre:** turn-based puzzle / drafting strategy (single-player-first, with versus)
**Platform:** Windows / Linux / Steam Deck (macOS if the engine makes it cheap)
**Business model:** premium single purchase, no microtransactions
**Comparables:** *Sagrada* (digital), *Astrea: Six-Sided Oracles*, *Dice Legacy*, *Islanders*, *Dorfromantik* (for the cozy-but-thinky loop and daily-challenge retention)

---

## 1. Logline

Assemble a corner of the cosmos, one die at a time. Draft the five Platonic solids — each an element with its own range and its own gamble — and place them into your board so their values land where they belong. Small dice are safe; the great icosahedron is the only key to the heavens, but it rolls wild. A quick game to learn, a deep one to master, and a different puzzle every single day.

---

## 2. From tabletop to digital — what changes

| Tabletop v0.1 | Digital v0.2 |
|---|---|
| 60 physical dice; scarcity fixed by component count | **Visible finite bag** — a tunable draw pile the game tracks and displays. Scarcity is now a strategy layer, not a logistics limit. |
| Leftover die placed on a Firmament tray | Firmament is a UI strip of banked dice, sortable, with clear affordances |
| Manual scoring, adjacency checks by eye | Engine validates placement and scores instantly; illegal moves are simply not offered |
| Rulebook teaches the game | **Interactive tutorial** teaches by playing; no rulebook shipped |
| Balanced by hand across 4 boards | **Procedural + hand-authored boards**, plus Steam Workshop for community boards |
| 2–4 humans required | **Solo vs AI, daily puzzles, and score-attack** are the core; humans optional |

**What deliberately does *not* change:** the five-solids-as-risk-profiles idea, bands-as-locks, the 3×4 board and its three cell types, the placement rules, favor tokens, and the scoring math. That core is the game — everything below is delivery.

---

## 3. Core gameplay (updated for digital)

### The five solids

| Element | Die | Range | Bands it reaches | Feel |
|---|---|---|---|---|
| Fire | d4 | 1–4 | Low | Plentiful, dependable |
| Earth | d6 | 1–6 | Low, part of Mid | Workhorse |
| Air | d8 | 1–8 | Low, Mid | Flexible mid |
| Aether | d12 | 1–12 | Low, Mid, High | Rare; a key to High |
| Water | d20 | 1–20 | Low, Mid, High, **Celestial** | Scarce, swingy, the only key to Celestial |

Value bands: **Low 1–4** (any die) · **Mid 5–8** (d6+) · **High 9–12** (d12, d20) · **Celestial 13–20** (**d20 only**).

The whole game rests on this: the solids are not a power ladder, they are five risk profiles. A d4 can never reach past Low but always lands there; a d20 can reach anywhere but betrays you as often as it saves you.

### The board

A **3 × 4 grid of 12 cells**. Three cell types:

- **Element cell** — accepts only its element (any face value).
- **Band cell** — accepts any die; *scores* only if the face is in-band (an off-band die still sits there and counts for adjacency/objectives).
- **Wild cell** — accepts anything.

The template that keeps boards balanced: exactly one Low / Mid / High / Celestial band cell, four distinct element cells, four wild cells. (Hand-authored and Workshop boards must pass a validator enforcing this recipe; procedural boards generate within it.)

### A round (six per match)

1. **Roll.** The game draws dice from the bag and rolls them into a shared pool.
2. **Draft & place.** Players take dice from the pool (or the Firmament) and place them, following the placement rules.
3. **Firmament.** Leftover dice bank into the Firmament, keeping their faces, available in later rounds.

Twelve placements fill a board.

**Placement rules** (the engine enforces these; illegal targets are greyed out):
1. **Connection** — after the first die, each die touches an existing one orthogonally or diagonally.
2. **Element adjacency** — no two same-element dice orthogonally adjacent (diagonal is fine).
3. **Cell lock** — respect element/band/wild cells.

**Digital nicety:** placement is a two-step *aim → confirm*, so a misclick isn't a ruined game, but once confirmed a die is locked (no free undo in ranked/daily modes; casual mode allows one undo per round). This preserves the weight of a decision while removing frustration.

### Favor tokens (3 per match)

Spend one, before confirming a placement: **Adjust** (±1 pip), **Reroll** (reroll the drafted die), or **Defy** (ignore element-adjacency once). Unspent tokens are worth +1 each at scoring.

### Objectives & scoring

One **public objective** per match (shared), one secret **private element** (+2 per matching die). Scoring: satisfied band cell **+4**, public objective as printed, private element **+2 each**, unspent favor **+1 each**, each empty cell **−2**. High score wins; ties broken by fewest empties, then most band cells.

*(The six public objectives and four starter board layouts from the tabletop rulebook carry over verbatim as the launch content set.)*

### The one new mechanic: the visible bag

Each match uses a finite bag with a known starting composition (default weighting favors small dice; big dice are rare). Rolls draw **without replacement**, and the UI shows what remains at all times. This means:

- You can *read the game*: "only 2 Water left, and three players want Celestial cells" changes how you draft.
- The endgame tightens deterministically instead of staying random to the last roll.
- It's a skill expression a computer surfaces for free and a table never could.

A **Casual toggle** switches to infinite weighted rolls (no depletion) for players who'd rather not track it.

---

## 4. Game modes

**Solo is the launch core.** Everything here should be fun against no human.

- **Campaign / Ascent.** ~40–60 hand-authored boards grouped into elemental "spheres," each with a par score and a twist (locked cells, a stingy bag, a forced objective). 1–3 star ratings drive completion. This is the tutorial-through-mastery spine.
- **Daily Cosmos.** One seeded board + bag + objective, identical for every player worldwide, one attempt. Global and friends leaderboards. This is the retention engine — cheap to build, endlessly re-playable, screenshot-shareable.
- **Endless / Score Attack.** Escalating rounds with a mounting bag scarcity; play until you can't place legally. Personal-best chasing.
- **Puzzle mode.** Fixed board, fixed dice already known — find the optimal placement. Leans into the deterministic nature; great bite-sized content and Workshop fodder.

**Versus.**

- **vs AI** — three difficulty tiers (Novice plays greedily; Adept values band cells and denial; Oracle reads the bag and plays for objectives). AI turns resolve instantly, so no downtime.
- **Local hotseat / pass-and-play** — cheap to build, nice for Steam Deck on a couch.
- **Online (post-launch stretch).** See §8; recommend async "same-draft" over real-time to kill turn-order downtime.

---

## 5. Progression & meta

- **Unlocks through play** (never purchase): new boards, additional public objectives, harder AI, and cosmetic **dice themes** (Obsidian, Stained Glass, Aurora, Clockwork…) and board backdrops.
- **Mastery tracks** per element/board give long-tail goals.
- **No pay-to-win, no MTX.** Single premium price. Optional cosmetic DLC pack post-launch is acceptable; gameplay content (boards, objectives) should trend toward free updates + Workshop.

---

## 6. UX, information design & feel

**Screen architecture** (see the wireframe in chat): your board center-left; the current roll along the top; a right rail with bag composition, the active objective, and favor tokens; the Firmament as a strip along the bottom.

**How much math to show.** Default surfaces *ranges and bag counts* — enough to play well without a spreadsheet. An optional **Oracle overlay** (toggle) shows exact odds of each remaining die hitting each band, for players who want it. Casual by default, deep on demand.

**Feel is the make-or-break.** A dice game lives in the tumble and the *click* of a die locking home:
- Dice roll with light physics or scripted tumbles; the d20 gets a longer, more dramatic settle.
- A satisfying **attune** flash + tone when a die lands in-band on a band cell; a duller thunk when it's off-band.
- Restrained juice on big moments (a Celestial cell filling), always respecting a **reduced-motion** setting.
- Ambient, cosmic-alchemical score; each element has a signature timbre.

**Art direction.** Deep-space alchemy — constellations, elemental glow. Each solid has a distinct color *and silhouette and material*: Fire (molten tetra), Earth (stone cube), Air (glass octa), Aether (starfield dodeca), Water (liquid-crystal icosa). Distinct shapes do double duty as accessibility (below).

**Onboarding.** No rulebook. A 5-minute interactive tutorial teaches drafting, bands, and adjacency by doing; the Ascent's early boards ramp complexity.

---

## 7. Accessibility (a launch requirement, not a nice-to-have)

- **Colorblind-safe by construction:** elements are never encoded by color alone — each has a unique solid silhouette and a symbol. Provide colorblind palettes anyway.
- **Text scaling** and a readable minimum size; dyslexia-friendly font option.
- **Full input remap**; complete **controller support** (this is a grid + a pool — it maps cleanly to a d-pad/stick + confirm).
- **Reduced-motion** and screen-shake toggles.
- **Screen-reader / narration** pass for menus and board state where feasible.

---

## 8. Steam integration

- **Achievements** — "Fill a board with no empties," "Satisfy a Celestial cell with a natural 20," "Win using only Fire/Earth/Air" (*Humble Cosmos*), "7-day Daily streak."
- **Leaderboards** — Daily Cosmos (global + friends), Endless.
- **Cloud saves** — resume any match on any machine / Steam Deck.
- **Steam Workshop** — community boards and objective sets, gated by the balance validator. Big longevity lever.
- **Trading cards / badges** — cheap wishlisting and visibility.
- **Demo for Steam Next Fest** — the Ascent's first sphere + one Daily; the demo *is* the marketing. Build wishlists hard before launch.

**Steam Deck is a priority target, not an afterthought.** Turn-based, low-spec, controller-native — aim for **Steam Deck Verified at launch**. Cozy strategy games overperform on Deck.

---

## 9. Technology

- **Engine:** Godot 4 (free, no royalties, strong 2D/2.5D, good Deck/Linux story) as the default recommendation; Unity if the team already knows it. The game is presentation-light and logic-light — either is overkill in a good way.
- **Target spec:** runs on effectively anything from the last decade; that low barrier widens the audience and eases Deck verification.
- **Architecture:** deterministic core (seeded RNG) so Daily and Puzzle modes are reproducible and replays/leaderboards are verifiable; UI as a thin layer over that core.
- **Save/cloud:** small JSON-ish saves, trivially cloud-synced.
- **Netcode (if online ships):** prefer **async, same-seed drafting** — everyone drafts the same rolled pool from their own copy, conflicts resolved by a rotating priority token. Removes real-time downtime and is far simpler than lockstep. Real-time is a later stretch.

---

## 10. Scope & roadmap

**MVP (vertical slice):** one board, the full core loop, one objective, working bag + Firmament, placeholder art, one AI tier. Proves the feel.

**v1.0 launch:**
- Core loop, 4+ starter boards, all 6 objectives, visible bag + Casual toggle
- Ascent campaign (first ~2 spheres), Daily Cosmos, Endless
- vs AI (3 tiers), local hotseat
- Full accessibility pass, controller support, Steam Deck Verified
- Achievements, cloud saves, leaderboards
- Interactive tutorial

**Post-launch:** more boards/objectives, dice themes, Steam Workshop, Puzzle mode expansion, then online versus if the numbers justify it.

Cut lines if time is short, in order: online → Workshop → Puzzle mode. Never cut: feel, accessibility, Daily.

---

## 11. Business

- **Premium**, single purchase, roughly **$9.99–$14.99** for this cozy-strategic niche.
- **Demo + Next Fest** for wishlists; wishlists are the whole ballgame for indie Steam visibility.
- Optional post-launch **cosmetic DLC** (theme packs); keep gameplay content leaning free/Workshop.

---

## 12. Open decisions (your call, designer)

1. **Draft interaction for humans.** Snake draft (more denial, some downtime) vs simultaneous same-seed draft (snappier, less direct denial). I lean simultaneous for online/hotseat, snake for a "classic" mode — but pick one as canonical if you want to keep it simple.
2. **How much information to surface by default.** Bag counts only, or bag counts + a light "reliability" hint on each die? The Oracle overlay covers the hardcore either way.
3. **Undo policy.** Casual undo per round vs fully committed everywhere. Affects how punishing the game feels.
4. **Online at launch or never-until-proven.** Strongly recommend never-until-proven; solo + Daily carries a game like this.
5. **Art direction commitment** — cosmic-alchemical is my pitch; a cleaner minimalist/geometric look (à la *Islanders*) is a cheaper, equally valid alternative.

---

## 13. Tuning levers (now config values)

Everything that was a physical constraint is now a number in a balance file:

- **Bag composition & size** — the master scarcity dial. Per-mode and per-board.
- **Band cell value** (default flat 4) — flat keeps all solids equally worth chasing; per-band scaling makes big-dice boards feel richer at the risk of "biggest die wins."
- **Empty-cell penalty** (−2) — the pressure to place awkward dice.
- **Favor token count** (3) and **unspent value** (+1) — agency vs tension.
- **Firmament rules** — pick limits, aging-out, whether it's shared or per-player. Digital lets you experiment freely here.
- **AI weighting profiles** per difficulty tier.

The core loop is deliberately small and fully specified; the digital layers above are where the game becomes a *product*.