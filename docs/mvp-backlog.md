# MVP Product Plan — Number Maze (working title)

Companion to `ad-analysis.md`. That document is the source of truth for *what
the ad showed*. This document is the source of truth for *what we are building*.

---

## 0. Standing instructions for all agents

- **Unity 6000.5.9f1, URP, portrait-only, mobile target, new Input System.**
- **Do not write comments in code.** Names carry the meaning. Exception: a
  comment is explicitly requested in a task.
- Free / built-in Unity assets and primitives only. Art is placeholder; do not
  spend time on it.
- Every task lists acceptance criteria. A task is done when those pass, not
  when the code compiles.
- Domain code (Phase 1) must not reference `UnityEngine`. It is plain C# in its
  own assembly definition so it can be unit-tested without entering play mode.
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
  where the invariant-correctness risk sits (see §5).

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

Only reachable, adjacent-by-graph nodes are tappable. Consumed nodes are inert.

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

**Invariant C — boss requires a detour**

```
boss.power > power of a player taking the shortest path to the boss
```

This is what makes decision 12 true rather than aspirational.

**Invariant A — progress never stalls**

From any reachable state, at least one unconsumed node must be both reachable
and affordable. Checked by greedy-worst simulation: repeatedly take the
affordable unconsumed node that is *worst* for the player (multipliers before
additions). If that simulation consumes the whole graph, the level is safe.

This one is a strong practical check, not a proof. Back it with fuzz tests
(T-07) rather than trusting it.

### Region scaling

Per region `R`, computed during generation:

- `P_min(R)` — power of the worst route that can reach `R`
- `P_max(R)` — power of the optimal route that can reach `R`

```
min(enemy power in R) <= P_min(R)      // floor: something is always edible
max(enemy power in R) ~  P_max(R)      // ceiling: good routing is rewarded
```

The `P_min`/`P_max` spread is the skill expression. Tune it, don't collapse it.

---

## 3. Architecture

The maze is **a graph, not a world.** The isometric scene is a view of that
graph. This split is the reason agents can work in parallel and the reason the
generator is testable at all.

```
Domain (plain C#, no UnityEngine)      <- Phase 1
  LevelGraph, Node, Edge, NodeType
  LevelGenerator (seeded)
  PowerEnvelope, SolvabilityValidator
  RunState, ActionResolver

Presentation (Unity)                   <- Phase 2
  WorldBuilder, BadgeSystem, VisualTiers
  CameraRig, FloorStateController

Interaction (Unity)                    <- Phase 3
  TapInput, GraphPathfinder
  EncounterController, PickupController

Flow (Unity)                           <- Phase 4
  GameStateMachine, PillarCutscene
```

---

## 4. Task backlog

Dependencies in brackets. Tasks with no shared dependency can run in parallel.

### Phase 0 — Foundation

**T-01 — Project bootstrap**
Unity 6 LTS, URP, portrait-locked, new Input System. Assembly definitions:
`Game.Domain` (no Unity refs), `Game.Domain.Tests`, `Game.Presentation`,
`Game.Interaction`, `Game.Flow`. Folder structure, .gitignore, one empty scene.
*Accept:* builds to Android; `Game.Domain` compiles with zero UnityEngine
references; an empty NUnit test runs outside play mode.

> `Game.Domain` and `Game.Domain.Tests` (with a smoke test) already exist at
> `Assets/Scripts/Domain(.Tests)/`, plus the standalone `dotnet test` project —
> bootstrapped early because Phase 1's feedback loop was worth having before
> Phase 1 itself started. What's left for T-01: `Game.Presentation`,
> `Game.Interaction`, `Game.Flow` asmdefs, Android build config, portrait
> lock, Input System setup, and the bootstrap scene.

### Phase 1 — Domain [T-01]

**T-02 — Graph data model**
`Node` (id, type, value, regionId, position), `Edge`, `LevelGraph`. Node types:
`Start`, `Empty`, `Enemy`, `Boss`, `Additive`, `Multiplier`. Adjacency queries,
serialization to JSON for test fixtures.
*Accept:* a hand-built 10-node graph round-trips through JSON; adjacency
queries return correct neighbours.

**T-03 — Run state and action resolution** [T-02]
`RunState` (current power, current node, consumed set). `ActionResolver`
implementing §2 Resolution exactly. Returns a result enum (`Win`/`Tie`/`Loss`/
`Pickup`) plus the new state. Pure function, no side effects.
*Accept:* unit tests cover all five outcome branches including the tie case;
power never decreases in any test; tapping a non-adjacent node is rejected.

**T-04 — Maze layout generator** [T-02]
Seeded topology only — corridors, junctions, elevation changes, region
partitioning. No content values yet. Same seed produces an identical graph.
*Accept:* 1000 seeds all produce fully-connected graphs; identical seed →
byte-identical JSON.

**T-05 — Content placement and power envelope** [T-04, T-03]
Place enemies, additives, multipliers, and the boss. Compute `P_min`/`P_max`
per region by forward simulation. Apply the region scaling rules in §2.
*Accept:* every region has at least one enemy ≤ `P_min`; `P_min ≤ P_max` for
all regions; boss placement satisfies Invariant C.

