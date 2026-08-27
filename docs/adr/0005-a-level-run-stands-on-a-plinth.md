# A level run stands on a plinth, because a flight belongs only where the ground actually drops

## Status

accepted

## Context

[ADR-0004](0004-a-staircase-is-laid-against-its-own-climb.md) fixed which way a
flight faces and said so: the masonry filling the drop and the row of buttresses
were the same defect, the back of a backwards staircase, seen once at a climb and
eight times along a level run. It fixed the seeing-it-once case and left the
seeing-it-eight-times case open as #101, restated as *a level run of nine
correctly laid one-unit flights reads as nine climbs rather than one*.

The shape of the ship level, seed `20250824`, is why. It climbs on 10 staircase
tiles in 2 runs — a run of 9 and a run of 1 — but only **4 tiles in the whole
level stand one step above a lower neighbour**: two of the ten climbing tiles,
and two tiles of an upper terrace at the head of a climb. The other 8 climbing
tiles are a mid-level walkway, level with every neighbour they have. Each still
carried its own one-unit flight, so eight flights hung under ground that never
drops.

The serration was measured before anything was changed, off the built world's
own triangles rather than off its vertices. For each tile the check slices the
tile's footprint 24 ways along each ground axis, intersects every triangle of the
mesh under that tile with each slice plane, takes the highest point of the
intersection, and reads the shortfall as the fraction of the one-step drop the
mass fails to reach at the emptiest slice, worst axis winning. A plinth filling
the drop reads `0`. A flight reads whatever its own descent leaves empty at its
foot. Measured on the built ship world:

```
before (b as shipped): of 8 level tiles, 8 leave a notch deeper than 0.05 of a step, the deepest 0.935
after  (c as here):    of 8 level tiles, 0 leave a notch deeper than 0.05 of a step, the deepest 0.000
```

The same two framings T-31 compared its candidates in are kept beside this
decision, in `docs/prototypes/t-36/`:
`before-b-as-shipped-play.png` and `after-c-as-implemented-play.png` at gameplay
distance, `before-b-as-shipped-detail.png` and
`after-c-as-implemented-detail.png` cropped on the long run. The before pair is
the row of triangular buttresses #68 recorded, photographed from the shipped
pose; the after pair is one unbroken plinth with a flight at each of its ends.

The candidate this implements is T-31's `c-crest-at-the-head-both-ends`, which
that sweep measured at *2 crest at the head, 0 at the foot, 8 crest level* over
the ten climbing tiles. The built world here reports the same reading from the
other side: **4 of 4 flights crest at the head of their own climb, 0 at the foot,
0 level**, and 8 of 8 plinths fill their tile's whole drop. The two counts agree —
the sweep counted only climbing tiles and saw 2 flights, the check counts every
footed tile and sees those 2 plus the 2 terrace-edge flights that top the climbs
out.

`floor_foundation_allsides` imports at `0.55002 x 0.50002 x 0.55002` against a
tile edge of `1.0` and a step of `1.0`, so filling a tile's drop with it needs
`1.818x` across the ground and `2x` in height. Stretched by exactly those
amounts it measures `1.00004 x 1.00005 x 1.00004`, which is one tile edge by one
tile edge by one step to within the bounds epsilon: it is inflated on purpose and
still spills nothing.

## Decision

**A level run stands on a plinth, and a flight belongs only where the ground
actually drops.** What a tile carries under its floor is a footing, and which
footing is a pure decision of the domain's own tile grid, in
`TileFootings.Under`:

- a tile whose floor stands one step above a lower neighbour's is footed with a
  **flight** — the corrected `stairs_narrow` of ADR-0004, laid against the climb
  that leads away from the ground it stands above;
- a climbing tile level with every neighbour is footed with a **plinth** — the
  pack's `floor_foundation_allsides`, stretched to fill the drop flush;
- a tile level with or below every neighbour is footed with **nothing**.

The rule reads elevations off `TileGrid`, never off built geometry, so the whole
of it is stated and tested in the fast loop, and `LevelBlueprintBuilder` does
nothing but ask it. `TileFootings.AscentOf` keeps `StaircaseClimb.AscentOf` for a
climbing tile and falls back to *away from the ground it stands above* for a
terrace tile, which the fast loop asserts agree wherever both apply.

The Unity side stays a lookup: one asset name in `WorldModels`, one colour in
`WorldPalette`, one pinned pack measurement in `DungeonPack`, one arm each in
`ModelPose`'s three switches. `ArtPacks` is untouched — the foundation is a
Dungeon-pack mesh and the pack already answers for anything that is not rigged.
Nothing in the domain moves.

