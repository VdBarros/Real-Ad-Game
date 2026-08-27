# A staircase is laid against its own climb, because the pack's flight descends along its own forward

## Status

accepted

## Context

T-23 gave every staircase tile the pack's `stairs_narrow` mesh and asserted,
10 of 10 on the `ship` seed, that *every staircase faces the way its own climb
goes, the L bend included*. Eighteen assertions held. On device no tread was
ever visible, and #68 recorded two apparently separate defects: **masonry
filling the drop** rather than steps, and **a row of triangular buttresses**
under a level interior run.

The mesh was measured before anything was changed. `stairs_narrow` imports at
`1.0001 x 1.2751 x 1.0001` from `(-0.5, 0, 0)`, and its crest over ten slices
of its own local z reads

```
1.275  1  0.875  0.75  0.625  0.5  0.475  0.375  0.25  0.275
```

So the pack's flight **descends** along its local +Z: the tallest mass sits at
the mesh's own origin and the lowest at its far end. T-23 pointed that local +Z
**along the ascent**. Every flight in the shipped world is therefore installed
backwards — the landing at the foot of the climb, the lowest step at the head —
and what the fixed `euler(30, 45, 0)` camera sees is the closed back of a
staircase.

Measured on the built `ship` world, seed `20250824`, which climbs on 10
staircase tiles in 2 runs — a run of 9 and a run of 1:

```
a-baseline:            of 10 staircase tiles, 0 crest at the head of the climb, 10 crest at its foot, 0 crest level
b-crest-at-the-head:   of 10 staircase tiles, 10 crest at the head of the climb, 0 crest at its foot, 0 crest level
```

The sweep's own comparison sheets are kept beside this decision, in
`docs/prototypes/t-31/`: `sheet-three-way-play.png` (a, b and c at gameplay
zoom), `sheet-play-detail.png` (all eight candidates, cropped on the long run)
and `sheet-level.png` (all eight at the whole-level framing). The remedy as
actually implemented, rather than as prototyped, is
`docs/prototypes/t-31/after-b-as-implemented.png`.

That measurement collapses #68's two findings into one. **The masonry filling
the drop and the row of buttresses are the same defect** — the back of a
backwards staircase, seen once at a climb and eight times along a level run.

It also kills the reading that turning a flight makes an ascent read as a
descent. A flight climbing north has its risers facing south, which is the side
a 45 degree yaw looks from; turning the mesh so its treads face the camera is
not a second remedy competing with correcting it, it *is* correcting it.

Two candidates were killed by measurement rather than by taste:

- **Change the camera pitch.** The 30 degree pitch is what T-19 settled for
  portrait, and [ADR-0002](0002-difficulty-scales-with-the-level-number.md)
  then froze the map size around it — preset size is explicitly not a knob that
  moves with the level number *because* the framing was settled for a level of
  `ship`'s size. Turning the pitch to expose treads reopens both, and it was
  never needed: at the shipped pitch a correctly laid flight already reads
  10 of 10.
- **Vary the climb direction in `MazeCarver`.** A domain change. It moves every
  generated number, so
  `OpeningPlanFreezeTests.TheOpeningPlanMintsTheLevelsItAlwaysMinted` fails by
  construction — the level-1 freeze six difficulty tickets preserved. #94 filed
  it as rejected unless measurement said otherwise, and measurement said the
  opposite: the geometry alone accounts for all 10 of 10.

## Decision

**A staircase is laid against its own climb.** `LevelBlueprintBuilder`'s
staircase part takes the yaw of `TileSides.Opposite(ascent)` rather than of the
ascent, so the pack's descending flight descends the way the tile's own climb
falls.

Nothing else moves. `ModelPose.PositionOf` already places the mesh's origin half
a run behind the mesh's own forward, so reversing the yaw relocates the origin
from the foot edge to the head edge on its own: the flight still fills exactly
the one step its tile hovers over, still covers exactly one tile edge by one
tile edge, still tops out flush with its own floor, and the bounding box is
identical to the byte. The floor tile under a staircase is untouched, keeps its
collider and stays ordinary walkable ground; the staircase geometry still
carries no collider; floor-state painting reads the consumed set and never
looked at a staircase.

The reason lives in the code rather than in a comment. `StaircaseFlight` pins
the pack flight's crest over ten slices of its own run, so that a reader finds
`5.1` at the mesh's origin and `1.1` at its far end where the laying is decided,
and it names the two ends of a laid flight — `CrestOf` is the pose's origin,
`SinkOf` is one run along the mesh's own forward. The claim is split
deliberately: the fast loop asserts the **pose** against that pin, and
`StaircaseCheckCommand` ties the pin to the **asset**, measuring the real mesh's
ten slices against it and then measuring every built flight's mass off its
vertices. Neither half alone would catch a swapped mesh; together they do, and
the check is where the vertices are.

### What is given up: T-23's orientation criterion

**The criterion abandoned is #72's third acceptance criterion** — *"the
staircase mesh is oriented along the direction of the climb, including where the
staircase bends into an L"* — together with the three assertions that carried
it: `StaircaseCheckCommand`'s *every staircase faces the way its own climb goes,
the L bend included* (10 of 10), `StaircaseTests.TheStaircaseFacesTheWayTheClimbGoes`
and `ModelPoseTests.AStaircaseKeepsTheYawItsClimbAsksFor`. #68's own mesh mapping
carries the same claim — *the blueprint gives such a tile a staircase mesh
oriented along the climb* — and it is amended with them.