**T-06 — Solvability validator** [T-05]
Implement Invariants A, B, C as three independent checks returning a structured
failure reason. Generator retries with the next seed on failure, capped at N
attempts, and logs the rejection rate.
*Accept:* deliberately malformed graphs are rejected with the correct reason;
Invariant B is a pure O(n) computation.

**T-07 — Generator fuzz suite** [T-06]
Run 10,000 seeds. Assert every accepted level satisfies all three invariants,
and additionally brute-force-verify Invariant A on small graphs by exhaustive
state search to confirm the greedy-worst approximation holds.
*Accept:* zero invariant violations across the run; report of rejection rate
and mean generation time; brute-force and greedy-worst agree on all small
graphs.

### Phase 2 — Presentation [T-02]

Can start as soon as the data model exists — does not wait for the generator.

**T-08 — Isometric world builder**
Consume a `LevelGraph`, instantiate floor tiles, walls, stairs, and node
prefabs. Fixed dimetric camera angle. Placeholder primitives are fine.
*Accept:* a hand-authored fixture graph renders as a walkable-looking maze;
rebuilding from the same graph is deterministic.

**T-09 — Number badge system**
World-space badges above every node. Blue rounded-rect for player and pickups,
red pill for enemies. Prefix rendering (`+N`, `xM`, bare N). Count-up animation
for the player badge on power change.
*Accept:* badges face the camera at all times, stay legible at the mobile
portrait resolution, and never overlap their own node's geometry.

**T-10 — Player visual tiers and weapon drop** [T-08]
Power thresholds swap the player prefab through tiers. Defeated enemies drop a
weapon prop the player picks up and visibly carries.
*Accept:* crossing a threshold swaps the prefab and plays a level-up VFX;
weapon persists across subsequent fights.

**T-11 — Camera rig** [T-08]
Three modes: follow (default), level fly-through (level start, scripted path
over the whole maze), and pickup zoom beat (push in, hold, pull out).
*Accept:* fly-through covers every region; zoom beat returns exactly to the
follow framing; no clipping through geometry.

**T-12 — Floor state controller** [T-08]
Two floor materials, cursed and cleared. Clearing an enemy transitions its
corridor with an animated sweep.
*Accept:* only the corridor owned by the defeated enemy changes; state survives
camera moves; transition is under 1 second.

### Phase 3 — Interaction [T-03, T-08]

**T-13 — Tap input and target preview**
Raycast taps onto nodes. Hovering/holding highlights the target and shows the
predicted outcome (win / tie / loss) before commit. Non-adjacent nodes are
visibly non-tappable.
*Accept:* touch targets are at least 9mm on a reference device; prediction
always matches what `ActionResolver` subsequently returns.

**T-14 — Pathfinding and walk** [T-13]
Shortest path along graph edges to the tapped node, with a walk animation and
the dotted-path trail from the ad. Cancellable by tapping elsewhere.
*Accept:* path never crosses walls; cancelling mid-walk leaves the run state
untouched.

**T-15 — Encounter resolution** [T-14]
Drive `ActionResolver` on arrival and play the matching outcome: win (slash,
enemy dissolves, power counts up, corridor clears, weapon drops), tie
(clash-stalemate, walk back), loss (knockback, walk back).
*Accept:* tie and loss are visually distinguishable; both leave `RunState`
byte-identical to before the tap.

**T-16 — Pickup resolution** [T-14]
Additives and multipliers apply with the count-up animation and a zoom beat.
Consumed pickups leave an empty pedestal.
*Accept:* `x2` on power 5 yields exactly 10; consumed pickups are not
re-tappable.

### Phase 4 — Flow

**T-17 — Game state machine** [T-06, T-11, T-15, T-16]
`Boot → Cutscene → Generate → Preview → Play → BossDefeated → Result → Generate`.
Result panel shows final power and a Next button.
*Accept:* full loop runs 20 times without leaking objects between levels;
generation failure retries silently.

**T-18 — Pillar cutscene** [T-09, T-17]
Scripted, non-interactive, skippable. Player 5, girl 25, rival 99. Player
throws hearts, drops to 2, becomes a skeleton, girl counts to 50, her pillar
grows to the rival's height, she crosses, player falls through the portal into
the generated level. Numbers are hardcoded, no simulation.
*Accept:* plays start to finish under 20 seconds; skippable at any frame;
hands off to `Generate` cleanly.

---

## 5. Suggested execution order

1. **T-01** alone, first. Everything blocks on it.
2. Then two parallel tracks: **T-02 → T-03 → T-04 → T-05 → T-06 → T-07**
   (domain) and **T-08 → T-09 → T-10 → T-11 → T-12** (presentation).
3. **T-13 → T-14 → T-15 → T-16** once both tracks land.
4. **T-17**, then **T-18** last — the cutscene is pure polish and should not
   block anything.

The riskiest task by a wide margin is **T-05/T-06**. If the envelope maths is
wrong, the game is either unloseable and boring or stranding players despite
the guarantee. Get T-07 running early and treat its output as the real measure
of whether the design works.
