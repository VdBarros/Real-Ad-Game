# Elevation is a per-tile integer, and terraces never overlap on screen

## Status

accepted

## Context

The tile grid addressed tiles by `(floor, x, y)` and carved every floor on the
same lattice footprint, so floor 1 sat directly above floor 0 with two world
units between them. Under the fixed isometric framing — `euler(30, 45, 0)`,
orthographic size 9.5 — that stacking is not merely ugly, it is unplayable:
a node on floor 1 at `(x, y)` projects **32 px** from a node on floor 0 at
`(x + 2, y + 2)`, and the finger reach is **71 px**. `TapAim` picks the nearest
candidate in screen space and only consults depth when two distances are exactly
equal, so the occluded lower node wins by a fraction of a pixel. Playtesting
found it the obvious way: tap the enemy you can see, walk to the one you cannot.

The upper floor also hid the lower one, which breaks the premise the whole
design rests on — the reference ad shows the entire map before play, and that
reveal is what makes this a routing puzzle rather than an exploration game.

## Decision

The storey is gone. A tile is addressed by `(x, y)` plus an **elevation**, an
integer count of one-world-unit steps, and no two tiles share `(x, y)`. A
**terrace** is a maximal set of tiles at one elevation; it is emergent, never
stored. Consecutive terraces are offset by `(Δ, Δ)` tiles — chosen so their
footprints are disjoint with an unowned row between them — and lifted two steps,
so a terrace floats above and behind the one before it with nothing underneath.

A **staircase** is an ordinary run of walkable tiles climbing one step per tile
from the lower terrace's far edge to the upper terrace's near edge. It is not a
special adjacency: `TileGrid.Neighbours` is four-neighbour and nothing else, and
`StairLink` is gone along with the zero-length corridor it used to justify.

## Considered options

**Fix the renderer and the picker, keep the stack.** Hide or fade the
non-current floor and require visibility in `TapAim`. Two files, no domain
change. Rejected: hiding a floor destroys the full-map reveal the routing puzzle
depends on, and a visibility test *adds* a special case where flattening
*removes* three (stair adjacency, the zero-length corridor, its assertion
exemption).

**Grow the map horizontally**, which is how the problem was first described.
Rejected on measurement: at orthographic size 9.5 on a 1080×1920 portrait frame,
the `ship` level already spends **79% of the available width and 31% of the
height**. Screen-right is world `x − y`; screen-up is world `x + y` plus
elevation. Growing horizontally forces either a zoom-out that halves the tile
size or a camera that never shows the whole map. Terracing along the `x + y`
diagonal is free in width and has threefold headroom in height.

**Keep the floor as a coordinate, only reposition it.** `StairLink` survives at
different `(x, y)` instead of the same. Smallest diff. Rejected because it
leaves the claim false: the floor would still be a real coordinate with an
adjacency existing solely to serve it. Per-tile elevation is the only version
where "the floor is only visual" is true of the model and not just of the render.

**Uniqueness of `(x, y)` versus screen non-overlap.** These are different
invariants and the cheaper offset satisfies only the second. Both are kept: the
`(x, y)` invariant is one assertion and it makes the class of bug that started
this impossible to represent, rather than merely absent.

## Consequences

The camera becomes **three** states, not two, reversing what [#15] and [#16]
recorded. Play framing follows the player; a drag pans away from it and release
eases back over ~0.35 s, clamped to the level's bounding box plus a tile. The
opening flight still ends on the whole-level frame and holds before easing to
the player, because that reveal is the routing puzzle. `ZoomBeat` still outranks
the follow, exactly as it outranked the held frame.

`TapHold` gains a position and `TapGesture` gains `Pan`: a press that travels
more than the 4.5 mm touch reach becomes a pan and forfeits its tap. Below that
threshold, sliding still re-aims, which is an affordance worth keeping.

Nothing in the power reasoning changes. `P_min`, `P_max`, the floor rule, the
adversary panel and the oracle read `Neighbours` and never looked at the storey.
Regions stay bound to one terrace, and a staircase belongs to the region of the
terrace it leaves, so the upper terrace's `P_min` still reads as power on
arrival. Staircase tiles have corridor-degree two, so they stay corridor tiles
for free — but they must be excluded from slot candidacy explicitly, or content
gets minted standing halfway up a flight of stairs.

The serialized member `"floor"` becomes `"elevation"`. There are no golden files
on disk; `LevelGraphDocumentTests` builds its expected document inline.
