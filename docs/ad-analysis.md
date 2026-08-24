# Ad Deconstruction — "Hero Wars: Alliance RPG Legend"

Reference document for reproducing the advertised game. Everything in
**Verified** sections was read directly off the video frames. Everything in
**Ambiguous** / **Not shown** sections is missing from the source and must be
decided by us before implementation.

---

## 1. Source

| | |
|---|---|
| File | `WhatsApp_Video_2026-08-23_at_16_54_23.mp4` |
| Duration | 56.6 s |
| Resolution | 576 × 1296 (vertical 9:20), ~89 fps |
| Container | YouTube in-feed sponsored ad (device screen recording) |
| Advertised app | Hero Wars: Alliance RPG Legend |
| Ad copy | "Winning comes from the choices you make, not ho[w…]" (truncated in UI) |
| Audio | 1 mono AAC track — **not analysed** |

The playable area is the top ~70 % of the frame. The bottom third is YouTube
chrome (Install button, ad card, nav bar) and is not part of the game.

---

## 2. Shot list

| Time | Segment | Purpose |
|---|---|---|
| 00:00 – 00:18 | **Act 1 — The Pillars** | Narrative hook. Establishes the number→height rule and the "don't give your power away" premise. |
| 00:18 – 00:19 | Transition | Player falls through a green portal on the ground. |
| 00:19 – 00:21 | Level fly-through | Camera pans the whole dungeon before play starts. |
| 00:21 – 00:48 | **Act 2 — The Dungeon** | The actual gameplay loop. This is the part worth cloning. |
| 00:48 – 00:53 | Branding | "PLAY NOW!" + real-game screenshots (unrelated to the ad gameplay). |
| 00:53 – 00:57 | Closing teaser | Re-states the number→height rule with new numbers. |

---

## 3. Act 1 — The Pillars (00:00–00:18)

### Verified

Three characters, each with a floating number badge, each standing on top of a
cylindrical wooden pillar. **Pillar height is proportional to the number.**

| Character | Number | Badge colour | Pillar |
|---|---|---|---|
| Player (shirtless peasant) | 5 | Blue | Ground level (no pillar) |
| Girl (green cloak) | 25 | Red | Medium |
| Rival (armoured champion) | 99 | Red | Tall |

Sequence:

1. `00:00–00:08` — Camera pulls back to establish the three pillars. No input.
2. `00:08` — Player produces a small pink heart and throws it upward at the girl.
3. `00:09` — Player's number drops **5 → 4 → 2**. His sprite desaturates and
   becomes a **skeleton** (power drained).
4. `00:10` — Girl's number counts up **25 → 34 → 46 → 50**.
5. `00:10.5` — At 50 she plays a golden level-up burst and **changes outfit**
   (peasant → queen). Her pillar **grows** until it is level with the rival's.
6. `00:11–00:17` — She walks across to the rival; they exchange large hearts.
   The player, still at 2, is stranded at the bottom.
7. `00:17–00:18` — He falls through a green portal in the ground → Act 2.

### Rules extracted

- **R1.1** `number → pillar height` (vertical position is the win condition).
- **R1.2** `number → character visual tier` (peasant→queen; player→skeleton).
- **R1.3** Power can be **transferred** to another actor, and doing so is the losing play.
- **R1.4** Crossing a threshold triggers a level-up VFX + sprite swap.

### Ambiguous

- **Numbers do not conserve.** Player loses 3 (5→2); girl gains 25 (25→50 —
  exactly ×2). Either the transfer is a multiplier on the receiver, or the ad
  simply cheated for drama. No in-game rule can be derived.
- The link between the two acts is narrative: he ends Act 1 at **2, as a
  skeleton**, and starts Act 2 at **2, as a skeleton**. Consistent.

### Closing teaser (00:53–00:57)

Same rule, isolated and shown clean: girl at 10 on a short stump, player lying
on the ground counting **1 → 2 → 3**, and the pillar under him **grows in real
time as the number rises**. This is the clearest single statement of the core
fantasy in the whole ad.

---

## 4. Act 2 — The Dungeon (00:21–00:48)

This is the reproducible game.

### 4.1 Presentation

- Fixed-angle **isometric** view, roughly 2:1 dimetric.
- Camera follows the player, and additionally: pans the level at the start,
  and **zooms in for pickup beats** (a "moment" close-up when the player takes
  a multiplier or opens a chest).
- **No HUD whatsoever.** No health bar, no coin counter, no level number, no
  buttons, no pause. All information lives on the world objects themselves.

### 4.2 Entities

| Entity | Badge | Examples seen | Notes |
|---|---|---|---|
| **Player** | Blue rounded-rect, above head | 2 → 3 → 5 → 10 → 17 | Single unit, no squad. |
| **Enemy** | Red pill/ellipse | 1, 2, 4, 6, 7, 8, 9, 10 | Static, standing at a post. |
| **Boss** | Red pill (larger) | **108** | Lich Queen, guards the treasure room. |
| **Elite** | Red pill | **10** (giant skeleton hound), **100** (robed figure) | Higher-tier blockers. |
| **Additive pickup** | Blue `+N` | +7, +10, +25, +30, +50, +100 | Chests, gold barrels, a floating sword. |
| **Multiplier pickup** | Blue `xM` | ×2, ×3, ×4 | Red hearts on skull altars, and chests. |

Enemy visual tiers observed, ascending: plain white skeleton → purple-armoured
skeleton knight → red imp → giant skeleton hound → robed caster → Lich Queen.

