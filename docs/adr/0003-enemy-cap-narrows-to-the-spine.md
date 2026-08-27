# `EnemyCap` applies only to the Spine, and the Adversary panel alone forbids stranding

## Status

accepted

## Context

`ContentPlacer` mints a content node's number **during** an adversary's own
walk, and for an enemy it clamps what it mints:

```
minted = Math.Min(minted, Math.Max(1, (int)(power * tuning.EnemyCap)))
```

`EnemyCap` is **0.6** in `Tiny`, `Ship` and `Stress` alike, and the walk it
clamps against is `NextOnTheWorstWalk` — multiplier, then additive, then the
cheapest affordable enemy. Every enemy in the level is therefore minted at no
more than 60% of the power a deliberately poor route holds when it arrives. That
was the point: an enemy the walk could not afford is never even representable,
so a level that cannot stall is produced directly instead of being produced and
then filtered.

It is also, measured, the reason the game has no doors. Over 5000 `ship` seeds
`AdversaryStalled`, `UnaffordableEnemy`, `RegionFloorUnmet` and
`EnvelopeInverted` all fire **zero** times. The Adversary panel, Floor Repair and
the Power Envelope are dead weight today, not because they are wrong but because
the cap makes it impossible for them to fail. The `P_max / P_min` spread has
p10 = 1 across 20 000 Regions. The player who reached level 20 without thinking
named the mechanism exactly: *"I never met an enemy I could lose to."*

Backlog decision #14 records stranding as "impossible by construction —
generator guarantees it", and the cap is what a reader would reasonably assume
that construction is.

## Decision

`EnemyCap` narrows to the **Spine**: the same poor-adversary minting walk,
truncated the moment the boss becomes affordable. Enemies the Spine consumes are
capped exactly as they are today. Content the Spine never reaches is minted
afterwards against the rich wallet — the `P_max` of its own Region — and an
enemy minted that way is an **Elite**, out of reach on arrival and beatable only
after the player has gone elsewhere and come back. Walking into one costs
nothing; it simply does not open.

The cap and the proof of Invariant A then protect the same walk and nothing
more. Along the Spine there is still one route that finishes the level however
badly it is played. Off the Spine, the level has doors.

**Backlog decision #14 is narrowed, not reversed.** Stranding stays impossible.
What moves is where the guarantee comes from. It was never `EnemyCap` that
proved Invariant A — `AdversaryPanel.FirstStall`, called from
`SolvabilityValidator`, is the guarantee, together with the exhaustive oracle on
`tiny`, and that machinery already carries its own measured error bars: over 6000
`tiny` mutants the six-policy panel false-alarms **0** times and misses **7** of
the 3875 stalls the oracle finds, 0.18%, with the suite failing the build on a
single false alarm or on a miss rate above 0.4%. Relaxing the cap is safe by
validation. The cost is a rejection rate, and a rejection rate is a number the
sweep already prints.

The fraction of off-Spine content minted rich is an **authored** knob on the
level's plan — near zero at level 1, approaching all of it at the plateau. It is
deliberately not emergent: an emergent fraction would decide for itself how many
closed doors the frozen level-1 tutorial has.

`Elite` is a **reading**, never stored state and never a slot type: an enemy's
value taken against its Region's `P_min`, computed whenever it is asked for,
following the discipline `Gate Enemy` already sets. The Recipe still counts one
boss, multipliers, enemies and additives.

## Considered options

**Raise `EnemyCap` toward 1.0.** One constant, no new walk, and every existing
measurement still applies. Rejected because it makes enemies expensive without
making any of them a door: at 0.95 the cheapest affordable enemy on the frontier
is still affordable on arrival, every tap order still finishes the level, and
whatever spread appears still comes from ordering rather than from access. An
expensive-but-always-affordable enemy is the present bug with a larger number.
It degrades ungracefully too — the closer the cap sits to 1.0, the more of the
game's difficulty rests on where `(int)(power * cap)` rounds.

**Delete the cap and rely on the panel alone.** Validation would still be sound,
since the panel is the guarantee either way. Rejected because nothing would then
bias minting toward a level that is acceptable: the generator would reject seeds
until one happened to be playable, inside a 50-attempt cap. There is room in the
budget for that — 0.95 ms per level at 12% rejection leaves headroom to reject
roughly 90% of seeds — but that headroom is a ceiling rather than a licence, and
spending it buys nothing that capping the Spine does not already give.

**Add a lock role to the Recipe.** Place "locked enemies" explicitly, as a
fourth kind of content beside enemies, multipliers and additives. Rejected: it
makes lockedness a fact stored on a node, which then has to be kept true while
Floor Repair moves numbers underneath it, and it creates a second source of
truth about what blocks a route beside the one the graph already answers. A
locked enemy is a relationship between a value and a Region's `P_min`, and a
reading is the honest way to represent a relationship.

**Let the Elite fraction emerge from the relaxed cap.** Mint everything off the
Spine rich and accept whatever fraction turns out to be unaffordable. Rejected
for the same reason as the authored knob above: level 1 is frozen on purpose,
and a tutorial's number of closed doors is not something to discover.

## Consequences

`AdversaryStalled` and `UnaffordableEnemy`, both zero today, will start firing.
That is the validation machinery becoming load-bearing rather than a regression,
and it is the first time the 50-attempt retry cap has had anything real to
survive.

Every minted value in every level changes, which is what makes this hard to
reverse. It is also why ordering in the work that follows is load-bearing rather
than convenient: the per-Region spread floor cannot be calibrated before the cap
is cut, because today's spread distribution is a product of the cap.

Invariant A is unchanged and still binding. Invariant B is unchanged in form but
not in value: the boss is still `round(0.8 * (P₀ * M + A))`, and Elites raise
`A`, so the boss rises with them — which is the intended direction, since the
level got richer.

The floor rule keeps an Elite-heavy Region playable, and needs no new rule to do
it. Floor Repair still pulls each Region's cheapest enemy below that Region's
`P_min`, so `min(enemy power in R) <= P_min(R)` still holds and every Region
still has something edible in it. A Region can be mostly locked; it cannot be
sealed. Note that the minting walk carries a second clamp beside the cap — the
first enemy minted in a Region is held below the power the walk arrived in that
Region with — and it narrows to the Spine automatically, since it only ever
applied to enemies the walk itself minted. Floor Repair, not that clamp, is what
keeps the floor rule true in a Region the Spine barely enters.

Minting stops being a single pass. The walk no longer visits every content node,
so the off-Spine pass becomes the thing that makes `ValueNeverMinted`
impossible, and the check that every content node was minted is a check on that
pass rather than on the walk.

The Spine is a reading of a finished level, so it can be asserted on directly:
on an accepted level every enemy on the Spine is affordable when the Spine
reaches it, and above the level-1 plan some off-Spine enemy is not affordable on
arrival — otherwise no lock was created and the change did nothing.

A future reader arriving at backlog decision #14 will find it still true and its
proof somewhere else. That relocation, rather than the cap's value, is what this
decision exists to record.