It is given up because **it asserted the transform while the geometry was
backwards**. Pointing the mesh's local +Z along the ascent is precisely what put
every riser at the wrong end of the tile, so the criterion passed 10 of 10 in
the exact world where not one step was visible. An orientation criterion a
backwards mesh satisfies is not a criterion about orientation; it is a criterion
about a number in a transform, and it was measurably compatible with the bug it
was supposed to exclude.

Its replacement asserts the **mass, not the transform**: a flight's mass crests
at the head of its own climb and sinks at its foot, read off the vertices in the
built world — `EveryFlightCrestsAtTheHeadOfItsOwnClimb`, reporting `10 of 10
crest at the head, 0 at the foot, 0 level` where the shipped pose reported
`0 of 10`. The same claim is stated in the fast loop against the pinned profile,
by `StaircaseTests.EveryFlightCrestsAtTheHeadOfItsOwnClimbAndSinksAtItsFoot` and
`ModelPoseTests.AStaircaseSetsItsCrestDownOnTheEdgeTheClimbEndsAt`. T-23's
foot-edge assertion — *every staircase starts on the tile edge its climb starts
at* — is replaced rather than deleted for the same reason: it named the correct
edge for a mesh laid the wrong way round.

## Considered options

**`c` — a stretched foundation under the level walkway.** A corrected flight
only under a tile that stands one step above a lower neighbour, and a stretched
`floor_foundation_allsides` under the level run between them. Measured *2 crest
at the head, 0 at the foot, 8 crest level*, which is the intended reading for a
level run. **Deferred, not rejected**, and only the serration of a level run's
underside is deferred with it — filed as #101 with what it costs: the first two
acceptance criteria of #72, since 8 of 10 staircase tiles would carry a
foundation rather than a staircase, and T-23's no-stretching assertion, since
`floor_foundation_allsides` measures `0.55 x 0.5 x 0.55` against a tile edge of
`1.0` and would need roughly 1.8x across the ground.

**`d` — corrected flights at the feet only, foundation elsewhere.** The same
2/0/8 reading as `c` and the same two costs, without `c`'s flight under the
upper terrace's own near edge. Nothing to prefer it for.

**`e` — a plinth under every staircase tile, no flight anywhere.** Measured
*0 crest at the head, 0 at the foot, 10 crest level*. Rejected because it
abandons user story 5 of #68 outright: a level with no flight anywhere does not
look like steps, it looks like a ledge. It is the one candidate that answers the
ticket by deleting its subject.

**`f` — every flight turned a quarter, treads across the run.** Measured
*0 / 0 / 10*: turning the flight across its own tile puts crest and sink at the
same reach along the climb, so no tile crests either way. It also reads as a
flight going somewhere the walk does not.

**`g` — the pack's walled flight, `stairs_walled`.** Its crest reads
`1 1 1 0.75 0.75 0.535 0.675 1 0.754 1`: the side walls stand full height at
both ends, so it crests level whichever way it is laid — measured *0 / 0 / 10*.
The mesh that looks most like a staircase in isolation is the one that cannot
express which way it climbs.

**`h` — the pack's wide flight, `stairs_wide`.** Measured *10 / 0 / 0*, the same
as `b`. Rejected on fit: it measures `1.7501` across against a tile edge of
`1.0`, so seating it needs a non-uniform squash, and
`ModelPoseTests.AStaircaseNeedsNoStretchingAcrossItsTileBecauseThePackCutItToTheGrid`
asserts `scale.X == 1` and `scale.Z == 1` exactly because the pack cut
`stairs_narrow` to the grid. `b` gets the same reading for no scale at all.

**Keep T-23's yaw assertion and add the crest assertion beside it.** The
smallest diff that records the new property. Rejected because the two are
contradictory, not complementary: satisfying the crest assertion forces the yaw
of the opposite side, so keeping both leaves a check that can never pass. An
assertion that a change makes false is replaced, not retained beside its
replacement.

## Consequences

`#68`'s mesh-mapping decision is amended: a staircase tile's mesh is oriented
**against** the climb, and the orientation that matters is a property of the
pack's mesh rather than of the tile. Any future staircase mesh has to be
measured the way this one was — a mesh swapped in on the assumption that a
flight rises along its own forward will read backwards and pass every transform
assertion while doing it.

`StaircaseFlight` is where that measurement lives, and it is deliberately in
`Game.Presentation.Pure` rather than in the Editor check: the fast loop can then
state the readability claim, and the Editor check's job narrows to proving the
pin still describes the asset. The pin is ten numbers; a mesh replacement that
changes any of them fails a command instead of surfacing in a screenshot.

Nothing in the domain moves. The level-1 freeze fingerprint over 200 seeds is
`7573283008921576682` before and after — a presentation yaw cannot reach
generation, and the fingerprint is the proof rather than the claim.

The four candidate meshes the sweep needed — `stairs`, `stairs_wide`,
`stairs_walled`, `floor_foundation_allsides` — leave the resources tree, because
anything under `Assets/Resources/` ships whether it is referenced or not, and the
prototype command that loaded them leaves with them. The sweep is therefore no
longer runnable from a working tree; its numbers are the ones quoted above, its
pictures are the three sheets, and its harness is recoverable from the branch's
own history rather than from `main`. Measured, `Game.EditorTooling.AndroidBuildCommand.Build`
before and after: **34,259,183 bytes to 34,257,935 bytes, a delta of -1,248** —
`b` adds no asset and no runtime branch, so the remedy costs nothing in the APK,
and the sign of the residual says it is build noise rather than a saving.

The second defect #68 recorded is not fixed here and is not the same defect any
more: it is now *a level run of nine correctly laid one-unit flights reads as
nine climbs rather than one*, which is a question about how many flights a run
should carry, not about which way one faces. That is #101.
