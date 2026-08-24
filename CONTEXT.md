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

## Open terms

Not yet defined; each is owned by an open ticket.

- **Power envelope**, **`P_min`**, **`P_max`** — precise meaning under gating,
  [#8](https://github.com/VdBarros/Real-Ad-Game/issues/8)
