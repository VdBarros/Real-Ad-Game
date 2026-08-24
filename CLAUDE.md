# Real Ad Game — Number Maze

## Standing instructions for all agents

- **Unity 6000.5.9f1, URP, portrait-only, mobile target, new Input System.**
  This is the version pinned in `ProjectSettings/ProjectVersion.txt` — verify
  with `unity projects info` (unity-cli) if it may have changed, don't assume.
- **Do not write comments in code.** Names carry the meaning. Exception: a
  comment is explicitly requested in a task.
- Free / built-in Unity assets and primitives only. Art is placeholder; do not
  spend time on it.
- Every task lists acceptance criteria. A task is done when those pass, not
  when the code compiles.
- **Assembly definition (asmdef) boundaries are load-bearing, not advisory:**
  - `Game.Domain` — plain C#, **zero `UnityEngine` references**. Rules,
    graph, generator, validator. Must be unit-testable outside play mode.
  - `Game.Domain.Tests` — NUnit tests for `Game.Domain`.
  - `Game.Presentation` — Unity-side rendering of a `LevelGraph` (world
    builder, badges, camera, floor state).
  - `Game.Interaction` — input, pathfinding, encounter/pickup resolution.
  - `Game.Flow` — game state machine, cutscene.
  - Domain must never reference Presentation/Interaction/Flow, or `UnityEngine`
    at all. If a domain type needs Unity, it's in the wrong assembly.
- **Agents stay in `.cs` files. Never hand-edit `.unity`, `.prefab`, or
  `.asset` YAML.** Hand-written fileIDs/GUIDs are easy to get wrong and these
  files merge catastrophically across parallel agents. Build the world from
  code — T-08's `WorldBuilder` instantiates everything from a `LevelGraph` at
  runtime; that's the template for all presentation work. Minimize Editor
  wiring. This is enforced, not just documented: `.claude/settings.json`
  denies Edit/Write on `*.unity`/`*.prefab`/`*.asset`.
- **Prefer the `unity-cli` skill / Unity MCP over raw shell or manual Editor
  steps whenever the task touches Unity** — project/editor info, package and
  module management, builds, running tests, scene/hierarchy inspection, and
  live-Editor operations. Fall back to hand-editing only for things unity-cli
  genuinely can't do.
- **Fast Phase 1 loop:** `Game.Domain`/`Game.Domain.Tests` source
  (`Assets/Scripts/Domain(.Tests)/`) is also globbed by a standalone project
  at `dotnet/Game.Domain.Tests/` — same files, no duplication. Run
  `dotnet test dotnet/Game.Domain.Tests` (~2s) instead of entering the Unity
  Editor for domain work. 6 of 18 tasks are Phase 1 and that's where the
  invariant-correctness risk sits — see mvp-backlog §2 and §5.

## Docs

- [`docs/ad-analysis.md`](docs/ad-analysis.md) — source of truth for *what the
  reference ad showed*. Verified-vs-ambiguous breakdown of the game being
  reproduced.
- [`docs/mvp-backlog.md`](docs/mvp-backlog.md) — source of truth for *what we
  are building*: locked design decisions, core rules spec, architecture, and
  the full task backlog (T-01…T-18). §0 of that doc is the same standing
  instructions as above; §2 is the exact rules spec implementers must match.

## Agent skills

### Issue tracker

Issues live in GitHub Issues on `VdBarros/Real-Ad-Game`, via the `gh` CLI.
See `docs/agents/issue-tracker.md`.

### Triage labels

Default five-role vocabulary; label string equals role name.
See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: `CONTEXT.md` at the root, ADRs in `docs/adr/`.
See `docs/agents/domain.md`.
