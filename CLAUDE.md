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
  - `Game.Presentation.Pure` — plain C#, **zero `UnityEngine` references**.
    Presentation logic that is a pure function: visual tier from power,
    fly-through waypoints, the grid-to-world projection, the zoom-beat
    trigger. Lives here so the fast loop can test it.
  - `Game.Presentation` — Unity-side rendering of a `LevelGraph` (world
    builder, badges, camera, floor state). References `Game.Presentation.Pure`.
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
  **Neither binary is on `PATH` — call them by full path, don't conclude the
  tooling is missing:**
  - `C:\Users\vinib\AppData\Local\Unity\bin\unity.exe` — the unity CLI
  - `C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Unity.exe` — the
    Editor, for `-batchmode -quit -executeMethod …`

  Batch mode fails while the Editor is open on this project (Unity lock), so
  close it first. Driving a *live* Editor with `unity command` additionally
  needs `com.unity.pipeline` in `Packages/manifest.json`, which is not there
  yet — add it with `unity pipeline install` if a task needs live control.
- **Fast Phase 1 loop:** `Game.Domain`, `Game.Presentation.Pure` and
  `Game.Domain.Tests` source (`Assets/Scripts/Domain/`,
  `Assets/Scripts/Presentation.Pure/`, `Assets/Scripts/Domain.Tests/`) is also
  globbed by standalone projects under `dotnet/` — same files, no duplication.
  Run `dotnet test dotnet/Game.Domain.Tests` (~30s, of which T-07's ten-thousand
  seed fuzz sweep is ~25s) instead of entering the Unity Editor for domain work;
  add `--filter "FullyQualifiedName!~GeneratorFuzz"` for a ~5s loop while
  iterating, and run the whole thing before you commit. The sweep's measurements
  — rejection rate by reason, mean generation time, peak oracle state counts, the
  `P_max/P_min` spread — print from a passing test, so they are only visible with
  `-v n`. 6 of 18 tasks are Phase 1
  and that's where the invariant-correctness risk sits — see mvp-backlog §2.
  **Those projects compile the way Unity compiles**, or domain code passes the
  fast loop and fails in the Editor: `dotnet/Directory.Build.props` pins C# 9
  for everything under `dotnet/`, and `Game.Domain` and
  `Game.Presentation.Pure` target netstandard2.1.
  **The NUnit the fast loop runs is not the NUnit the Editor compiles.** Unity
  6000.5.9f1 ships `com.unity.ext.nunit` 2.1.0, a fork of `nunit.framework`
  **3.5**, whose assemblies are .NET Framework only and cannot load under the
  net10.0 test host — so the suite *runs* against 3.14
  (`FastLoopNUnitVersion`) and is separately *compiled* against 3.5
  (`UnityNUnitVersion`) by `dotnet/Game.Domain.Tests.Conformance/`, which
  globs the same test sources at Unity's TFM and runs nothing. The risk is not
  only NUnit 4: anything added between 3.5 and 3.14 compiles in `dotnet test`
  and fails the Editor. `Does.Not.Contain(<non-string>)` is the case that broke
  `main` twice — write `Has.No.Member(x)` for a non-string. Do not raise
  `UnityNUnitVersion`; raise `FastLoopNUnitVersion` only if it stays ≥ the
  Editor's surface.
  Two projects must **fail** to build, and CI asserts each failure so neither
  ceiling can silently drift: `dotnet/LangVersionProbe/` with CS8773, and
  `dotnet/NUnitApiProbe/` with CS1503.
- **Unity-side settings are applied from code, not by hand.**
  `Assets/Editor/ProjectBootstrap.cs` owns portrait lock, bundle id, scripting
  backend and API level; `Assets/Editor/AndroidBuildCommand.cs` produces the
  APK. Both run from the `Tools/Real Ad Game` menu or via
  `-executeMethod Game.EditorTooling.ProjectBootstrap.Apply` /
  `Game.EditorTooling.AndroidBuildCommand.Build` in batch mode.

## Where things live

Four artifacts, four jobs. Nothing restates another.

- **[Spec: Number Maze MVP](https://github.com/VdBarros/Real-Ad-Game/issues/17)**
  — source of truth for *what we are building*: problem, solution, user
  stories, and every implementation and testing decision.
- **T-01 … T-18** (GitHub issues, `ready-for-agent`, phase-labelled, wired with
  native `blocked_by` edges) — source of truth for *what is left to do*. The
  frontier is whatever has no open blocker. **Do not restate the task list
  anywhere else.**
- [`docs/mvp-backlog.md`](docs/mvp-backlog.md) — the premises (§1), the core
  rules spec with its invariant proofs (§2), and the architecture (§3). §0 is
  the same standing instructions as above. **§2 is what implementers must match
  exactly**, and it holds reasoning no ticket does — Invariant B's proof, in
  particular.
- [`docs/ad-analysis.md`](docs/ad-analysis.md) — source of truth for *what the
  reference ad showed*, including what it faked. §7 records where each original
  open question was answered.

Reasoning, measurements and rejected alternatives for every decision live in
the closed tickets of the wayfinding map,
[issue #1](https://github.com/VdBarros/Real-Ad-Game/issues/1). **Where a doc and
a closed ticket disagree, the ticket wins.**

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
