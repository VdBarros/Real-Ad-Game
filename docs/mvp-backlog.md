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
| 20 | Taking a reward | The prop's own `chest_lid` node swings open over the first 55% of the 0.30 s beat, then the whole prop fades out. Nothing of it is left on the tile ([#117]) |
| 21 | Walking surface | Exactly one per tile. A tile footed with a flight walks on the flight itself; every other tile walks on its floor quad. Never both ([#113]) |

Decision 13 is about *numbers*. Enemy **appearance** is deliberately dynamic:
[#14] bands each enemy by `enemy.value / player.power`, because absolute bands
collapse 47% of enemies into one look. The badge never changes; the model does.

Decision 20 replaces the pedestal the take used to leave — a crushed, widened,
stone-tinted slab that read as the chest falling through the floor and staying
there. The ad shows a chest that *opens* and is then gone, and the tile alone
carries on saying a node was here. Two consequences are load-bearing. A mesh
prop **fitted** to a tile may never carry a non-uniform scale: `ModelPose.Fitted`
sizes the chest and the coin stack from height alone and squares that across all
three axes, because a lid hinged inside a squashed parent shears instead of
swinging, and because the fit factor only means anything while the mesh keeps
its authored proportions. The rule is about fitting, not about mesh props at
large — a part sized to *span* a cell is stretched to the grid on purpose and
stays non-uniform: the staircase, the foundation, the wall panel and the floor
quad. And a level resumed onto an already-consumed node snaps to the end of the
take, so the chest is gone from the first frame rather than fading again on
every reload.

Decision 21 settles what a tile stands on. The floor quad used to be
unconditional, so a tile footed with a flight carried two surfaces: the quad at
the tile's own elevation and the pack's staircase spanning the step below it.
The quad hovered over most of the flight, because a flight only reaches the
tile's floor at the crest end of its climb. Since the flight already tops out
flush with that floor and covers the whole tile, it *is* the surface, and the
quad above it was a second, wrong one. Two things ride on whichever part a tile
walks on, and both follow the surface rather than the quad: `FloorState` adopts
it, so a staircase tile flips cleared and cursed with the rest of its region;
and it is the one part of a tile that keeps its collider, so a tap on a climbing
tile lands on the treads instead of on air a step above them. The consequence to
watch is that `PartStyle.Staircase`'s own material never survives a built level
— the floor state paints every flight as ground the moment the run opens.

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
[#61]: https://github.com/VdBarros/Real-Ad-Game/issues/61
[#113]: https://github.com/VdBarros/Real-Ad-Game/issues/113
[#114]: https://github.com/VdBarros/Real-Ad-Game/issues/114
[#117]: https://github.com/VdBarros/Real-Ad-Game/issues/117
[#129]: https://github.com/VdBarros/Real-Ad-Game/issues/129
[#130]: https://github.com/VdBarros/Real-Ad-Game/issues/130

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
    P == E.power  ->  TIE   : E drains P while contact holds; E unchanged; return
    P <  E.power  ->  LOSS  : E drains P while contact holds; E unchanged; return

tap(Additive A)   ->  P += A.value ; consume A
tap(Multiplier M) ->  P *= M.value ; consume M
tap(Boss B)       ->  same as Enemy; WIN also ends the level
```

A tie is not a win: **affordable means `P > E`**, never `P >= E`. A `P == E`
transition consumes nothing and is not a legal move in any reasoning about the
level ([#9]). `Tie` stays a separate outcome from `Loss` because it drives its
own animation and its own aim preview; no validator or oracle reads it.

**A tie or a loss costs power — the drain ([#135]).** Winning was free and
losing was impossible, so the game carried no risk. Contact with a node that
will not fall now bleeds the player's number while the contact is held:

```
Drain.Floor        = 1      // the run continues at 1; there is no game over
Drain.RampSeconds  = 0.30   // rate ramps 0 -> full over the first 0.3 s
Drain.Seconds      = 2.00   // contact held that long takes any P to the floor

spent(t) = t < R  ->  V * t^2 / 2R          // V = 1 / (Seconds - R/2)
           t >= R ->  V * (t - R/2)
P(t)     = Floor + ceil((P0 - Floor) * (1 - spent(t)))
```

The drain is a fraction of the span `P0 - Floor` per second, not an absolute
rate, so it reads the same at 54 and at 54 000, and it reproduces the
reference's 54 -> 44 -> 30 -> 16 -> 1 over two seconds. **The ramp is not
cosmetic**: it makes touching a wall a probe. Pulling out inside a frame costs
nothing, a quarter-second brush costs 3 of 59, and only holding on costs
everything — so a player can test a wall and learn its number without being
punished for asking. Breaking off mid-drain (#133) keeps whatever is left.

**The enemy's value never moves.** Confirmed in the reference: the boss holds
55 from first frame to last. Nothing is consumed, no corridor opens, the run
returns to the node it came from, and only `P` is different.

**A drain is not a move.** It consumes nothing, so it adds no ordering to any
reasoning about the level, and every invariant below is quantified over
orderings of moves that *consume*. Two consequences, both real:

- The **power ceiling still bounds a draining run.** Affordability and every
  gain are monotone in `P`, so a run that has lost power is bounded by the same
  `(P0 + ΣA) * ΠM` as the run it came from, and the floor keeps it at or above
  1.
- The **oracle's state space is unchanged** — measured, not assumed: peak
  `(consumed-set, power)` counts on `tiny` mutants stayed at p50 199 / p90 1747
  / max 16177 across the change. Admitting drained states would not have grown
  it, it would have destroyed it: from `(mask, 1)` almost every level has no
  affordable reachable node, so Invariant A would fail everywhere and no level
  would generate. A player who throws their power away can therefore walk into
  a corner Invariant A never promised them out of — a stall reachable only by
  draining, never by playing. Pickups stay affordable at any power, so it takes
  a state whose only affordable reachable node is an enemy.

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

- The **tile grid** is ~60 walkable `(x, y)` cells, each carrying an elevation.
  Geometry: what renders, and where a walk may physically go. No two cells share
  an `(x, y)` (see [ADR-0001](adr/0001-terraces-instead-of-stacked-floors.md)).
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

**The gate product is capped** — `M ≤ 0.5 * strip target / P₀`, checked at
placement and again by the validator, and rejected into the same retry loop as
every other reason ([#129]). Gates come off a fixed `{2, 3, 4}` ladder cycled
over however many the recipe asks for, placed without regard to where they sit,
so it is the product that explodes rather than any one factor. Capping `Π` over
*every* multiplier in the level, ignoring routes, is an upper bound on the
product any single route can collect: conservative, never wrong in the
dangerous direction, and free — where an exact per-route bound is a
subset-selection search over a cyclic decision graph, since gates are consumed
once. It costs nothing today. The ladder cannot mint more than 24 on `tiny` and
`ship` or 96 on `stress`, against caps of 50, 150 and 500, so the
ten-thousand-seed sweep turns away no attempt at all and the per-reason
histogram is unchanged. What it buys is a guard on the recipe: a fourth gate on
`tiny` and a fifth on `ship` are refused on two ladder shuffles in three.

**Dead time is budgeted in seconds** — no reasonable route walks more than
`2.0 s` with nothing happening, which at the one walk speed the game owns
(`Pace.StepsPerSecond`, 4 tiles a second, read by `Walk`) is **8 tile steps**
([#130]). The seconds are the number; the tile count is derived, so the two
cannot drift apart.

A *beat* is a tile a route can meet something on: a content node, the boss, the
start, or a climbing tile — a flight of stairs is a traversal moment with its
own camera and animation, and it is also the one stretch the generator is
forbidden to place a slot on, so counting it as silence would make the budget
unsatisfiable by construction rather than by tuning. Everything else is
silence. The measure charges **every edge of the maze** with the silence forced
on either side of it: `crossing(u, v) = d(u) + d(v) + 1`, where `d` is the tile
distance to the nearest beat. Any route crossing that edge must walk at least
that far between two beats, so the maximum over all edges is the worst dead
time over every route that does not deliberately double back — exact on
corridors and trees, and where a route walks past content within one step
without taking it, it charges the shorter figure, because content one step off
your path is not silence. This is stronger than a budget over one named walk:
it holds over `ParWalk`, `PoorWalk` and the beeline alike because it holds over
every pair of beats, not over one traversal.

It is enforced twice. `SlotSelector` spends its free slots on the deepest
silence first, but **only while the budget is broken** — the moment the layout
fits, the remaining slots go back to the shuffled order, so levels that were
already well spaced keep the shape the braid was measured at. The validator
then refuses anything left over as `DeadWalkBeyondBudget`, into the same
fifty-attempt retry loop as every other reason. Before the change the worst
walk was 23 steps (5.75 s) on `ship`, 9 on `tiny` and 47 on `stress`; after it
the worst over the whole ten-thousand-seed sweep is 8 steps on every preset and
every plan, and the retry loop turns away no attempt at all — the rejection is
the backstop, the slot pass is the fix. Rejection rates moved *down* slightly
(ship 12% → 11.4%, tiny 43.1% → 42.5%, level 7 44.7% → 42.5%).

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

Checked by an **adversary panel of six policies**, not one ([#9], T-07). The
original single greedy-worst walk — multiplier, then addition, then cheapest
affordable enemy — is **unsound in the dangerous direction**: measured against
the exhaustive oracle it missed 14 real stalls in 2292 mutated levels. It never
false-alarmed, so every failure it reports is real, but silence from it did not
mean safe.

The panel is that policy plus five siblings — additive-first, enemy-first,
biggest-additive-first, biggest-multiplier-first, and additive-last (multiplier,
then cheapest affordable enemy, then addition). **A level fails if any policy
strands.** Six walks cost microseconds each. The original priority order stops
being the *definition* of worst and becomes one member of the panel.

Additive-last is T-07's addition, and it is the ordering that keeps final power
lowest: an addition taken before a multiplier gets multiplied, so the adversary
that never takes one early is the one that arrives poorest. On the fuzz suite's
6000 `tiny` mutants the five-policy panel missed 31 real stalls; the six-policy
panel misses 7. The other two type orders — enemy-then-addition and
addition-then-enemy — were measured and caught nothing the six already had.

Generation retries a rejected level, **capped at 50 attempts**, then throws
with a per-reason histogram rather than looping.

**The brute-force oracle only has teeth under mutation** ([#9]). On unbroken
levels greedy and exhaustive agree vacuously — 0 stalls in 118 `tiny` levels —
because #8 mints values during the adversary's own walk, so affordability is
true by construction. T-07 must therefore *break* levels first (inflate one
enemy ×3 / ×10 / ×50, one at a time) and compare the panel's verdict with the
oracle's on every mutant. **"Agree" means identical verdict, never identical
consumed set** — the oracle explores all orderings, the panel walks six, so
their consumed sets differ legitimately on levels both call safe.

**Exact agreement is not a property any fixed set of greedy walks can have**,
and T-07 measured the gap rather than pretending otherwise. Over 6000 mutants
(400 `tiny` levels × 5 enemies × 3 factors): the oracle finds a stall in 3875,
the panel in 3868, it **false-alarms 0 times** and **misses 7 of the 3875
stalls there were to catch** — 0.18%. The suite therefore fails the build on a
single false alarm, because every stall the panel reports must be real, and on
a miss rate above 0.4% of the oracle's stalls, which is the measured residual
with headroom. Both residual and bar are `tiny`-mutant
numbers: no unmutated level in the ten-thousand-seed sweep stalls at all.

The oracle runs on **`tiny` only**; `ship` blows a 200k state budget six times
in six, ~610 ms each. Peak `(consumed-set, power)` state count on `tiny` mutants
is a median of 181, p90 1746, max 16177, so it runs inside the fast loop rather
than as a separate job.

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

Both walls are searches over the **same reachability the game runs on** — only
an unconsumed enemy or boss is a door, a pickup is not ([#2]) — rather than the
stricter "everything on the way must be consumed" rule #8's prototype used.
That is a real change of number, not just of wording: a region no enemy gates
becomes reachable earlier and therefore cheaper, so `P_min` falls and the
spread widens. The alternative is a domain that disagrees with `RunState` about
what the player can reach, which T-06's panel and T-07's oracle both walk.

The `P_min`/`P_max` spread is the skill expression. Tune it, don't collapse it.
Measured `P_max/P_min` p10/50/90 = **1.0 / 46.1 / 492** over 500 `ship` seeds
(#8's prototype measured 1.0 / 14.3 / 193.2 under its own stricter
reachability); the p10 of 1.0 is the start region, where both walls are `P₀` by
definition.

A region whose **only** content slot is the boss carries no enemy and so has no
floor rule to honour — 8 of 2000 regions across those seeds. Every other region
does: after roles are drawn, a region left with treasure and no enemy swaps one
of its pickups for an enemy from the region holding the most, which keeps the
recipe's counts exact.

### Presets ([#4])

| | `tiny` | `ship` | `stress` |
|---|---|---|---|
| purpose | exhaustive verification | what ships | generation-time regression |
| content nodes | **11** | 24 | 90 |
| tiles | — | ~60 | — |
| terraces | 1 | 2 | 3 |
| regions | 2 | 4 | 9 |
| `D_min` | — | **16** | — |

`ship`'s `D_min` was lowered from 20 to 16 on measured rejection rates ([#7]).
The carve alone already spends ~10% of `ship` seeds and ~32% of `tiny` seeds;
envelope rejections stack on top, so the **combined** rate is what has to stay
sane. Re-measured on `ship` after terracing ([#58]): **441/500** seeds accepted
on the first attempt against 443/500 before, and over 5000 seeds **12.0%** of
attempts rejected against 10.91% before, with no new rejection reason. `tiny` is
one terrace and is unchanged to the seed: 43.11% of attempts either way.
Terracing moves the reasons around rather than adding to them — `BossTooShallow`
falls from 140 to 2 because a way up now sits at the back of a terrace, and
`TooFewOffPathSlots` rises from 442 to 561 for the same reason. That same shift
halves what routing is worth: the median region's `P_max/P_min` spread falls
from 48 to 24.5. Nothing in the power reasoning changed; the levels did.

`tiny` counts **11** content nodes, not 12. #8's recipe — 1 boss, 3
multipliers, 5 enemies, 2 additives — totals 11, and `Start` is geometric
rather than a slot, so it must not be counted into the preset's slot budget. A
recipe that does not match its preset exactly is a rejection, so the two have
to agree.

### Maze construction ([#7])

Recursive backtracker per terrace → **braid 0.25** → staircases → **Voronoi**
regions → extract the decision graph.

Terraces are offset by `(Δ, Δ)` tiles and lifted two steps, Δ chosen so that
consecutive footprints are disjoint with an unowned row between them — Δ = **6**
for `ship`. That offset is along world `x + y`, which is straight up the screen,
so it costs no width: the `ship` level already spends **79%** of a 1080×1920
portrait frame's width at orthographic size 9.5 and only **31%** of its height.
Growing the map sideways is not available; growing it up-screen has threefold
headroom. Screen separation at the worst-aligned column is **3.15 m** against
the **1.94 m** a boss badge needs to clear the terrace above.

Braid is the load-bearing knob. At 0 the maze is 53% gates and has a single
forced order; at 1.0 it is 3% gates and no puzzle. **0.25 gives ~30% gates and
~2.3 pockets per level**, and the placement policy has to fit that supply.

A tile becomes a decision node when its corridor-degree is not 2, or it is the
start. Junctions promote to never-consumed `Empty` nodes. **The empty-path
assertion and the exemption it needed are both gone**, because there is no longer
a zero-length corridor: a staircase is a run of ordinary walkable tiles from a
lower terrace's far edge to the next terrace's near edge, and its tiles have
corridor-degree 2 like any other corridor tile. The tile at the foot gains a
neighbour and so promotes to a junction on its own.

A way up leaves from the far row of the lower terrace, at a lattice column
chosen at least three cells from every other way up over the same gap. Where the
column already lines up with a column of the terrace above — Δ/2 ≤ c ≤ W−1 —
the staircase is a **single tile** in the unowned row. Where it does not, the
staircase steps into the unowned row and then runs level along the row **above**
it, which is the terrace above's near row extended sideways into empty space,
until it reaches that terrace's leading column. It cannot instead run along the
unowned row itself: that row is adjacent to the lower terrace's far row along
its whole length, so every tile of such a run would gain neighbours and the
staircase would become a ledge rather than a flight of steps. Running one row
higher, a staircase touches nothing but its own two ends. Successive ways up over
one gap take successive landing rows, leftmost highest, so their runs never
cross; they therefore arrive spread down the leading column of the terrace above
rather than piled into its corner. The L costs ~4 tiles per staircase (~8 for
`ship`, +13%); confining staircases to the straight-run window would put both of
`ship`'s next to each other, and two adjacent staircases are not a routing
choice. Measured: confining them costs 6 points of layout acceptance
(839/1000 against 902/1000) and lifts the mean gate ratio to 0.42, where the L
holds it at 0.35 against 0.31 before terracing.

Staircase tiles must be **excluded from slot candidacy explicitly**, at every
source of candidates rather than only at the corridor run. `SlotSelector` draws
from three: corridor runs, dead ends and junctions. A staircase is a corridor
run, so the run needs the exclusion or content is minted standing halfway up a
flight of stairs — but an L-bend can also give a climbing tile corridor-degree
one or three, which makes it a dead end or a junction and reaches the other two
sources. All three go through one gate, and the fuzz sweep asserts no decision
node on any generated level stands on a climbing tile ([#114]). A staircase
belongs to the region of the terrace it **leaves**, so the region boundary sits
at the top of the climb.

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
of the tile key rather than a counter, sibling order is the `(elevation, y, x)`
sweep that also assigns node ids, and no `Dictionary`/`HashSet` iteration
appears in the build path.

**The camera is three states** — reversing [#15] and [#16], which recorded two
and said follow mode does not exist. Rotation stays a constant
(`euler(30, 45, 0)`) and **play framing keeps orthographic 9.50**, but that size
no longer holds a whole level: at 9.50 a `ship` level spends **857 px of a 540 px
half-width**, and **56 of 56 seeds run off the frame**. So the opening flight
ends on a **reveal framing** that widens until every tile — plus the headroom a
boss and its badge need above one — is on screen, holds it for **0.6 s**, and
only then lets go. That reveal is the one moment the whole level is visible, and
it is what makes this a routing puzzle rather than an exploration game. It is a
per-seed fit (**10.06 to 12.57** over 56 `ship` seeds), never tighter than the
play size, which is where [#15]'s "the flight's endpoint needs no per-level fit"
stops being true.

Play framing then **follows the player**: the camera eases toward wherever the
player stands, rate-limited so automatic motion never crosses the **1000 px/s**
legibility ceiling — measured worst is 815 px/s on the opening and 750 px/s on
the follow. The settle off the reveal, the tracking during a walk and the return
from a drag are **one mechanism**: a drag suspends the follow and places the
camera directly, and letting go simply resumes it, so the return is the same
eased, rate-limited step the follow already takes and cannot cross the ceiling by
construction — worst measured 750 px/s over 56 `ship` seeds pulled to each of
eight horizons. Nominally the return is the follow's ~0.35 s; a drag long enough
that 0.35 s would outrun the ceiling takes as long as legibility needs instead.
The clamp is the level's bounding box plus one tile **measured in the camera's
own plane**, across and up rather than in world axes, because up the screen is
mostly world `y` and a world box would crush a vertical drag to nothing.
`ZoomBeat` outranks the follow exactly as it outranked the held frame, and hands
back to the player rather than to a constant. See
[ADR-0001](adr/0001-terraces-instead-of-stacked-floors.md) for why.

The follow's profile starts at the rate limit rather than easing in, because it
is a position filter whose speed peaks on its first frame and is clipped there.
[#61] left it alone: an ease-in would need the filter to carry elapsed time or
velocity, and a camera that starts slowly is worse on the one leg that matters
most — a release should feel like letting go, not like being let go of.

`TapHold` grows a position to make this possible: a press that travels more than
the 4.5 mm touch reach becomes a pan and forfeits its tap — for the whole of that
press, so straying and coming back does not buy the tap back — and below that
threshold sliding still re-aims. The threshold is that reach and no other number.
**The rig still exposes no rotation field**,
which is what lets #11 copy each badge's rotation once at construction instead
of billboarding every frame.

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
