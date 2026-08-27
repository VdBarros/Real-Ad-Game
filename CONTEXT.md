# Context — Number Maze

Glossary for the domain. No implementation detail lives here.

Terms are added as decisions settle them. Open terms are listed at the bottom.

## Tile

One walkable cell of the dungeon, addressed by integer `(x, y)` together with an
elevation. Tiles define geometry: what renders, and where a walk may physically
go. No two tiles share an `(x, y)`, so no two tiles can ever occupy the same
place on screen. A `ship` level holds roughly sixty of them.

## Elevation

How high a tile sits, counted in whole steps of one world unit. It is the only
thing left of what used to be a storey: elevation changes what a tile looks
like and nothing about where a walk may go. Two tiles are neighbours because
their `(x, y)` are adjacent, never because of their heights.

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
terminate at that tile — which is why the tile at the foot of a staircase is one:
the staircase is the third corridor. Junction nodes are never consumed, so they
carry no state and enter no reasoning about power.

## Terrace

Every tile at one terrace elevation, taken together. A terrace is read off the
tiles rather than stored, and consecutive terraces never share an `(x, y)`: each
sits above and behind the one before it with an unowned row between them and
nothing underneath. A staircase's tiles stand at the odd elevation between two
terraces and are on neither. The impression of having climbed a floor is all a
terrace is.
_Avoid_: floor, storey, tier.

## Staircase

The run of tiles that climbs from a lower terrace's far edge to the next
terrace's near edge, one step up at each end and level in between. It bends into
an L where the two terraces' columns do not line up, running through the space
that is beside the terrace above and beyond the terrace below, so that it is
never beside anything but its own two ends. A staircase is ordinary walkable
corridor — no adjacency of its own, never a slot, and never a decision node,
since its tiles have corridor-degree two like any other corridor tile.
_Avoid_: stair, stair link, ramp.

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
grid completely: every tile belongs to exactly one, and none spans two terraces
— a staircase belongs to the region of the terrace it leaves, so a terrace's
`P_min` reads as the power a player arrives holding. A decision node's region is
simply its tile's region, so nothing is ever region-less. A corridor is said to
cross a region boundary when the tiles at its two ends belong to different
regions.

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

A tap names a destination, not a path, and the game picks the route. A player
who wants a different path expresses it by tapping its nodes in turn, so route
choice is a sequence of taps rather than a property of one.

## Outcome

What a tap turned out to be. A **win**, a **tie** or a **loss** where the route
ended on an enemy; a plain **walk** where it ended on anything else, pickups
included. A tap the rules refuse — at a node that is not reachable, or after the
boss has already fallen — is **rejected**, and a rejected tap has no route and
changes nothing.

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

## Elite

An enemy nobody can afford on arrival: its number stands above the power the
cheapest way into its region leaves the player holding. An Elite is a locked
door rather than a threat — meeting one costs nothing, it simply does not open,
and the way through it is to go elsewhere, come richer, and come back. Whether
an enemy is an Elite is read off its number against its region's `P_min`
whenever it is asked for; it is never a fact stored on the enemy, exactly as
with a Gate Enemy. An Elite is also not a kind of content: the recipe counts
enemies, and which of them turn out to be Elites is a reading of the finished
level.
_Avoid_: mini-boss, blocker, locked enemy.

## Slot

A content node whose content does not exist yet. Layout decides where the slots
are and fixes them; content placement decides what fills each one and may not
move, add or remove any. No slot survives generation unfilled.

## Layout

Everything about a level that is decided before any content exists: which tiles
are walkable, where the stairs and regions are, which tiles are decision nodes,
and which of those are slots. Layout is fixed once it is done — the topology a
level ends up with is the topology layout chose, and nothing downstream moves,
adds or removes a node. A layout that fails its own checks is discarded whole
and a fresh seed tried, because there is no repair that would leave the
topology honest.

## Preset

A named level size. `tiny` exists so exhaustive verification is tractable,
`ship` is what the game ships, `stress` measures generation time. A preset
counts content nodes; junction nodes are whatever the layout happens to need. A
preset says how big a level is and nothing about how hard it is: the level that
ships is the same size at every level number, and a preset alone no longer
decides which numbers end up on it.

## Level Graph

One complete generated level: its tile grid, its decision graph, and the
content sitting on the content nodes. It is what the generator returns and the
only thing presentation is given. A level graph is a pure function of its seed
and its **plan** — the size a level number is played at, the recipe it fills,
and the numbers generation is tuned with, taken together — and the same pair
always yields the same level. A seed and a size are not enough to name a level:
two levels of the same size can differ in every number on them.

## Lattice

The coarse grid of rooms a terrace is carved out of, before it becomes tiles.
One lattice cell becomes one tile, and the wall between two joined cells
becomes another, so a terrace holds rather more tiles than the lattice holds
cells. The lattice is a preset's way of saying how big a terrace is; nothing
outside carving knows it exists.

## Carve

Cutting a terrace's corridors out of its lattice, by walking the cells in a
random order and knocking through the wall behind each step. Carving alone
produces a maze with exactly one route between any two tiles, which is why
braiding follows it.

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

## Par

What routing a level well is worth, expressed as the two powers a run can
finish it holding: the beeline to the boss at the bottom, and the richest entry
into the boss's region at the top. Both walls count the boss the run had to beat
to finish, so a Par and the number it rates are the same kind of number. Where a
finished run's power landed between the two is what the result reports back to
the player, and a Par whose walls meet rates nothing, because the routing on
that level was worth nothing. Par is read off the completed level rather than
authored beside it, so it cannot drift from the level it belongs to, and it
gates nothing — a level always leads to the next one.

## Recipe

How many of each kind of content a level holds: one boss, and a fixed count of
multipliers, enemies and additives. It is a count, never a set of numbers — the
numbers are minted per level. A recipe belongs to a level's plan rather than to
its size, so levels of the same size hold different mixes at different level
numbers; only the total is fixed, because a recipe that does not ask for exactly
the slots the carve offers is a rejected level, not a squashed one.

## Minting

Choosing a content node's number, done **during** an adversary's own walk
rather than before it: the walk reaches for a node, and the number is chosen
then, against the power the walk actually holds. An enemy that walk could not
afford is therefore never minted on the Spine, so a route that always finishes
is produced directly instead of being produced and then filtered. What the walk
never reaches is minted afterwards, against the most a route could arrive
holding rather than the least, which is where Elites come from.

## Spine

The walk a level's numbers are minted along: the poorest route anyone could
take, followed only as far as the moment the boss becomes affordable. Every
enemy on the Spine is affordable when the Spine reaches it, so the Spine is one
route that finishes the level however badly it is played. It is read off the
level rather than stored, and what it never touches is where a level's locked
doors live.
_Avoid_: critical path, main line, backbone.

## Floor Repair

The pass that makes the floor rule true after minting has finished. The floor
here is the bottom of a range of numbers, and has nothing to do with a terrace. `P_min` is
a property of the completed level, so minting cannot see it; instead each
region's cheapest enemy is pulled below that region's `P_min` afterwards.
Lowering a number only ever lowers power, so the walls move down underneath the
repair and it iterates to a fixed point.

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
