# Difficulty scales with the level number, and a level graph is a pure function of its seed and its plan

## Status

accepted

## Context

`GameBoot` hands `LevelSupply` a fixed `MazePreset.Ship` and nothing else, so
the level number reaches generation only as part of the seed. Level 20 is level
1 with different geometry: the same preset, the same `ContentRecipe.Ship`, the
same `PowerTuning.Ship`, the same starting Power of 2. A player reached level 20
by tapping without thinking, and was right to — four shipped decisions compose
into a guarantee that playing badly works. Power is monotonically non-decreasing
(backlog decision #8) and losing a fight has no penalty (#7), so nothing a
player does can take anything away. `EnemyCap` mints every enemy at no more than
60% of the power the minting walk holds on arrival, so every enemy on the
frontier is affordable by construction. Invariant A forbids a stall in every
reachable state. Every tap order therefore finishes the level.

The existing fuzz sweep measures how little a route is worth today. Over 5000
`ship` seeds the `P_max / P_min` spread across 20 000 Regions has **p10 = 1** —
at least a tenth of the game's Regions have literally zero routing value, and on
the `tiny` preset the median is 1. Every power-axis rejection reason fires
**zero** times: `AdversaryStalled`, `UnaffordableEnemy`, `RegionFloorUnmet` and
`EnvelopeInverted` are all 0, leaving `TooFewOffPathSlots` (561),
`BossWithinReach` (119) and `BossTooShallow` (2). Generation costs **0.95 ms**
per level at a **12%** rejection rate, so acceptance strictness is not
budget-constrained in this project: there is room to reject 90% of seeds and
still generate a level in ~10 ms.

Whatever makes level 12 harder than level 1 has to be a function of the level
number, and the level number has to reach the thing that mints numbers. Today it
reaches nothing but the seed.

## Decision

A **plan** maps a level number to the `(preset, recipe, tuning)` triple that
level is generated with, and it is the only thing in the game that treats a
level number as a difficulty. It lives in `Game.Domain`. `LevelSupply` consults
it on each `Draw()`, keyed on its own draw counter — never on
`GameCycle.LevelNumber`, which lives in `Game.Presentation.Pure` and which
`Game.Domain` may not reference.

The level graph's purity contract changes with it. It was "a pure function of
its seed and preset"; it becomes **a pure function of its seed and its plan**.
`CONTEXT.md`'s *Preset*, *Level Graph* and *Recipe* entries are amended, because
all three either carried the old claim or leaned on it: a preset is now a size
and nothing else, and a recipe belongs to a plan rather than to a preset.

The generator needs nothing new to accept this. `LevelGenerator` already exposes
a `Generate(seed, preset, recipe, tuning, out report)` overload; a plan simply
bypasses the `ContentRecipe.For(preset)` and `PowerTuning.For(preset)` lookups
that the shorter overload performs.

**Level 1's plan is today's `ContentRecipe.Ship` and `PowerTuning.Ship`, frozen
verbatim** — 2 multipliers, 14 enemies, 7 additives, starting Power 2, `EnemyCap`
0.6. Roughly fifteen expensive fuzz tests keep pointing at it, so they remain a
fixed reference point while the curve above them is calibrated. The curve rises
to a **plateau at level 12–15** and is constant above it, because nothing in this
project persists across app launches — there is no `PlayerPrefs` anywhere in it
— and a difficulty peak the player never reaches in one sitting is a peak that
does not exist.

The knobs that move with the level number are starting Power, the Recipe mix,
the Elite fraction, the per-Region minimum spread, star thresholds and
`MinimumOffPathSlots`. Preset size does not move, and neither does
`BraidFactor`: the portrait framing settled in
[ADR-0001](0001-terraces-instead-of-stacked-floors.md) was settled for a level of
`ship`'s size, and `BraidFactor` is the one knob deciding how much of a level is
gate and how much is optional pocket, so harvesting dead ends with it would
reintroduce the corridor this change exists to remove.

## Considered options

**Keep the curve in `Game.Flow`.** The level number already lives there, so a
table mapping it to a recipe and a tuning could sit beside `GameStateMachine`
and hand the triple down to `LevelSupply`. Rejected because it puts the most
calibration-hungry code in the project outside the fast loop. `dotnet/` globs
`Game.Domain` and `Game.Presentation.Pure` only; every constant on the curve is
meant to be derived from a measured sweep, and that sweep is a domain test that
runs in ~30 s. In `Game.Flow` the same calibration needs the Editor for every
iteration. It also inverts the dependency the architecture is careful about:
presentation would decide what a level number means, and the domain could no
longer generate level 12 on its own — which is exactly what the sweep has to do
thousands of times.

**Let difficulty emerge from the seed.** No plan at all: accept only seeds whose
generated content is hard enough for the level number. Rejected because nothing
is then reproducible from a level number, level 1 cannot be frozen, and there is
no monotone quantity to sweep or calibrate. Emergence is also what produced the
present distribution: p10 = 1 is what the generator picks when nobody authored
otherwise.

**Put the curve in the presets.** Add `ship12`, `ship15` and so on. Rejected:
`ContentPlacer` rejects with `RecipeSlotMismatch` unless
`recipe.Slots == preset.ContentSlots`, and the slot total is deliberately frozen
at 24, so a dozen presets of identical size would differ only in the fields the
plan exists to carry — while multiplying the sweep's per-preset corpora and
splitting a glossary term that means "a level size" across two jobs.

**Grow the map with the level number.** The obvious reading of "harder", and the
one the problem was first described as. Rejected: the player asked for
meaningful choices rather than a bigger map, the framing was settled for a level
of this size, and a larger maze with a spread of 1 is a longer autopilot.

## Consequences

Determinism is unchanged in strength and restated in shape: a level is
reproducible from a seed and a level number, which is what any bug report needs
to carry. What is gone is the weaker claim that a seed and a preset are enough.
Two levels of the same seed and the same preset can now differ in every minted
number, so anything that caches, compares or names a level by `(seed, preset)`
is wrong.

`PlacedLevel` grows a `Plan` beside its `Recipe`, `Tuning`, `Envelope` and
`Verdict`, which makes `LevelSupply.Draw()` returning a `PlacedLevel` the single
test seam for most of the work that follows. No new seam is introduced.

The fuzz sweep sweeps plans where it sweeps presets today, and reports its
existing measurements — rejection rate by reason, spread percentiles, mean
generation time — per plan. This matters more than it sounds: the risk in
scaling several knobs at once is that they compose into something unsatisfiable
for a later plan, and a rejection-rate bar per plan is the tripwire that catches
it in ~30 s rather than in play.

Backlog decision #18 (two multipliers per `ship` level) is partly reopened. It
was justified by "at 24 content nodes a third collapses every value to 1", which
is a consequence of a starting Power of 2 rather than of the slot count: minting
sets a node's value from the *increment* of a geometric curve, and at Power 2–5
that increment floors to 1. A third multiplier may enter the Recipe only at the
plans whose raised starting Power makes it viable. Level 1 keeps two.

Nothing about the frozen baseline moves, so this decision on its own changes no
generated level. Levels only start to differ once the plans above level 1 are
authored — which is why the contract change is recorded now, before any of them
exist, rather than read later as an accident.
