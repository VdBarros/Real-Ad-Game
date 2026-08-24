# MVP Product Plan — Number Maze (working title)

Companion to `ad-analysis.md`. That document is the source of truth for *what
the ad showed*. This document is the source of truth for *what we are building*.

---

## 0. Standing instructions for all agents

- **Unity 6000.5.9f1, URP, portrait-only, mobile target, new Input System.**
  This is the version pinned in `ProjectSettings/ProjectVersion.txt` — verify
  with `unity projects info` (unity-cli) if it may have changed, don't assume.
- **Do not write comments in code.** Names carry the meaning. Exception: a
  comment is explicitly requested in a task.
- Free / built-in Unity assets and primitives only. Art is placeholder; do not
  spend time on it.
- Every task lists acceptance criteria. A task is done when those pass, not
  when the code compiles.
- **Prefer the `unity-cli` skill / Unity MCP over raw shell or manual Editor
  steps whenever the task touches Unity** — project and editor info, package
  and module management, builds, running tests, scene and hierarchy
  inspection, and live-Editor operations.
- **Assembly definition (asmdef) boundaries are load-bearing, not advisory:**
  - `Game.Domain` — plain C#, **zero `UnityEngine` references**. Rules, graph,
    generator, validator. Unit-testable outside play mode.
  - `Game.Domain.Tests` — NUnit tests for `Game.Domain`.
  - `Game.Presentation` — Unity-side rendering of a `LevelGraph`.
  - `Game.Interaction` — input, pathfinding, encounter and pickup resolution.
  - `Game.Flow` — game state machine, cutscene.
  - Domain must never reference Presentation, Interaction or Flow, or
    `UnityEngine` at all. If a domain type needs Unity, it is in the wrong
    assembly.
- **Agents stay in `.cs` files. Never hand-edit `.unity`, `.prefab`, or
  `.asset` YAML.** These are error-prone to write by hand (fileIDs/GUIDs) and
  merge catastrophically when parallel agents touch the same file. Build the
  world from code instead — T-08's `WorldBuilder` instantiates everything
  (tiles, walls, node prefabs) from a `LevelGraph` at runtime; that pattern is
  the template for all presentation work, not a one-off. Wire as little as
  possible in the Editor by hand. This is enforced in `.claude/settings.json`
  (Edit/Write denied on `*.unity`/`*.prefab`/`*.asset`), not just convention.
- **Fast domain feedback loop:** `Game.Domain` and `Game.Domain.Tests` source
  lives under `Assets/Scripts/Domain(.Tests)/` and is also compiled by a
  standalone project at `dotnet/Game.Domain.Tests/` that globs those same
  files — no duplication. Run `dotnet test dotnet/Game.Domain.Tests` for a
  ~2-second test loop instead of round-tripping through the Unity Editor for
  Phase 1 work. This matters most here: 6 of 18 tasks are Phase 1, and that's
  where the invariant-correctness risk sits.
  **The standalone project must be split so it compiles the way Unity does.**
  Left alone it targets net8.0 / C# 12 while Unity compiles netstandard2.1 /
  C# 9, so domain code can pass the 2-second loop and fail in the Editor.
- **Where this document and a closed wayfinding ticket disagree, the ticket
  wins.** The map is issue #1; every decision below carries a link to the
  ticket holding the reasoning, the measurements and the rejected
  alternatives. This document states what was decided, not why.

---

## 1. Locked design decisions

