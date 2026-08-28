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

        const float OcclusionBound = IsoProjection.OcclusionBound;

        const float CrestBound = 0.05f;

        const int Complaints = 6;

        const int Samples = 24;

        const float ShotDistance = 60f;

        const float DetailOrthographicSize = 4f;

        const string PlayShot = "dev/scratch/t-36-level-run-play.png";

        const string DetailShot = "dev/scratch/t-36-level-run-detail.png";

        const float SerrationBound = 0.05f;

        static readonly int Slices = StaircaseFlight.PackCrestFromItsOriginOnward.Count;

        public static void Check()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var report = new StringBuilder("t-23/t-31/t-36 staircase steps, ship seed ")
                .Append(Seed.ToString(CultureInfo.InvariantCulture))
                .Append(':');
            var failures = 0;

            using (var models = new WorldModels())
            {
                failures += Resolves(models, report);
                failures += Measured(models, report);
                failures += Built(models, report);
            }

            report.Append("\n  t-23/t-31/t-36: ")
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

            failures += Plinth(models, report);

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

        static int Plinth(WorldModels models, StringBuilder report)
        {
            var prefab = models.Of(PartModel.Foundation);

            if (prefab == null)
            {
                return Assert(
                    report, false, "the foundation mesh sits on the grid", "there is no mesh to measure");
            }

            var box = Local(prefab);
            var edge = IsoProjection.TileEdge;
            var step = IsoProjection.StepHeight;
            var failures = Assert(
                report,
                Math.Abs(box.size.x - DungeonPack.FoundationWidth) <= Epsilon
                && Math.Abs(box.size.z - DungeonPack.FoundationRun) <= Epsilon
                && Math.Abs(box.size.y - DungeonPack.HeightOf(PartModel.Foundation)) <= Epsilon,
                "the imported foundation measures the pinned spans the plinth is stretched from",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "it measures {0:0.#####} by {1:0.#####} by {2:0.#####} against the pinned "
                    + "{3:0.#####} by {4:0.#####} by {5:0.#####}",
                    box.size.x,
                    box.size.y,
                    box.size.z,
                    DungeonPack.FoundationWidth,
                    DungeonPack.HeightOf(PartModel.Foundation),
                    DungeonPack.FoundationRun));

            failures += Assert(
                report,
                Math.Abs(box.center.x) <= Epsilon
                && Math.Abs(box.center.z) <= Epsilon
                && Math.Abs(box.min.y) <= Epsilon,
                "the foundation pivots on the centre of its own base, so a tile centre places it",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "its bounds sit at ({0:0.#####}, {1:0.#####}, {2:0.#####}) with a base at {3:0.#####}",
                    box.center.x,
                    box.center.y,
                    box.center.z,
                    box.min.y));

            var scale = ModelPose.ScaleOf(APlinthPart());
            var stretched = new Vector3(box.size.x * scale.X, box.size.y * scale.Y, box.size.z * scale.Z);

            failures += Assert(
                report,
                Math.Abs(stretched.x - edge) <= Epsilon
                && Math.Abs(stretched.z - edge) <= Epsilon
                && Math.Abs(stretched.y - step) <= Epsilon
                && scale.X > 1f && scale.Y > 1f && scale.Z > 1f,
                "the foundation is stretched to its tile on purpose, and the stretch lands it on exactly "
                + "one tile edge by one tile edge by one step, so it spills nothing",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:0.#####} by {1:0.#####} by {2:0.#####} is stretched {3:0.###}x, {4:0.###}x, "
                    + "{5:0.###}x to {6:0.#####} by {7:0.#####} by {8:0.#####} against a tile edge of "
                    + "{9:0.#####} and a step of {10:0.#####}",
                    box.size.x,
                    box.size.y,
                    box.size.z,
                    scale.X,
                    scale.Y,
                    scale.Z,
                    stretched.x,
                    stretched.y,
                    stretched.z,
                    edge,
                    step));

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

            failures += EveryFootingWearsTheMeshItAsksFor(models, graph, byName, report);
            failures += EveryFlightCrestsAtTheHeadOfItsOwnClimb(graph, byName, report);
            failures += NoTileWearsAFootingItsGridDidNotAskFor(graph, byName, report);
            failures += NoLevelRunIsSerratedUnderneath(graph, byName, report);
            failures += EveryStaircaseSitsInItsDrop(graph, byName, report);
            failures += EveryPlinthFillsItsDropAndSpillsNothing(graph, byName, report);
            failures += EveryTileCarriesExactlyOneWalkingSurface(graph, byName, report);
            failures += AStaircaseStaysWalkableGround(graph, byName, report);
            failures += AStaircaseHidesNothing(graph, byName, report);
            failures += TheMaterialsStayFlat(root, report);

            Photograph(graph, report);

            WorldObjects.Destroy(root);
            builder.Dispose();

            return failures;
        }

        static void Photograph(LevelGraph graph, StringBuilder report)
        {
            var middle = LevelRunCentre(graph);

            PreviewFilm.Sun();

            var play = PreviewFilm.Rig(middle, ShotDistance, IsoProjection.OrthographicSize);
            PreviewFilm.Warm(play);
            PreviewFilm.Shoot(play, PlayShot);
            WorldObjects.Destroy(play.gameObject);

            var detail = PreviewFilm.Rig(middle, ShotDistance, DetailOrthographicSize);
            PreviewFilm.Warm(detail);
            PreviewFilm.Shoot(detail, DetailShot);
            WorldObjects.Destroy(detail.gameObject);

            report.Append("\n  the level run at gameplay distance is in ")
                .Append(PlayShot)
                .Append(" and cropped in ")
                .Append(DetailShot);
        }

        static Vector3 LevelRunCentre(LevelGraph graph)
        {
            var sum = Vector3.zero;
            var counted = 0;

            foreach (var tile in graph.Tiles.Tiles)
            {
                if (TileFootings.Under(graph.Tiles, tile.Position) == TileFooting.Nothing)
                {
                    continue;
                }

                var ground = IsoProjection.Of(tile.Position);
                sum += new Vector3(ground.X, ground.Y, ground.Z);
                counted++;
            }

            return counted == 0 ? Vector3.zero : sum / counted;
        }

        static int Flights(LevelGraph graph)
        {
            var flights = 0;

            foreach (var tile in graph.Tiles.Tiles)
            {
                if (TileFootings.Under(graph.Tiles, tile.Position) == TileFooting.Flight)
                {
                    flights++;
                }
            }

            return flights;
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

        static int EveryFootingWearsTheMeshItAsksFor(
            WorldModels models,
            LevelGraph graph,
            IDictionary<string, Transform> byName,
            StringBuilder report)
        {
            var flightMeshes = Meshes(models.Of(PartModel.Staircase));
            var plinthMeshes = Meshes(models.Of(PartModel.Foundation));
            var flights = 0;
            var flighted = 0;
            var plinths = 0;
            var plinthed = 0;
            var complaint = new List<string>();

            foreach (var tile in graph.Tiles.Tiles)
            {
                var footing = TileFootings.Under(graph.Tiles, tile.Position);

                if (footing == TileFooting.Nothing)
                {
                    continue;
                }

                var flight = footing == TileFooting.Flight;
                var name = flight ? PartNames.Stair(tile.Position) : PartNames.Footing(tile.Position);
                var wanted = flight ? flightMeshes : plinthMeshes;

                if (flight)
                {
                    flights++;
                }
                else
                {
                    plinths++;
                }

                Transform instance;

                if (!byName.TryGetValue(name, out instance))
                {
                    if (complaint.Count < Complaints)
                    {
                        complaint.Add(name + " is not in the world");
                    }

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

                if (dressed && flight)
                {
                    flighted++;
                }
                else if (dressed)
                {
                    plinthed++;
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
                flights > 0 && plinths > 0 && flighted == flights && plinthed == plinths,
                "every tile a step above a lower neighbour carries the pack's staircase mesh, and every "
                + "climbing tile level with all of them carries the pack's foundation mesh",
                flighted + " of " + flights + " flights and " + plinthed + " of " + plinths + " plinths do"
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
                if (TileFootings.Under(graph.Tiles, tile.Position) != TileFooting.Flight
                    || !byName.TryGetValue(PartNames.Stair(tile.Position), out instance))
                {
                    continue;
                }

                counted++;
                var ascent = TileFootings.AscentOf(graph.Tiles, tile.Position);
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
                counted > 0 && atTheHead == counted && counted == Flights(graph),
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

        static int NoTileWearsAFootingItsGridDidNotAskFor(
            LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            var asked = 0;
            var raised = 0;
            var unasked = new List<string>();

            foreach (var tile in graph.Tiles.Tiles)
            {
                var footing = TileFootings.Under(graph.Tiles, tile.Position);
                var flight = byName.ContainsKey(PartNames.Stair(tile.Position));
                var plinth = byName.ContainsKey(PartNames.Footing(tile.Position));

                if (footing != TileFooting.Nothing)
                {
                    asked++;
                }

                if (flight)
                {
                    raised++;
                }

                if (plinth)
                {
                    raised++;
                }

                if (unasked.Count >= Complaints)
                {
                    continue;
                }

                if (flight && plinth)
                {
                    unasked.Add(PartNames.Tile(tile.Position) + " wears a flight and a plinth at once");
                }
                else if (flight && footing != TileFooting.Flight)
                {
                    unasked.Add(PartNames.Stair(tile.Position) + " on ground footed with " + footing);
                }
                else if (plinth && footing != TileFooting.Plinth)
                {
                    unasked.Add(PartNames.Footing(tile.Position) + " on ground footed with " + footing);
                }
            }

            return Assert(
                report,
                unasked.Count == 0 && raised == asked,
                "no tile wears a footing its own grid did not ask for, so the world raises exactly one "
                + "per footed tile and none anywhere else",
                raised + " footings for " + asked + " footed tiles"
                + (unasked.Count == 0 ? "" : "; " + string.Join(", ", unasked.ToArray())));
        }

        static int NoLevelRunIsSerratedUnderneath(
            LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            var level = 0;
            var serrated = 0;
            var deepest = 0f;
            var complaint = new List<string>();

            foreach (var tile in graph.Tiles.Tiles)
            {
                if (TileFootings.Under(graph.Tiles, tile.Position) != TileFooting.Plinth)
                {
                    continue;
                }

                level++;
                var shortfall = Shortfall(tile.Position, byName);
                deepest = Math.Max(deepest, shortfall);

                if (shortfall <= SerrationBound)
                {
                    continue;
                }

                serrated++;

                if (complaint.Count < Complaints)
                {
                    complaint.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} leaves a notch {1:0.###} of a step deep under its floor",
                        PartNames.Tile(tile.Position),
                        shortfall));
                }
            }

            return Assert(
                report,
                level > 0 && serrated == 0,
                "no tile of a level interior run notches its own underside, so a run reads as one climb "
                + "rather than as one flight per tile",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "of {0} level tiles, {1} leave a notch deeper than {2:0.##} of a step, the deepest "
                    + "{3:0.###}{4}",
                    level,
                    serrated,
                    SerrationBound,
                    deepest,
                    complaint.Count == 0 ? "" : "; " + string.Join("; ", complaint.ToArray())));
        }

        static float Shortfall(TilePosition position, IDictionary<string, Transform> byName)
        {
            var ground = IsoProjection.Of(position);
            var floor = ground.Y - IsoProjection.StepHeight;
            var triangles = new List<Vector3[]>();

            foreach (var name in new[] { PartNames.Stair(position), PartNames.Footing(position) })
            {
                Transform instance;
                if (byName.TryGetValue(name, out instance))
                {
                    Triangles(instance, triangles);
                }
            }

            if (triangles.Count == 0)
            {
                return 1f;
            }

            var edge = IsoProjection.TileEdge;
            var worst = 0f;

            for (var axis = 0; axis < 2; axis++)
            {
                var lowest = 1f;

                for (var slice = 0; slice < Samples; slice++)
                {
                    var at = (axis == 0 ? ground.X : ground.Z)
                        + edge * ((slice + 0.5f) / Samples - 0.5f);
                    var crest = Silhouette(triangles, axis, at);
                    var filled = float.IsNaN(crest)
                        ? 0f
                        : Math.Min(1f, Math.Max(0f, (crest - floor) / IsoProjection.StepHeight));

                    lowest = Math.Min(lowest, filled);
                }

                worst = Math.Max(worst, 1f - lowest);
            }

            return worst;
        }

        static float Silhouette(List<Vector3[]> triangles, int axis, float at)
        {
            var crest = float.NaN;

            foreach (var triangle in triangles)
            {
                var low = float.MaxValue;
                var high = float.MinValue;

                for (var corner = 0; corner < 3; corner++)
                {
                    var reach = axis == 0 ? triangle[corner].x : triangle[corner].z;
                    low = Math.Min(low, reach);
                    high = Math.Max(high, reach);
                }

                if (at < low || at > high)
                {
                    continue;
                }

                for (var corner = 0; corner < 3; corner++)
                {
                    var here = triangle[corner];
                    var there = triangle[(corner + 1) % 3];
                    var from = axis == 0 ? here.x : here.z;
                    var to = axis == 0 ? there.x : there.z;

                    if (Math.Abs(to - from) <= float.Epsilon)
                    {
                        crest = float.IsNaN(crest)
                            ? Math.Max(here.y, there.y)
                            : Math.Max(crest, Math.Max(here.y, there.y));
                        continue;
                    }

                    var share = (at - from) / (to - from);
                    if (share < 0f || share > 1f)
                    {
                        continue;
                    }

                    var y = here.y + (there.y - here.y) * share;
                    crest = float.IsNaN(crest) ? y : Math.Max(crest, y);
                }
            }

            return crest;
        }

        static void Triangles(Transform instance, List<Vector3[]> triangles)
        {
            foreach (var filter in instance.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                var vertices = mesh.vertices;
                var indices = mesh.triangles;

                for (var index = 0; index + 2 < indices.Length; index += 3)
                {
                    triangles.Add(new[]
                    {
                        filter.transform.TransformPoint(vertices[indices[index]]),
                        filter.transform.TransformPoint(vertices[indices[index + 1]]),
                        filter.transform.TransformPoint(vertices[indices[index + 2]])
                    });
                }
            }
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
                if (TileFootings.Under(graph.Tiles, tile.Position) != TileFooting.Flight)
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

                var ascent = TileFootings.AscentOf(graph.Tiles, tile.Position);
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

        static int EveryPlinthFillsItsDropAndSpillsNothing(
            LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            var plinths = 0;
            var seated = 0;
            var complaint = new List<string>();
            var edge = IsoProjection.TileEdge;
            var step = IsoProjection.StepHeight;

            foreach (var tile in graph.Tiles.Tiles)
            {
                if (TileFootings.Under(graph.Tiles, tile.Position) != TileFooting.Plinth)
                {
                    continue;
                }

                Transform instance;
                if (!byName.TryGetValue(PartNames.Footing(tile.Position), out instance))
                {
                    continue;
                }

                plinths++;
                var box = World(instance);
                var ground = IsoProjection.Of(tile.Position);

                if (Math.Abs(box.size.x - edge) <= Epsilon
                    && Math.Abs(box.size.z - edge) <= Epsilon
                    && Math.Abs(box.size.y - step) <= Epsilon
                    && Math.Abs(box.center.x - ground.X) <= Epsilon
                    && Math.Abs(box.center.z - ground.Z) <= Epsilon
                    && Math.Abs(box.max.y - ground.Y) <= Epsilon)
                {
                    seated++;
                }
                else if (complaint.Count < Complaints)
                {
                    complaint.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} fills {1:0.#####} by {2:0.#####} by {3:0.#####} centred on "
                        + "({4:0.#####}, {5:0.#####}) topping out at {6:0.#####} rather than {7:0.#####}",
                        instance.name,
                        box.size.x,
                        box.size.y,
                        box.size.z,
                        box.center.x,
                        box.center.z,
                        box.max.y,
                        ground.Y));
                }
            }

            return Assert(
                report,
                plinths > 0 && seated == plinths,
                "every plinth fills its own tile's whole drop and spills nothing off it, so a stretched "
                + "foundation still fits the cube its tile gives it",
                seated + " of " + plinths + " do"
                + (complaint.Count == 0 ? "" : "; " + string.Join("; ", complaint.ToArray())));
        }

        static int EveryTileCarriesExactlyOneWalkingSurface(
            LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            var tiles = 0;
            var surfaced = 0;
            var doubled = new List<string>();
            var bare = new List<string>();

            foreach (var tile in graph.Tiles.Tiles)
            {
                tiles++;
                var flight = TileFootings.Under(graph.Tiles, tile.Position) == TileFooting.Flight;
                var quad = byName.ContainsKey(PartNames.Tile(tile.Position));
                var stair = byName.ContainsKey(PartNames.Stair(tile.Position));

                if (quad != flight && stair == flight)
                {
                    surfaced++;
                    continue;
                }

                if (quad && stair && doubled.Count < Complaints)
                {
                    doubled.Add(PartNames.Tile(tile.Position) + " hangs a floor quad over its own flight");
                }
                else if (!quad && !stair && bare.Count < Complaints)
                {
                    bare.Add(PartNames.Tile(tile.Position) + " has nothing to stand on");
                }
            }

            return Assert(
                report,
                tiles > 0 && surfaced == tiles,
                "every tile carries exactly one walking surface, a floor quad where it is footed with a "
                + "plinth or with nothing and the flight itself where it is footed with one",
                surfaced + " of " + tiles + " do"
                + (doubled.Count == 0 ? "" : "; " + string.Join("; ", doubled.ToArray()))
                + (bare.Count == 0 ? "" : "; " + string.Join("; ", bare.ToArray())));
        }

        static int AStaircaseStaysWalkableGround(
            LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            var climbs = 0;
            var floored = 0;
            var collided = 0;
            var flights = 0;
            var landing = 0;
            var complaint = new List<string>();
            var edge = IsoProjection.TileEdge;
            var step = IsoProjection.StepHeight;

            foreach (var tile in graph.Tiles.Tiles)
            {
                var flight = TileFootings.Under(graph.Tiles, tile.Position) == TileFooting.Flight;

                if (!StaircaseClimb.Climbs(tile.Position) && !flight)
                {
                    continue;
                }

                climbs++;
                Transform surface;

                if (!byName.TryGetValue(
                    LevelBlueprintBuilder.WalkingSurfaceOf(graph.Tiles, tile.Position), out surface))
                {
                    continue;
                }

                var ground = IsoProjection.Of(tile.Position);
                var at = new Vector3(ground.X, ground.Y, ground.Z);

                if (flight
                    || ((surface.position - at).magnitude <= Epsilon
                        && Math.Abs(surface.localEulerAngles.x) < AngleEpsilon
                        && Math.Abs(surface.localEulerAngles.z) < AngleEpsilon))
                {
                    floored++;
                }

                var box = Colliding(surface);

                if (box == null)
                {
                    if (complaint.Count < Complaints)
                    {
                        complaint.Add(surface.name + " carries no collider, so a tap falls through it");
                    }

                    continue;
                }

                collided++;

                if (!flight)
                {
                    continue;
                }

                flights++;
                var reach = box.Value;

                if (Math.Abs(reach.size.x - edge) <= Epsilon
                    && Math.Abs(reach.size.z - edge) <= Epsilon
                    && Math.Abs(reach.center.x - ground.X) <= Epsilon
                    && Math.Abs(reach.center.z - ground.Z) <= Epsilon
                    && Math.Abs(reach.max.y - ground.Y) <= Epsilon
                    && Math.Abs(reach.min.y - (ground.Y - step)) <= Epsilon)
                {
                    landing++;
                }
                else if (complaint.Count < Complaints)
                {
                    complaint.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} is tappable over {1:0.#####} by {2:0.#####} centred on ({3:0.#####}, "
                        + "{4:0.#####}) between {5:0.#####} and {6:0.#####} rather than over its own tile "
                        + "filling the drop under {7:0.#####}",
                        surface.name,
                        reach.size.x,
                        reach.size.z,
                        reach.center.x,
                        reach.center.z,
                        reach.min.y,
                        reach.max.y,
                        ground.Y));
                }
            }

            var failures = Assert(
                report,
                climbs > 0 && floored == climbs,
                "a climbing tile stands on ground at its own elevation, flat where a plinth foots it and "
                + "the flight's own treads where a flight does",
                floored + " of " + climbs + " do");

            failures += Assert(
                report,
                climbs > 0 && collided == climbs,
                "a climbing tile keeps the collider that makes it walkable ground",
                collided + " of " + climbs + " do");

            failures += Assert(
                report,
                flights > 0 && landing == flights,
                "a tap on a tile footed with a flight lands on the flight rather than on air above it: the "
                + "collider covers the whole tile and fills the one step the tile hovers over",
                landing + " of " + flights + " do"
                + (complaint.Count == 0 ? "" : "; " + string.Join("; ", complaint.ToArray())));

            return failures;
        }

        static Bounds? Colliding(Transform instance)
        {
            var found = false;
            var reach = new Bounds();

            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
            {
                if (!found)
                {
                    found = true;
                    reach = collider.bounds;
                    continue;
                }

                reach.Encapsulate(collider.bounds);
            }

            return found ? reach : (Bounds?)null;
        }

        static int AStaircaseHidesNothing(
            LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            var standing = 0f;

            foreach (var tile in graph.Tiles.Tiles)
            {
                foreach (var name in new[] { PartNames.Stair(tile.Position), PartNames.Footing(tile.Position) })
                {
                    Transform instance;
                    if (!byName.TryGetValue(name, out instance))
                    {
                        continue;
                    }

                    standing = Math.Max(standing, World(instance).max.y - IsoProjection.Of(tile.Position).Y);
                }
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

        static WorldPart APlinthPart()
        {
            var graph = LevelGenerator.Generate(Seed, MazePreset.Ship).Graph;

            foreach (var part in LevelBlueprintBuilder.Build(graph).AllParts)
            {
                if (part.Model == PartModel.Foundation)
                {
                    return part;
                }
            }

            return new WorldPart(
                PartNames.Footing(new TilePosition(1, 0, 0)),
                PartShape.Cube,
                PartModel.Foundation,
                PartStyle.Foundation,
                new WorldPoint(0f, -IsoProjection.StepHeight * 0.5f, 0f),
                new WorldPoint(0f, 0f, 0f),
                new WorldPoint(IsoProjection.TileEdge, IsoProjection.StepHeight, IsoProjection.TileEdge));
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