The **flight at the head** is the half of candidate `c` that separates it from
`d`, and it is the half that makes the reading work. Without it a run ends in a
bare one-step riser up onto the terrace, so the climb reads as *ramp, then
ledge*. With it the mass is continuous from the lower terrace's floor to the
upper terrace's floor: flight up, level plinth, flight up. That is the one climb
the ticket asks for, and it is why a **terrace** tile can now carry a staircase.

### What is given up: #72's first two criteria, and T-23's blanket no-stretching claim

**The criteria abandoned are the first two of #72** — *"a staircase tile —
identified by asking the domain, not by comparing neighbour elevations — carries
the staircase part model"* and *"a terrace tile does not carry the staircase part
model."* Both are now false: 8 of the ship's 10 staircase tiles carry a
foundation, and 2 terrace tiles carry a staircase.

They are given up because **they asserted the tile's class where the geometry
depends on the tile's edges.** Whether a tile needs a flight is not a fact about
what kind of tile it is; it is a fact about whether its floor stands above
anything. The old criteria were satisfiable — and were satisfied — by a world in
which eight flights hung under ground that never dropped, which is the defect
they were meant to exclude. The instinct they protected is still right and is
kept: the decision must come from the domain rather than from measured geometry.
It just needs neighbours' elevations, which are domain facts too.

Their replacements assert the **footing, not the tile class**, and are stated
twice — in the fast loop against the blueprint, and in `StaircaseCheckCommand`
against the built meshes:

| given up | replaced by |
| --- | --- |
| `StaircaseTests.EveryStaircaseTileCarriesTheStaircaseModel` | `EveryTileAStepAboveALowerNeighbourCarriesTheStaircaseModel` and `EveryClimbingTileLevelWithItsNeighboursCarriesTheFoundationModel` |
| `StaircaseTests.ATerraceTileCarriesNoStaircaseModel` | `ATileLevelWithOrBelowEveryNeighbourCarriesNoFlightUnderneath` and `ATerraceTileAtTheHeadOfAClimbCarriesTheFlightThatTopsTheClimbOut` |
| `StaircaseCheckCommand`'s *every staircase tile carries the pack's staircase mesh* | *every tile a step above a lower neighbour carries the pack's staircase mesh, and every climbing tile level with all of them carries the pack's foundation mesh* — `4 of 4 flights and 8 of 8 plinths do` |
| `StaircaseCheckCommand`'s *a terrace tile carries no staircase, so the world raises exactly one per climb* | *no tile wears a footing its own grid did not ask for, so the world raises exactly one per footed tile and none anywhere else* — `12 footings for 12 footed tiles` |

