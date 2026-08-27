using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Game.Domain;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTooling
{
    public static class StaircaseCheckCommand
    {
        const long Seed = 20250824L;

        const float Epsilon = DungeonPack.BoundsEpsilon;

        const float AngleEpsilon = 0.01f;

        const float OcclusionBound = 0.5f;

        const float CrestBound = 0.05f;

        const int Complaints = 6;

        static readonly int Slices = StaircaseFlight.PackCrestFromItsOriginOnward.Count;

        public static void Check()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var report = new StringBuilder("t-23/t-31 staircase steps, ship seed ")
                .Append(Seed.ToString(CultureInfo.InvariantCulture))
                .Append(':');
            var failures = 0;

            using (var models = new WorldModels())
            {
                failures += Resolves(models, report);
                failures += Measured(models, report);
                failures += Built(models, report);
            }

            report.Append("\n  t-23/t-31: ")
                .Append(failures == 0
                    ? "every assertion above held"
                    : failures + (failures == 1 ? " assertion" : " assertions") + " above failed");

            Debug.Log(report.ToString());

            if (failures > 0)
            {
                Debug.LogError("The staircase check failed " + failures + " assertions. Read the report above.");
            }
        }

        static int Resolves(WorldModels models, StringBuilder report)
        {
            var path = WorldModels.AssetPathOf(PartModel.Staircase);
            var prefab = models.Of(PartModel.Staircase);

            var failures = Assert(
                report,
                prefab != null,
                "the staircase part model resolves to a loadable mesh",
                PartModel.Staircase + " to " + (path ?? "nothing") + " = "
                + (prefab == null ? "NOTHING" : prefab.name));

            failures += Assert(
                report,
                PartModels.Of(PartStyle.Staircase) == PartModel.Staircase
                && PartModels.Of(PartStyle.Floor) == PartModel.FloorTile,
                "a staircase asks for its own mesh and leaves the floor tile alone",
                PartStyle.Staircase + " wants " + PartModels.Of(PartStyle.Staircase) + ", "
                + PartStyle.Floor + " wants " + PartModels.Of(PartStyle.Floor));

            return failures;
        }

        static int Measured(WorldModels models, StringBuilder report)
        {
            var prefab = models.Of(PartModel.Staircase);

            if (prefab == null)
            {
                return Assert(
                    report, false, "the staircase mesh sits on the grid", "there is no mesh to measure");
            }

            var box = Local(prefab);
            var edge = IsoProjection.TileEdge;
            var failures = 0;

            failures += Assert(
                report,
                Math.Abs(box.size.x - DungeonPack.StaircaseWidth) <= Epsilon
                && Math.Abs(box.size.z - DungeonPack.StaircaseRun) <= Epsilon
                && Math.Abs(DungeonPack.StaircaseWidth - edge) <= Epsilon
                && Math.Abs(DungeonPack.StaircaseRun - edge) <= Epsilon,
                "the imported staircase covers exactly one tile at the pinned spans, so it needs no stretching "
                + "across the ground",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "it measures {0:0.#####} wide by {1:0.#####} along the climb against the pinned "
                    + "{2:0.#####} by {3:0.#####} and a tile edge of {4:0.#####}",
                    box.size.x,
                    box.size.z,
                    DungeonPack.StaircaseWidth,
                    DungeonPack.StaircaseRun,
                    edge));

            failures += Assert(
                report,
                Math.Abs(box.size.y - DungeonPack.HeightOf(PartModel.Staircase)) <= Epsilon,
                "the imported staircase stands the pinned "
                + DungeonPack.StaircasePackHeight.ToString("0.####", CultureInfo.InvariantCulture)
                + " pack units tall before it is fitted to the step",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "it measures {0:0.#####} against {1:0.#####}",
                    box.size.y,
                    DungeonPack.HeightOf(PartModel.Staircase)));

            failures += Assert(
                report,
                Math.Abs(box.center.x) <= Epsilon
                && Math.Abs(box.min.y) <= Epsilon
                && Math.Abs(box.min.z) <= Epsilon,
                "the staircase pivots on the centre of its base at the crest end of its flight, so a tile edge "
                + "places it",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "its bounds sit at ({0:0.#####}, {1:0.#####}, {2:0.#####}) with a base at {3:0.#####} "
                    + "and a crest end at {4:0.#####}",
                    box.center.x,
                    box.center.y,
                    box.center.z,
                    box.min.y,
                    box.min.z));

            var slices = Crest(prefab, box);
            var pinned = StaircaseFlight.PackCrestFromItsOriginOnward;
            var drift = 0f;

            for (var slice = 0; slice < Slices; slice++)
            {
                drift = Math.Max(drift, Math.Abs(slices[slice] - pinned[slice] * DungeonPack.ImportScale));
            }

            failures += Assert(
                report,
                drift <= Epsilon && slices[0] > slices[Slices - 1],
                "the pack's flight descends along its own local forward, and the profile the pose is laid from "
                + "is still the profile the mesh has",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "its crest over {0} slices of local z reads {1} against the pinned {2}, drifting "
                    + "{3:0.#####} against a bound of {4:0.#####}",
                    Slices,
                    Sliced(slices),
                    Sliced(Scaled(pinned)),
                    drift,
                    Epsilon));

            var fitted = box.size.y * ModelPose.ScaleOf(AStaircasePart()).Y;

            failures += Assert(
                report,
                Math.Abs(fitted - IsoProjection.StepHeight) <= Epsilon,
                "the fitted staircase rises exactly the one elevation step a staircase tile stands above "
                + "the terrace below it",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:0.#####} pack units are squashed to {1:0.#####} against a step of {2:0.#####}, "
                    + "and two of those steps make the {3:0.#####} between one terrace and the next",
                    DungeonPack.StaircasePackHeight,
                    fitted,
                    IsoProjection.StepHeight,
                    Terraces.Rise * IsoProjection.StepHeight));

            return failures;
        }

        static int Built(WorldModels models, StringBuilder report)
        {
            var graph = LevelGenerator.Generate(Seed, MazePreset.Ship).Graph;
            var builder = new WorldBuilder();
            var root = builder.Build(graph);
            var failures = 0;

            var byName = new Dictionary<string, Transform>();
            foreach (var node in root.GetComponentsInChildren<Transform>(true))
            {
                byName[node.name] = node;
            }

            report.Append(Shape(graph));

            failures += EveryClimbWearsAStaircase(models, graph, byName, report);
            failures += EveryFlightCrestsAtTheHeadOfItsOwnClimb(graph, byName, report);
            failures += NoTerraceTileWearsOne(graph, byName, report);
            failures += EveryStaircaseSitsInItsDrop(graph, byName, report);
            failures += AStaircaseStaysWalkableGround(graph, byName, report);
            failures += AStaircaseHidesNothing(graph, byName, report);
            failures += TheMaterialsStayFlat(root, report);

            WorldObjects.Destroy(root);
            builder.Dispose();

            return failures;
        }

        static string Shape(LevelGraph graph)
        {
            var climbing = new List<TilePosition>();
            foreach (var tile in graph.Tiles.Tiles)
            {
                if (StaircaseClimb.Climbs(tile.Position))
                {
                    climbing.Add(tile.Position);
                }
            }

            var runs = 0;
            var bends = 0;
            var seen = new HashSet<TilePosition>();

            foreach (var start in climbing)
            {
                if (seen.Contains(start))
                {
                    continue;
                }

                runs++;
                var frontier = new List<TilePosition> { start };
                seen.Add(start);

                for (var index = 0; index < frontier.Count; index++)
                {
                    var here = frontier[index];
                    var ascent = StaircaseClimb.AscentOf(graph.Tiles, here);

                    foreach (var neighbour in graph.Tiles.Neighbours(here))
                    {
                        if (neighbour.Elevation != here.Elevation)
                        {
                            continue;
                        }

                        if (StaircaseClimb.AscentOf(graph.Tiles, neighbour) != ascent)
                        {
                            bends++;
                        }

                        if (seen.Add(neighbour))
                        {
                            frontier.Add(neighbour);
                        }
                    }
                }
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  the level climbs on {0} staircase tiles of {1}, in {2} {3}, turning {4} {5}",
                climbing.Count,
                graph.Tiles.Tiles.Count,
                runs,
                runs == 1 ? "run" : "runs",
                bends / 2,
                bends / 2 == 1 ? "time" : "times");
        }

        static int EveryClimbWearsAStaircase(
            WorldModels models,
            LevelGraph graph,
            IDictionary<string, Transform> byName,
            StringBuilder report)
        {
            var wanted = Meshes(models.Of(PartModel.Staircase));
            var climbs = 0;
            var meshed = 0;
            var complaint = new List<string>();

            foreach (var tile in graph.Tiles.Tiles)
            {
                if (!StaircaseClimb.Climbs(tile.Position))
                {
                    continue;
                }

                climbs++;
                var name = PartNames.Stair(tile.Position);
                Transform instance;

                if (!byName.TryGetValue(name, out instance))
                {
                    complaint.Add(name + " is not in the world");
                    continue;
                }

                var filters = instance.GetComponentsInChildren<MeshFilter>(true);
                var dressed = filters.Length > 0;

                foreach (var filter in filters)
                {
                    if (filter.sharedMesh == null || !wanted.Contains(filter.sharedMesh))
                    {
                        dressed = false;
                    }
                }

                if (dressed)
                {
                    meshed++;
                }
                else if (complaint.Count < Complaints)
                {
                    complaint.Add(name + " wearing "
                        + (filters.Length == 0 || filters[0].sharedMesh == null
                            ? "nothing"
                            : filters[0].sharedMesh.name));
                }
            }

            return Assert(
                report,
                climbs > 0 && meshed == climbs,
                "every staircase tile carries the pack's staircase mesh",
                meshed + " of " + climbs + " do"
                + (complaint.Count == 0 ? "" : "; " + string.Join("; ", complaint.ToArray())));
        }

        static int EveryFlightCrestsAtTheHeadOfItsOwnClimb(
            LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            var counted = 0;
            var atTheHead = 0;
            var atTheFoot = 0;
            var flat = 0;
            var complaint = new List<string>();

            foreach (var tile in graph.Tiles.Tiles)
            {
                Transform instance;
                if (!StaircaseClimb.Climbs(tile.Position)
                    || !byName.TryGetValue(PartNames.Stair(tile.Position), out instance))
                {
                    continue;
                }

                counted++;
                var ascent = StaircaseClimb.AscentOf(graph.Tiles, tile.Position);
                var ground = IsoProjection.Of(tile.Position);
                var floor = ground.Y - IsoProjection.StepHeight;
                var head = float.NaN;
                var foot = float.NaN;

                foreach (var filter in instance.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = filter.sharedMesh;
                    if (mesh == null)
                    {
                        continue;
                    }

                    foreach (var vertex in mesh.vertices)
                    {
                        var world = filter.transform.TransformPoint(vertex);
                        var standing = world.y - floor;
                        var reach = StaircaseFlight.ReachAlong(
                            new WorldPoint(world.x, world.y, world.z), ground, ascent);

                        if (reach > 0f)
                        {
                            head = float.IsNaN(head) ? standing : Math.Max(head, standing);
                        }
                        else if (reach < 0f)
                        {
                            foot = float.IsNaN(foot) ? standing : Math.Max(foot, standing);
                        }
                    }
                }

                var readable = !float.IsNaN(head) && !float.IsNaN(foot);

                if (readable && head > foot + CrestBound)
                {
                    atTheHead++;
                    continue;
                }

                if (readable && foot > head + CrestBound)
                {
                    atTheFoot++;
                }
                else
                {
                    flat++;
                }

                if (complaint.Count < Complaints)
                {
                    complaint.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} climbing {1} stands {2} tall at the head of its own climb and {3} at its foot",
                        instance.name,
                        ascent,
                        Standing(head),
                        Standing(foot)));
                }
            }

            return Assert(
                report,
                counted > 0 && atTheHead == counted,
                "every flight's mass crests at the head of its own climb and sinks at its foot, so a tread "
                + "faces the camera instead of the masonry filling the drop",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "of {0} staircase tiles, {1} crest at the head of the climb, {2} crest at its foot and "
                    + "{3} crest level{4}",
                    counted,
                    atTheHead,
                    atTheFoot,
                    flat,
                    complaint.Count == 0 ? "" : "; " + string.Join("; ", complaint.ToArray())));
        }

        static int NoTerraceTileWearsOne(
            LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            var raised = 0;
            var climbs = 0;
            var terraced = new List<string>();

            foreach (var tile in graph.Tiles.Tiles)
            {
                var standing = byName.ContainsKey(PartNames.Stair(tile.Position));

                if (standing)
                {
                    raised++;
                }

                if (StaircaseClimb.Climbs(tile.Position))
                {
                    climbs++;
                }
                else if (standing)
                {
                    terraced.Add(PartNames.Stair(tile.Position));
                }
            }

            return Assert(
                report,
                terraced.Count == 0 && raised == climbs,
                "a terrace tile carries no staircase, so the world raises exactly one per climb",
                raised + " staircases for " + climbs + " climbing tiles"
                + (terraced.Count == 0 ? "" : "; on a terrace: " + string.Join(", ", terraced.ToArray())));
        }

        static int EveryStaircaseSitsInItsDrop(
            LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            var climbs = 0;
            var rising = 0;
            var crested = 0;
            var spanning = 0;
            var complaint = new List<string>();
            var edge = IsoProjection.TileEdge;
            var step = IsoProjection.StepHeight;

            foreach (var tile in graph.Tiles.Tiles)
            {
                if (!StaircaseClimb.Climbs(tile.Position))
                {
                    continue;
                }

                Transform instance;
                if (!byName.TryGetValue(PartNames.Stair(tile.Position), out instance))
                {
                    continue;
                }

                climbs++;
                var box = World(instance);
                var ground = IsoProjection.Of(tile.Position);

                if (Math.Abs(box.size.y - step) <= Epsilon
                    && Math.Abs(box.max.y - ground.Y) <= Epsilon
                    && Math.Abs(box.min.y - (ground.Y - step)) <= Epsilon)
                {
                    rising++;
                }
                else if (complaint.Count < Complaints)
                {
                    complaint.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} rises {1:0.#####} from {2:0.#####} to {3:0.#####} rather than {4:0.#####} "
                        + "from {5:0.#####} to {6:0.#####}",
                        instance.name,
                        box.size.y,
                        box.min.y,
                        box.max.y,
                        step,
                        ground.Y - step,
                        ground.Y));
                }

                var ascent = StaircaseClimb.AscentOf(graph.Tiles, tile.Position);
                var along = TileSides.Toward(ascent);
                var run = Math.Abs(along.X) > 0.5f ? box.size.x : box.size.z;
                var across = Math.Abs(along.X) > 0.5f ? box.size.z : box.size.x;

                if (Math.Abs(run - edge) <= Epsilon && Math.Abs(across - edge) <= Epsilon)
                {
                    spanning++;
                }
                else if (complaint.Count < Complaints)
                {
                    complaint.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} covers {1:0.#####} by {2:0.#####} of its tile",
                        instance.name,
                        run,
                        across));
                }

                var crest = new Vector3(
                    ground.X + along.X * edge * 0.5f,
                    ground.Y - step,
                    ground.Z + along.Z * edge * 0.5f);
                var here = instance.position;

                if ((here - crest).magnitude <= Epsilon)
                {
                    crested++;
                }
                else if (complaint.Count < Complaints)
                {
                    complaint.Add(instance.name + " sets its crest down at " + here + " rather than " + crest);
                }
            }

            var failures = Assert(
                report,
                climbs > 0 && rising == climbs,
                "every staircase fills the one step its tile hovers over, topping out flush with its floor",
                rising + " of " + climbs + " do");

            failures += Assert(
                report,
                climbs > 0 && spanning == climbs,
                "every staircase covers its whole tile, so a run of them meets with no gap",
                spanning + " of " + climbs + " do");

            failures += Assert(
                report,
                climbs > 0 && crested == climbs,
                "every staircase sets its crest end down on the tile edge its climb ends at",
                crested + " of " + climbs + " do"
                + (complaint.Count == 0 ? "" : "; " + string.Join("; ", complaint.ToArray())));

            return failures;
        }

        static int AStaircaseStaysWalkableGround(
            LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            var climbs = 0;
            var floored = 0;
            var collided = 0;
            var loose = 0;

            foreach (var tile in graph.Tiles.Tiles)
            {
                if (!StaircaseClimb.Climbs(tile.Position))
                {
                    continue;
                }

                climbs++;
                Transform floor;

                if (byName.TryGetValue(PartNames.Tile(tile.Position), out floor))
                {
                    var ground = IsoProjection.Of(tile.Position);
                    var at = new Vector3(ground.X, ground.Y, ground.Z);

                    if ((floor.position - at).magnitude <= Epsilon
                        && Math.Abs(floor.localEulerAngles.x) < AngleEpsilon
                        && Math.Abs(floor.localEulerAngles.z) < AngleEpsilon)
                    {
                        floored++;
                    }

                    if (floor.GetComponentInChildren<Collider>() != null)
                    {
                        collided++;
                    }
                }

                Transform stair;
                if (byName.TryGetValue(PartNames.Stair(tile.Position), out stair)
                    && stair.GetComponentInChildren<Collider>() != null)
                {
                    loose++;
                }
            }

            var failures = Assert(
                report,
                climbs > 0 && floored == climbs,
                "a staircase tile keeps the same flat floor tile at its own elevation that a terrace tile has",
                floored + " of " + climbs + " do");

            failures += Assert(
                report,
                climbs > 0 && collided == climbs,
                "a staircase tile keeps the collider that makes a floor tile walkable ground",
                collided + " of " + climbs + " do");

            failures += Assert(
                report,
                loose == 0,
                "the staircase geometry itself carries no collider, so it cannot be walked into",
                loose + " of " + climbs + " carry one");

            return failures;
        }

        static int AStaircaseHidesNothing(
            LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            var standing = 0f;

            foreach (var tile in graph.Tiles.Tiles)
            {
                Transform instance;
                if (!StaircaseClimb.Climbs(tile.Position)
                    || !byName.TryGetValue(PartNames.Stair(tile.Position), out instance))
                {
                    continue;
                }

                standing = Math.Max(standing, World(instance).max.y - IsoProjection.Of(tile.Position).Y);
            }

            var hidden = IsoProjection.SightReach(Math.Max(standing, 0f));

            return Assert(
                report,
                hidden <= IsoProjection.TileEdge * OcclusionBound,
                "a staircase hides at most " + OcclusionBound.ToString("0.##", CultureInfo.InvariantCulture)
                + " of the tile edge behind it, so the climb costs no ground in sight",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "the tallest staircase stands {0:0.#####} above the floor it tops out at and hides "
                    + "{1:0.###} tile edges at the camera's {2:0.#} degree pitch, against the bound of "
                    + "{3:0.###} and the {4:0.###} the parapet hides",
                    standing,
                    hidden / IsoProjection.TileEdge,
                    IsoProjection.CameraPitch,
                    OcclusionBound,
                    IsoProjection.SightReach(DungeonPack.HeightOf(PartModel.WallPanel))
                    / IsoProjection.TileEdge));
        }

        static int TheMaterialsStayFlat(GameObject root, StringBuilder report)
        {
            var world = new List<Material>();

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var material = renderer.sharedMaterial;
                if (material != null
                    && material.name.StartsWith(WorldMaterials.NamePrefix, StringComparison.Ordinal)
                    && !world.Contains(material))
                {
                    world.Add(material);
                }
            }

            var ceiling = Enum.GetValues(typeof(PartStyle)).Length;

            return Assert(
                report,
                world.Count <= ceiling,
                "the built world's material count is within its ceiling of " + ceiling,
                world.Count + " distinct world materials for "
                + root.GetComponentsInChildren<Renderer>(true).Length + " renderers");
        }

        static WorldPart AStaircasePart()
        {
            var graph = LevelGenerator.Generate(Seed, MazePreset.Ship).Graph;

            foreach (var part in LevelBlueprintBuilder.Build(graph).AllParts)
            {
                if (part.Model == PartModel.Staircase)
                {
                    return part;
                }
            }

            throw new InvalidOperationException("The ship level climbs, so it always has a staircase part.");
        }

        static HashSet<Mesh> Meshes(GameObject prefab)
        {
            var meshes = new HashSet<Mesh>();

            if (prefab == null)
            {
                return meshes;
            }

            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null)
                {
                    meshes.Add(filter.sharedMesh);
                }
            }

            return meshes;
        }

        static string Standing(float height)
        {
            return float.IsNaN(height)
                ? "nothing"
                : height.ToString("0.####", CultureInfo.InvariantCulture);
        }

        static float[] Crest(GameObject prefab, Bounds box)
        {
            var crest = new float[Slices];

            for (var slice = 0; slice < Slices; slice++)
            {
                crest[slice] = float.MinValue;
            }

            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                foreach (var vertex in mesh.vertices)
                {
                    var local = prefab.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                    var reach = box.size.z <= 0f ? 0f : (local.z - box.min.z) / box.size.z;
                    var slice = Math.Min(Slices - 1, Math.Max(0, (int)(reach * Slices)));
                    crest[slice] = Math.Max(crest[slice], local.y);
                }
            }

            return crest;
        }

        static float[] Scaled(IReadOnlyList<float> pack)
        {
            var scaled = new float[pack.Count];

            for (var slice = 0; slice < pack.Count; slice++)
            {
                scaled[slice] = pack[slice] * DungeonPack.ImportScale;
            }

            return scaled;
        }

        static string Sliced(IReadOnlyList<float> crest)
        {
            var text = new StringBuilder();

            for (var slice = 0; slice < crest.Count; slice++)
            {
                text.Append(slice == 0 ? "" : " ")
                    .Append(crest[slice] == float.MinValue
                        ? "-"
                        : crest[slice].ToString("0.####", CultureInfo.InvariantCulture));
            }

            return text.ToString();
        }

        static Bounds Local(GameObject prefab)
        {
            var box = new Bounds();
            var first = true;

            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                var offset = prefab.transform.InverseTransformPoint(
                    filter.transform.TransformPoint(mesh.bounds.center));
                var here = new Bounds(offset, Vector3.Scale(mesh.bounds.size, filter.transform.lossyScale));

                if (first)
                {
                    box = here;
                    first = false;
                }
                else
                {
                    box.Encapsulate(here);
                }
            }

            return box;
        }

        static Bounds World(Transform instance)
        {
            var box = new Bounds();
            var first = true;

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (first)
                {
                    box = renderer.bounds;
                    first = false;
                }
                else
                {
                    box.Encapsulate(renderer.bounds);
                }
            }

            return box;
        }

        static int Assert(StringBuilder report, bool held, string claim, string detail)
        {
            report.Append("\n  ").Append(held ? "ok   " : "FAIL ").Append(claim).Append(" - ").Append(detail);

            return held ? 0 : 1;
        }
    }
}
