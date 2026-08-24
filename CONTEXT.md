# Context — Number Maze

Glossary for the domain. No implementation detail lives here.

Terms are added as decisions settle them. Open terms are listed at the bottom.

## Tile

One walkable cell of the dungeon, addressed by integer `(x, y, floor)`. Tiles
define geometry: what renders, and where a walk may physically go. A `ship`
level holds roughly sixty of them.

## Tile Grid

The set of all tiles in a level. Owned by the domain — integer coordinates are
plain data and carry no engine dependency. Rendering and walk pathfinding read
the tile grid; nothing else does.

## Decision Node

A tile lifted into the decision graph. Two kinds exist: content nodes and
junction nodes. Every decision node is a tile; most tiles are neither.

## Content Node

A decision node that holds something the player can act on: the start, an
enemy, the boss, an additive pickup, or a multiplier pickup. A `ship` level
holds roughly twenty-four. Content nodes are what the size presets count.

## Junction Node

A decision node that holds nothing. It exists because three or more corridors
terminate at that tile, or because the tile changes floor. Junction nodes are
never consumed, so they carry no state and enter no reasoning about power.

## Stair

The only thing that joins two floors: a pair of tiles at the same `(x, y)` on
adjacent floors, walkable in both directions. Tiles that merely sit one above
the other are not connected — nothing but a stair crosses a floor, so a stair
is data the level carries rather than a coincidence of coordinates. The
corridor joining a stair's two tiles covers no tiles at all; it is the one
corridor that is zero-length by construction.

## Corridor

Exactly one decision-graph edge, together with the run of tiles it covers. A
corridor never branches — a fork is a junction node, not a corridor. Every tile
in a corridor's interior belongs to that one corridor and no other.

## Decision Graph

The graph of decision nodes extracted from the tile grid, connected by
corridors. All reasoning about whether a level is playable — solvability, the
power envelope, fuzzing — happens here, never on the tile grid. The graph is
undirected: nothing in the game is one-way.

## Region

A contiguous group of tiles, used to scale content. Regions partition the tile
grid completely: every tile belongs to exactly one, and none spans two floors.
A decision node's region is simply its tile's region, so nothing is ever
region-less. A corridor is said to cross a region boundary when the tiles at
its two ends belong to different regions.

## Power

The single number the player carries. It is monotonically non-decreasing: no
rule in the game reduces it. Defeating an enemy raises it, so an enemy is an
addition with a gate attached.

## Consumed

The state of a decision node that has been resolved — enemy defeated, pickup
taken. A consumed node is inert and cannot be acted on again. It does not stop
being a tile: consumed nodes are walkable.

## Reachable

A tile is reachable when a path of tiles connects it to the player's position
without passing through an unconsumed enemy or the boss. Reachability grows as
nodes are consumed and never shrinks.

## Route

The run of decision nodes a tap commits the player to, from where they stand to
the node they tapped. It is the shortest such run, and everything on it resolves
in order — a route is a plan, not a suggestion. Only its last node may be an
unconsumed enemy, since every other node on it was walked through; a tie or a
loss there leaves the player standing on the node before it, holding whatever
the walk already earned.

## Cleared

A tile is cleared when it can be reached from the start once every consumed
node is treated as walkable. Cursed is the opposite. Clearing is what the floor
material shows, and it is nothing more than a reading of the consumed set — the
enemy you just defeated is consumed, so its tile and the corridor behind it
turn at the same moment.

## Frontier

The unconsumed enemies adjacent to reachable space. These are the nodes the
player may engage right now. An enemy deep behind another enemy is not on the
frontier until the one in front of it dies.

## Gate Enemy

An enemy standing on an articulation point of the decision graph: killing it is
the only way to open what lies beyond. Gates control pacing. Whether an enemy is
a gate is read off the graph whenever it is asked for; it is never a fact stored
on the enemy.

## Pocket Enemy

An enemy in a dead end, guarding a reward and nothing else. Optional. Pockets
control greed.

## Slot

A content node whose content does not exist yet. Layout decides where the slots
are and fixes them; content placement decides what fills each one and may not
move, add or remove any. No slot survives generation unfilled.

## Preset

A named level size the generator can be asked for. `tiny` exists so exhaustive
verification is tractable, `ship` is what the game ships, `stress` measures
generation time. A preset counts content nodes; junction nodes are whatever the
layout happens to need.

## Level Graph

One complete generated level: its tile grid, its decision graph, and the
content sitting on the content nodes. It is what the generator returns and the
only thing presentation is given. A level graph is a pure function of its seed
and preset — the same pair always yields the same level.

## Braid

The proportion of the maze's dead ends that are reopened into loops after
carving. It is the one knob that decides how much of the level is a gate and
how much is an optional pocket: none of it braided gives a maze with a single
forced order, all of it braided gives a maze with no gates and no puzzle.

## Affordable

An enemy is affordable when the player's power is **strictly greater** than the
enemy's. Equal power is a tie, and a tie is a no-op — it changes nothing and
opens nothing, so it is never progress and never a legal step in any reasoning
about whether a level can be finished.

## Stall

A state from which no unconsumed node is both reachable and affordable. The
run cannot continue and cannot fail; it is simply stuck. Invariant A is the
promise that no reachable state of a shipped level is a stall.

## Power Envelope

The pair of walls a region carries: the least and the most power a player can
arrive there holding. The distance between them is what routing skill is worth,
which is why the envelope is tuned rather than collapsed — a region whose walls
meet is a region where the route did not matter.

## `P_min`

The cheapest way into a region: consume as little as possible, always taking
the smallest power gain available, and stop the instant the region is
reachable. The lower wall of the envelope.

## `P_max`

The richest entry into a region: take everything outside it first, in the best
order, and only then enter. The upper wall of the envelope, and what good
routing is worth.

## Adversary

A policy that plays a level badly on purpose, to find out whether playing it
badly can get stuck. Validation runs a **panel** of them rather than one, and a
level is rejected if any single policy stalls — no adversary is *the* worst,
so agreement across a panel is the claim being made.

## Oracle

Exhaustive search over every order a level could be played in, used to check
what the adversary panel might have missed. It is only tractable on the
smallest preset, and it only says anything useful about levels that have been
deliberately broken first: on a level that is already correct it finds nothing,
which proves nothing.

## Open terms

Not yet defined; each is owned by an open ticket.

None currently open. Camera vocabulary is deliberately absent: framing, cuts
and beats are presentation, and this glossary is the domain's.