### 4.3 Level structure

A **hand-authored, fully-visible maze** on multiple elevation levels, joined by
staircases. Corridors are walled; every route is a deliberate choice. The whole
map is shown to the player in a fly-through before play (00:19–00:21) — this is
what makes it a routing puzzle rather than an exploration game.

Two floor states:

- **Cursed** — dark teal/green, green fog, ghost VFX. Default state.
- **Cleared** — bright white marble with green grass edging. A corridor flips
  to this state after its guarding enemy dies (verified: same corridor at
  00:25.4 teal vs 00:27.4 white).

### 4.4 Verified rules

| ID | Rule | Evidence |
|---|---|---|
| **R2.1** | Defeating an enemy of value `N` adds `N` to player power | `2` beats `1` → `3` (00:26); `3` beats `2` → `5` (00:34) |
| **R2.2** | `+N` pickup adds `N`, with a count-up animation | `10` + `+7` chest → ticks `12`… → `17` (00:42–00:43) |
| **R2.3** | `xM` pickup multiplies | `5` × `x2` heart → `10` (00:40) |
| **R2.4** | Player sprite upgrades with power | bare skeleton (2) → partial flesh (3–5) → full flesh, red shirt, sword (10) → larger hero (17) |
| **R2.5** | A killed enemy **drops its weapon**, and the player picks it up | 00:25.4 → 00:27.4, player gains a visible sword |
| **R2.6** | Clearing a corridor purifies it (teal → white) | 00:25.4 vs 00:27.4 |
| **R2.7** | Bigger pickups sit deeper in the map, behind stronger guards | `+100` / `x4` are in the boss room with the `108` |

### 4.5 Input, as depicted

The ad shows a "ghost finger" overlay:

- A **white ring** = a tap. Taps land on three kinds of target:
  - an **enemy** → the hero walks toward it along a dotted path and fights;
  - a **chest** → it opens and the value is applied;
  - a **ground tile near the hero** → triggers the AoE ability (see below).
- A **blue swirl ring** = the finger dragging/hovering. Enemies under the
  hover **flash orange** — a targeting preview showing what would be engaged.

Movement is a walk animation along a dotted path. There is never a joystick,
d-pad, or swipe gesture on screen.

### 4.6 Combat

- Melee, contact range, ~0.5–1 s: a slash VFX, the enemy dissolves. No health
  bars, no visible damage numbers, no back-and-forth.
- **One AoE ability**, used once at 00:46–00:48 when the player is at 17: a
  frost/ice spear erupts from the tapped ground point and fans out in a cone,
  hitting the `9` and `7` enemies simultaneously. No cooldown UI is shown, so
  we cannot tell whether it is a charged skill, a consumable, or a hero passive.

### 4.7 Ambiguous / contradictory

1. **Enemy growth.** Immediately after the first kill, two distant enemies went
   `6 → 9` and `4 → 7` (both exactly +3, matching the player's new value).
   After the second kill, **no enemy changed**. Cannot be resolved from the
   video — the ad may simply have cheated. Needs a design decision.
2. **What happens on a losing fight** is never shown. The player never engages
   anything stronger than himself.
3. **Whether `+N` / `xM` pickups are consumed** — every pickup shown is taken
   once and the camera moves on; none is revisited.
4. **Order of operations is the actual game.** `x3` then `+50` yields
   `3p + 50`; `+50` then `x3` yields `3(p + 50)`. The ad never states this,
   but it is the only reason the maze layout matters. This is the mechanic
   that makes the game good and it should be designed deliberately.

---

## 5. The core loop, distilled

> You are a number in an authored maze. Every node — enemy, chest, altar — is
> either a gain or a wall. Your number decides which nodes you may take. The
> game is choosing the **route and the order** that turns 2 into more than 108
> before you reach the boss.

Three tension sources, all present in the ad:
1. **Gating** — you can only beat enemies at or below your number.
2. **Sequencing** — multipliers taken late are worth far more than early.
3. **Greed** — the fattest rewards sit behind the strongest guards.

Everything else (art, story, hero identity) is a skin over this.

---

## 6. Not shown in the video — must be invented

- Fail / lose state and retry flow
- Level start, level complete, level select, progression between levels
- Any HUD (the ad deliberately has none; a shipped game likely needs some)
- Tutorial / onboarding
- Audio design
- Save / persistence
- Meta-progression, currency, upgrades
- Pause, settings, any menu at all
- What the boss fight actually plays like

---

## 7. Open decisions

These block the task breakdown. Numbered for easy reply.

1. **MVP scope** — dungeon only, pillar scene only, or both?
2. **Control scheme** — tap-to-target (as depicted), virtual joystick, or drag-to-steer?
3. **Combat resolution** — instant number comparison, real-time HP exchange, or timed auto-battle?
4. **Attacking something stronger than you** — hard-blocked (can't select it),
   allowed but you lose the run, or allowed and you lose the difference in power?
5. **Enemy growth over time** — static enemies, enemies that grow on a timer,
   or enemies that grow when you do? (See §4.7.1.)
6. **Level authoring** — hand-authored levels loaded from data, or procedurally
   generated with a solvability check?
7. **How many levels** in the MVP, and is there a level-select/progression flow
   or a single scene?
8. **Is the AoE ability in scope** for the MVP, or is basic melee enough?
9. **Pillar scene** — if in scope, is it interactive (you choose to give or keep
   your power) or a non-interactive cutscene bookending the dungeon?