| # | Decision | Value |
|---|---|---|
| 1 | Scope | Dungeon gameplay + pillar cutscene (non-interactive) |
| 2 | Platform | Mobile, portrait |
| 3 | Control | Tap-to-target |
| 4 | Combat | Instant comparison, no HP, no damage |
| 5 | Win a fight | `P > E` → `P += E` |
| 6 | Tie | `P == E` → fight plays, nobody dies, player walks back |
| 7 | Lose a fight | `P < E` → fight plays, player walks back, **no penalty** |
| 8 | Player power | Monotonically non-decreasing |
| 9 | Level authoring | Procedural, seeded, with solvability validation |
| 10 | Map generation | Entirely at level start; full map visible |
| 11 | Level win | Defeat the boss |
| 12 | Boss difficulty | Not beatable on first arrival; requires clearing more map |
| 13 | Enemy scaling | At generation only. **Numbers never change during play.** |
| 14 | Stranding | Impossible by construction — generator guarantees it |
| 15 | In scope | Weapon-drop visual, floor clearing, power visual tiers |
| 16 | Out of scope | AoE ability, level select, meta-progression, currency, audio |
| 17 | Movement | Tap **any reachable node**; the player walks multi-hop to it. Unconsumed enemies block passage ([#2]) |
| 18 | Multiplier count | **Two** per `ship` level, not the ad's three — at 24 content nodes a third collapses every value to 1 ([#8]) |
| 19 | Workflow | Branch per task, PR to `main`, Actions runs `dotnet test` on Domain only. Unity work is human-reviewed ([#5]) |

Decision 13 is about *numbers*. Enemy **appearance** is deliberately dynamic:
[#14] bands each enemy by `enemy.value / player.power`, because absolute bands
collapse 47% of enemies into one look. The badge never changes; the model does.

[#2]: https://github.com/VdBarros/Real-Ad-Game/issues/2
[#3]: https://github.com/VdBarros/Real-Ad-Game/issues/3
[#4]: https://github.com/VdBarros/Real-Ad-Game/issues/4
[#5]: https://github.com/VdBarros/Real-Ad-Game/issues/5
[#6]: https://github.com/VdBarros/Real-Ad-Game/issues/6
[#7]: https://github.com/VdBarros/Real-Ad-Game/issues/7
[#8]: https://github.com/VdBarros/Real-Ad-Game/issues/8
[#9]: https://github.com/VdBarros/Real-Ad-Game/issues/9
[#10]: https://github.com/VdBarros/Real-Ad-Game/issues/10
[#11]: https://github.com/VdBarros/Real-Ad-Game/issues/11
[#12]: https://github.com/VdBarros/Real-Ad-Game/issues/12
[#14]: https://github.com/VdBarros/Real-Ad-Game/issues/14
[#15]: https://github.com/VdBarros/Real-Ad-Game/issues/15
[#16]: https://github.com/VdBarros/Real-Ad-Game/issues/16

### Calls I made — override any of these if you disagree

- **After the boss dies:** simple result panel → "Next" generates a fresh
  seeded level. No level select, no persistence.
- **Unity 6000.5.9f1 + URP + new Input System.** Pinned so parallel agents don't
  diverge on package versions.
- **Tie and loss are visually distinct** even though mechanically identical —
  tie is a clash-and-stalemate, loss is a knockback. Otherwise the player can't
  tell why a fight went nowhere.

---

## 2. Core rules specification

Agents implementing Phase 1 and Phase 3 must match this exactly.

### Resolution

```
tap(Enemy E) from power P:
    P >  E.power  ->  WIN   : P += E.power; consume E; clear corridor; drop weapon
    P == E.power  ->  TIE   : no state change; return to previous node
    P <  E.power  ->  LOSS  : no state change; return to previous node

tap(Additive A)   ->  P += A.value ; consume A
tap(Multiplier M) ->  P *= M.value ; consume M
tap(Boss B)       ->  same as Enemy; WIN also ends the level
```

A tie is strictly a no-op: **affordable means `P > E`**, never `P >= E`. A
`P == E` transition is not a legal move in any reasoning about the level
([#9]).

### Gating and reachability ([#2])

The player taps **any reachable node**, not only an adjacent one, and walks
multi-hop to it, resolving everything on the way.

- An **unconsumed enemy or boss blocks passage.** It is not a wall the walk
  routes around; it is a door.
- **Reachability is a flood fill over the consumed set** from the player's
  position, stopping at unconsumed enemies. It grows as nodes are consumed and
  never shrinks.
- Consumed nodes are inert but still walkable.
- **Cleared floor is a reading of the consumed set**, not a property owned by a
  corridor: the enemy you just defeated is consumed, so its tile and the
  corridor behind it change at the same moment. Corridor ownership does not
  exist ([#6]).

### Two structures, not one ([#3])

The domain holds **both** a tile grid and a decision graph, and they are not
the same thing:

- The **tile grid** is ~60 walkable `(x, y, floor)` cells. Geometry: what
  renders, and where a walk may physically go.
- The **decision graph** is the ~24 content-bearing nodes extracted from those
  tiles, joined by corridors. All reasoning about solvability, the power
  envelope and fuzzing happens here.

Without the split, T-07's brute-force criterion is unmeetable: exhaustive
search over 60 tiles is intractable where search over 24 nodes is not.

### Generation invariants

Let, over the whole level excluding the boss:

- `A` = Σ(additive pickup values) + Σ(enemy powers)
- `M` = Π(multiplier values)
- `P₀` = starting power

**Invariant B — boss is always eventually beatable**

```
boss.power < P₀ * M + A
```

Sufficient, and provably so: multiplication commutes, multipliers are worth
strictly less the earlier they are taken, so "all multipliers first" is the
worst possible ordering. If reachability gating prevents that ordering, the
player is forced into a *better* one. So `P₀ * M + A` is a lower bound on the
final power of every run that exists.

The boss is **derived from that bound**, not authored against it ([#8]):

```
boss.power = round(0.8 * (P₀ * M + A))
```

so B holds by construction and the boss scales with whatever the maze turned
out to contain. Rejected deriving it from `P_min` of the boss's region times a
margin: Invariant C then failed 80/200, because a beeline can pick up large
additives that the cheapest unlock skips.

The generator additionally asserts **nothing is gated behind the boss** —
reachability with the boss treated impassable must still cover every content
node — and places the boss-room treasure **in front of** the boss rather than
inside the room, which would be unreachable.

**Invariant C — boss requires a detour**

```
boss.power > power of a player taking the shortest path to the boss
```

This is what makes decision 12 true rather than aspirational. During layout,
before content exists, it is approximated by **tile distance from `Start`**
([#6]) — the same metric §2's region scaling keys off.

**Invariant A — progress never stalls**

From any reachable state, at least one unconsumed node must be both reachable
and affordable. A state where none is, is a **stall**.

Checked by an **adversary panel of five policies**, not one ([#9]). The
original single greedy-worst walk — multiplier, then addition, then cheapest
affordable enemy — is **unsound in the dangerous direction**: measured against
the exhaustive oracle it missed 14 real stalls in 2292 mutated levels. It never
false-alarmed, so every failure it reports is real, but silence from it did not
mean safe.

The panel is that policy plus four siblings — additive-first, enemy-first,
biggest-additive-first, biggest-multiplier-first. **A level fails if any policy
strands.** Five walks cost microseconds each, and together they miss 4 of 2292.
The original priority order stops being the *definition* of worst and becomes
one member of the panel.

Generation retries a rejected level, **capped at 50 attempts**, then throws
with a per-reason histogram rather than looping.

**The brute-force oracle only has teeth under mutation** ([#9]). On unbroken
levels greedy and exhaustive agree vacuously — 0 stalls in 118 `tiny` levels —
because #8 mints values during the adversary's own walk, so affordability is
true by construction. T-07 must therefore *break* levels first (inflate one
enemy ×3 / ×10 / ×50, one at a time) and assert the panel's verdict matches the
oracle's on every mutant. **"Agree" means identical verdict, never identical
consumed set** — the oracle explores all orderings, the panel walks five, so
their consumed sets differ legitimately on levels both call safe.

The oracle runs on **`tiny` only**; `ship` blows a 200k state budget five times
in six. Peak `(consumed-set, power)` state count on `tiny` is a median of 93,
so it runs inside the fast loop rather than as a separate job.

### Region scaling

**Content scales with tile distance from `Start`, never with `regionId`**
([#7]). Regions are Voronoi cells: contiguous, but carrying no near-to-far
ordering, so keying difficulty off their index would be keying it off nothing.

Per region `R`, computed during generation ([#8]):

- `P_min(R)` — **the cheapest way in.** Consume as little as possible, always
  the smallest available power gain, stop the instant `R` is reachable.
- `P_max(R)` — **the richest entry.** Strip everything outside `R` first, in
  the best order, then enter.

```
min(enemy power in R) <= P_min(R)      // floor: something is always edible
max(enemy power in R) ~  P_max(R)      // ceiling: good routing is rewarded
```

The earlier wording — "power of the worst route" and "the optimal one" — reads
as walk outcomes, and implementing it that way produced **spread ratios below
1**: a dawdling adversary reaches a region later and therefore *richer*, so the
"worst" wall sat above the "best" one. Both walls are greedy searches, not
exhaustive ones; `ship` never runs the oracle, and the greedy walls are
certified transitively by T-07 agreeing with brute force on `tiny`.

The floor rule cannot be honoured while minting, because `P_min` is a property
of the finished level. **Mint first, then walk the floor rule down:** in each
region pull the cheapest enemy below that region's `P_min`, iterating to a cap
of 6 passes. Lowering a value only ever lowers power, so `P_min` can move down
underneath the repair. Floor-rule failures go from 16/51 to **0/445**.

The `P_min`/`P_max` spread is the skill expression. Tune it, don't collapse it.
Measured `P_max/P_min` p10/50/90 = **1.0 / 14.3 / 193.2**; the p10 of 1.0 is
the start region, where both walls are `P₀` by definition.

### Presets ([#4])

| | `tiny` | `ship` | `stress` |
|---|---|---|---|
| purpose | exhaustive verification | what ships | generation-time regression |
| content nodes | 10–12 | ~24 | 90 |
| tiles | — | ~60 | — |
| floors | 1 | 2 | 3 |
| regions | 2 | 4 | 9 |
| `D_min` | — | **16** | — |

`ship`'s `D_min` was lowered from 20 to 16 on measured rejection rates ([#7]).
The carve alone already spends ~11% of `ship` seeds and ~32% of `tiny` seeds;
envelope rejections stack on top, so the **combined** rate is what has to stay
sane. Content placement accepts 89% of the seeds that reach it.

### Maze construction ([#7])

Recursive backtracker per floor → **braid 0.25** → stairs → **Voronoi**
regions → extract the decision graph.

Braid is the load-bearing knob. At 0 the maze is 53% gates and has a single
forced order; at 1.0 it is 3% gates and no puzzle. **0.25 gives ~30% gates and
~2.3 pockets per level**, and the placement policy has to fit that supply.

A tile becomes a decision node when its corridor-degree is not 2, or it is a
stair, or it is the start. Junctions promote to never-consumed `Empty` nodes.
**Stairs are exempt from the empty-path assertion** — a stair is two tiles, one
per floor at the same `(x, y)`, joined by one edge, so it is zero-length by
construction.

---

## 3. Architecture

The maze is **a graph, not a world.** The isometric scene is a view of that
graph. This split is the reason agents can work in parallel and the reason the
generator is testable at all.

```
Game.Domain (plain C#, no UnityEngine)   <- Phase 1
  LevelGraph, Node, Edge, NodeType
  TileGrid
  LevelGenerator (seeded)
  PowerEnvelope, SolvabilityValidator, AdversaryPanel, Oracle
  LevelGraphWriter
  RunState, ActionResolver

Game.Domain.Tests                        <- Phase 1
  NUnit, also globbed by dotnet/Game.Domain.Tests

Game.Presentation (Unity)                <- Phase 2
  WorldBuilder, BadgeSystem, VisualTiers
  CameraRig, FloorStateController

Game.Interaction (Unity)                 <- Phase 3
  TapInput, GraphPathfinder
  EncounterController, PickupController

Game.Flow (Unity)                        <- Phase 4
  GameStateMachine, PillarCutscene
```

**Serialization stays inside Domain with zero dependencies** ([#10]). A
`noEngineReferences` asmdef *can* reference Newtonsoft via `overrideReferences`
plus `precompiledReferences`, but a hand-rolled writer is preferred:
`LevelGraph` is small and closed, byte-identical output becomes true by
construction, and the two-compilation-context problem disappears. No library
gives deterministic ordering anyway — the model must.

**`WorldBuilder` receives a `LevelGraph` and nothing else** ([#12]). No bounds,
no camera parameters, no wall data. Projection constants are compile-time,
walls are derived from absent neighbours rather than stored, and props are a
switch on `NodeType`. Determinism comes from three rules: names are a function
of the tile key rather than a counter, sibling order is the `(floor, y, x)`
sweep that also assigns node ids, and no `Dictionary`/`HashSet` iteration
appears in the build path.

**The camera is two states, not three modes** ([#15], [#16]): a per-preset
constant — `euler(30, 45, 0)`, orthographic, size **9.50** — and cuts away from
it. Follow mode does not exist, because the whole level is on screen at that
framing for every seed. **The rig exposes no rotation field**, which is what
lets #11 copy each badge's rotation once at construction instead of
billboarding every frame.

**Teardown is one `LevelRoot`**, but the real leak risk is not GameObjects: the
procedurally generated badge `Texture2D` and `Material` ([#11]) are not
collected when the objects referencing them are destroyed, so `WorldBuilder`
owns them explicitly, caches them across levels, and destroys them on dispose.

---

## 4. The work itself

The task backlog and its execution order used to live here. They are now
**filed as issues** — T-01 through T-18, with native `blocked_by` edges, phase
labels and `ready-for-agent` — so there is exactly one place that says what is
left to do, and it is the same place that says who is doing it.

- **Spec: Number Maze MVP** — <https://github.com/VdBarros/Real-Ad-Game/issues/17>
  — the problem, the solution, the user stories, and every implementation and
  testing decision in one document.
- **T-01 … T-18** — the filed tickets. The frontier is whatever has no open
  blocker; T-01 is the root and everything else waits on it.
- **Issue #1** — the wayfinding map, whose closed tickets hold the reasoning,
  the measurements and the rejected alternatives behind every decision above.

This document keeps only what those cannot hold: the premises (§1), the rules
and their proofs (§2), and the architecture (§3).

The riskiest work by a wide margin is **T-05 and T-06**. If the envelope maths
is wrong the game is either unloseable and boring, or stranding players despite
the guarantee. T-07 is what tells you which, so get it running early and treat
its output as the real measure of whether the design works.