**T-23's no-stretching assertion is narrowed rather than abandoned.**
`ModelPoseTests.AStaircaseNeedsNoStretchingAcrossItsTileBecauseThePackCutItToTheGrid`
is still true and still stands: `stairs_narrow` was cut to the grid and its pose
still scales `1` across the ground. What is abandoned is the *blanket* reading of
it — that no dungeon mesh is ever stretched — because a plinth is. T-30 (#100)
made fit-to-cube a real constraint on prop choice, and the constraint it was
protecting is that **nothing spills off its tile**, not that nothing is scaled.
So the claim added beside it is the one that actually protects the cube:
`ModelPoseTests.AFoundationIsStretchedToItsTileOnPurposeAndStillSpillsNothingOffIt`
pins that the foundation is smaller than its tile on every axis, that every
stretch factor is greater than one, and that each stretched span lands on exactly
one tile edge or one step; `StaircaseCheckCommand` ties the same claim to the
asset — *the foundation is stretched to its tile on purpose, and the stretch
lands it on exactly one tile edge by one tile edge by one step, so it spills
nothing* — and `EveryPlinthFillsItsDropAndSpillsNothing` measures the built
bounds of all 8.

## Considered options

**`d` — corrected flights at the feet only, foundation elsewhere.** The same
`2/0/8` reading over climbing tiles as `c`, without the flight under the upper
terrace's own near edge. Rejected on the reading rather than on a number: the
climb then ends in a bare one-step riser, so the run reads as a ramp that stops
short of the floor it is climbing to. `c` costs one extra flight per climb and
finishes the sentence.

**`e` — a plinth under every staircase tile, no flight anywhere.** Measured
`0/0/10` by T-31 and rejected there for abandoning user story 5 of #68 outright.
It stays rejected here for the same reason, and this ticket sharpens it: with the
serration measurement in hand, `e` would score a perfect `0 of 8` and still be
wrong, which is exactly why the serration assertion is not the only one — the
crest assertion of ADR-0004 still has to hold, `4 of 4`, in the same run.

**Leave the drop under a level run empty — no plinth, no asset.** Scores `0 of 8`
on serration for free, adds nothing to the APK, and needs no mesh recovered.
Rejected because the notch measurement is not the point on its own: a level run
with nothing under it leaves the flight at its foot hanging off open air, and the
walkway floats a step above the terrace with a visible gap where it should meet
it. The measurement would improve and the picture would get worse, which is what
the screenshot criterion is in the ticket to catch.

**A scaled cube primitive, or a scaled `floor_tile_large`, instead of the
foundation.** The cube costs no asset at all, and `floor_tile_large` is already
in the tree. Rejected on the dress: a primitive cube takes a flat tint and reads
as an untextured box against KayKit stone, which is the fallback
`DungeonDressCheckCommand`'s *nothing falls back to its primitive* exists to
forbid; and `floor_tile_large` stands `0.15` pack units, so filling a step with
it needs `6.7x` in height against the foundation's `2x` — more distortion, not
less, for a mesh that is a floor rather than a footing.

**Staggered elevations, so a run actually rises.** A domain change. #72 forbids
it, and it moves every generated number, so
`OpeningPlanFreezeTests.TheOpeningPlanMintsTheLevelsItAlwaysMinted` fails by
construction. #101 filed it as rejected unless measurement said otherwise, and
measurement said the opposite: the presentation alone accounts for the whole of
the notch, `0.935` of a step to `0.000`, without a single generated number
moving.

**Keep the per-tile flight and hide the notch — darken the material, or turn the
camera.** Rejected for the reason ADR-0004 rejected the pitch change: the 30
degree pitch is what T-19 settled for portrait and
[ADR-0002](0002-difficulty-scales-with-the-level-number.md) froze the map size
around it. And a notch that is `0.935` of a step deep is not a shading problem;
it is 93.5% of the drop being empty.

## Consequences

`#68`'s mesh-mapping decision is amended a second time: a tile's mesh under its
floor is chosen by **what its own edges do**, not by which class of tile it is. A
staircase tile is no longer synonymous with a staircase mesh in either
direction — most of them carry a plinth, and some terrace tiles carry a flight.
Anything reading the world by tile class will be wrong about the footing; ask
`TileFootings.Under`.

`TileFootings` is deliberately in `Game.Presentation.Pure` beside
`StaircaseClimb`, so the fast loop states the rule in full and the Editor check's
job stays what ADR-0004 narrowed it to: proving the built meshes still match what
the pure decision asked for. The check gained the measurement rather than the
rule — `NoLevelRunIsSerratedUnderneath` computes a number off triangles and
compares it to a bound, and it was broken on purpose (the plinth shortened to
half its drop) and confirmed to fail before being restored.

`PartStyle` gained `Foundation`, which raises the world material ceiling from 13
to 14; the built ship level uses 10 distinct materials for 434 renderers, one
more of each than before.

Nothing in the domain moves. The level-1 freeze fingerprint over 200 seeds is
`7573283008921576682` before and after — a presentation footing cannot reach
generation, and the fingerprint is the proof rather than the claim.

The foundation mesh returns to `Assets/Resources/Dungeon/`, recovered from the
commit that removed it with its LFS pointer and import meta intact, so the
postprocessor settles it exactly as it settles the other five. Measured,
`Game.EditorTooling.AndroidBuildCommand.Build` at this branch's base and at its
tip: **35,826,109 bytes to 35,838,275 bytes, a delta of +12,166** — `28.5 KiB`
of FBX costs `11.9 KiB` in the APK once mesh compression and the archive have had
it, and the remedy adds no runtime branch beyond one more arm in three switches.

The plinth stops at the climb on purpose. A terrace has *nothing underneath* by
design — `CONTEXT.md` says so in as many words, and the whole illusion of having
climbed a floor rests on terraces reading as separate planes rather than as one
solid block. So the footing rule gives nothing to a tile that is level with or
below all of its neighbours, however large a run of them is, and the mass this
decision adds is confined to the drops the walk actually crosses. If a terrace's
own edge ever needs to read as solid, that is a question about how much of the
dungeon is a building, and it is not this one.
