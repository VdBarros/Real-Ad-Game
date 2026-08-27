using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Domain;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.EditorTooling
{
    public static class StaircasePrototypeCommand
    {
        const long Seed = 20250824L;

        const string ShotFolder = "dev/scratch/t-31/";

        const int CropEdge = 700;

        const int Slices = 10;

        const string Narrow = "stairs_narrow";

        const string Standard = "stairs";

        const string Wide = "stairs_wide";

        const string Walled = "stairs_walled";

        const string Foundation = "floor_foundation_allsides";

        delegate void Remedy(Stage stage);

        public static void Check()
        {
            var report = new StringBuilder("t-31 staircase prototypes, ship seed ")
                .Append(Seed.ToString(CultureInfo.InvariantCulture))
                .Append(':');

            Profiles(report);
            Shape(report);

            Shoot("warm", stage => { }, report);
            Shoot("a-baseline", stage => { }, report);
            Shoot("b-crest-at-the-head", Corrected, report);
            Shoot("c-crest-at-the-head-both-ends", CorrectedBothEnds, report);
            Shoot("d-crest-at-the-head-foot-only", CorrectedFootOnly, report);
            Shoot("e-plinth-all", PlinthAll, report);
            Shoot("f-across", Across, report);
            Shoot("g-walled", Walls, report);
            Shoot("h-wide", Widened, report);

            report.Append("\n  t-31: nine prototypes written under ").Append(ShotFolder);

            Debug.Log(report.ToString());
        }

        static void Profiles(StringBuilder report)
        {
            foreach (var asset in new[] { Narrow, Standard, Wide, Walled, Foundation })
            {
                var prefab = Resources.Load<GameObject>(WorldModels.ResourcesFolder + "/" + asset);

                if (prefab == null)
                {
                    report.Append("\n  ").Append(asset).Append(" resolves to nothing loadable");
                    continue;
                }

                var box = Local(prefab);

                report.Append(string.Format(
                    CultureInfo.InvariantCulture,
                    "\n  {0} measures {1:0.####} x {2:0.####} x {3:0.####} from ({4:0.####}, {5:0.####}, "
                    + "{6:0.####}) and its crest over ten slices of local z reads {7}",
                    asset,
                    box.size.x,
                    box.size.y,
                    box.size.z,
                    box.min.x,
                    box.min.y,
                    box.min.z,
                    Crest(prefab, box)));
            }
        }

        static string Crest(GameObject prefab, Bounds box)
        {
            var crest = new float[Slices];
            var found = false;

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

                var vertices = mesh.vertices;
                if (vertices.Length == 0)
                {
                    continue;
                }

                found = true;

                foreach (var vertex in vertices)
                {
                    var local = prefab.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                    var reach = box.size.z <= 0f ? 0f : (local.z - box.min.z) / box.size.z;
                    var slice = Math.Min(Slices - 1, Math.Max(0, (int)(reach * Slices)));
                    crest[slice] = Math.Max(crest[slice], local.y);
                }
            }

            if (!found)
            {
                return "nothing readable";
            }

            var text = new StringBuilder();
            for (var slice = 0; slice < Slices; slice++)
            {
                text.Append(slice == 0 ? "" : " ")
                    .Append(crest[slice] == float.MinValue
                        ? "-"
                        : crest[slice].ToString("0.###", CultureInfo.InvariantCulture));
            }

            return text.ToString();
        }

        static void Shape(StringBuilder report)
        {
            var graph = LevelGenerator.Generate(Seed, MazePreset.Ship).Graph;
            var runs = Runs(graph);
            var stepping = 0;

            foreach (var run in runs)
            {
                foreach (var tile in run)
                {
                    if (StepsUpFrom(graph.Tiles, tile))
                    {
                        stepping++;
                    }
                }
            }

            report.Append(string.Format(
                CultureInfo.InvariantCulture,
                "\n  the level climbs on {0} staircase tiles in {1} runs; {2} of them stand above a lower "
                + "neighbour, so {3} carry a flight that fills a drop nothing can see into",
                Climbing(graph).Count,
                runs.Count,
                stepping,
                Climbing(graph).Count - stepping));

            foreach (var run in runs)
            {
                report.Append("\n    a run of ").Append(run.Count).Append(" from ").Append(run[0])
                    .Append(" to ").Append(run[run.Count - 1]);
            }
        }

        static void Shoot(string name, Remedy remedy, StringBuilder report)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var graph = LevelGenerator.Generate(Seed, MazePreset.Ship).Graph;
            var builder = new WorldBuilder();
            var root = builder.Build(graph);
            var stage = new Stage(graph, root);

            remedy(stage);
            PreviewFilm.Sun();

            var run = Longest(graph);
            var lens = Rig(Centre(run), IsoProjection.OrthographicSize);
            Capture(lens, ShotFolder + name + "-play.png", ShotFolder + name + "-detail.png");
            WorldObjects.Destroy(lens.gameObject);

            var whole = LevelFraming.Whole(graph);
            var wide = Rig(whole.Target, whole.OrthographicSize);
            Capture(wide, ShotFolder + name + "-level.png", ShotFolder + name + "-level-detail.png");
            WorldObjects.Destroy(wide.gameObject);

            report.Append("\n  ").Append(name).Append(": ").Append(stage.Story())
                .Append("\n      ").Append(Crests(stage));

            WorldObjects.Destroy(root);
            builder.Dispose();
        }

        static string Crests(Stage stage)
        {
            var atTheHead = 0;
            var atTheFoot = 0;
            var flat = 0;
            var counted = 0;

            foreach (var tile in Climbing(stage.Graph))
            {
                var instance = stage.Find(PartNames.Stair(tile));
                if (instance == null)
                {
                    continue;
                }

                counted++;
                var along = TileSides.Toward(StaircaseClimb.AscentOf(stage.Graph.Tiles, tile));
                var ground = IsoProjection.Of(tile);
                var head = float.MinValue;
                var foot = float.MinValue;

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
                        var reach = (world.x - ground.X) * along.X + (world.z - ground.Z) * along.Z;

                        if (reach > 0f)
                        {
                            head = Math.Max(head, world.y);
                        }
                        else
                        {
                            foot = Math.Max(foot, world.y);
                        }
                    }
                }

                if (head > foot + 0.05f)
                {
                    atTheHead++;
                }
                else if (foot > head + 0.05f)
                {
                    atTheFoot++;
                }
                else
                {
                    flat++;
                }
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "of {0} staircase tiles, {1} carry a mass that crests at the head of the climb, {2} crest at "
                + "its foot and {3} crest level",
                counted,
                atTheHead,
                atTheFoot,
                flat);
        }

        static void Corrected(Stage stage)
        {
            foreach (var tile in Climbing(stage.Graph))
            {
                var ascent = StaircaseClimb.AscentOf(stage.Graph.Tiles, tile);
                stage.Replace(tile, Narrow, TileSides.Opposite(ascent));
            }

            stage.Told("every flight laid the other way round, so its crest stands at the head of its own climb "
                + "instead of at the foot");
        }

        static void CorrectedFootOnly(Stage stage)
        {
            foreach (var tile in Climbing(stage.Graph))
            {
                var ascent = StaircaseClimb.AscentOf(stage.Graph.Tiles, tile);

                if (StepsUpFrom(stage.Graph.Tiles, tile))
                {
                    stage.Replace(tile, Narrow, TileSides.Opposite(ascent));
                    continue;
                }

                stage.Replace(tile, Foundation, TileSide.North);
            }

            stage.Told("a corrected flight only where a staircase tile stands above a lower neighbour, a "
                + "foundation under the level walkway between them");
        }

        static void CorrectedBothEnds(Stage stage)
        {
            CorrectedFootOnly(stage);

            foreach (var tile in stage.Graph.Tiles.Tiles)
            {
                var position = tile.Position;
                if (StaircaseClimb.Climbs(position) || !StepsUpFrom(stage.Graph.Tiles, position))
                {
                    continue;
                }

                stage.Raise(position, Narrow, TileSides.Opposite(AscentInto(stage.Graph.Tiles, position)));
            }

            stage.Told("a corrected flight under every tile that stands one step above a neighbour, the upper "
                + "terrace's own near edge included, a foundation under the level walkway between them");
        }

        static void Across(Stage stage)
        {
            foreach (var tile in Climbing(stage.Graph))
            {
                var ascent = StaircaseClimb.AscentOf(stage.Graph.Tiles, tile);
                stage.Replace(tile, Narrow, Quarter(ascent));
            }

            stage.Told("every flight turned a quarter, so its treads read across the run");
        }

        static void PlinthAll(Stage stage)
        {
            foreach (var tile in Climbing(stage.Graph))
            {
                stage.Replace(tile, Foundation, TileSide.North);
            }

            stage.Told("no flight anywhere, a foundation under every staircase tile");
        }

        static void Walls(Stage stage)
        {
            foreach (var tile in Climbing(stage.Graph))
            {
                stage.Replace(tile, Walled, TileSides.Opposite(StaircaseClimb.AscentOf(stage.Graph.Tiles, tile)));
            }

            stage.Told("the pack's walled flight in place of the narrow one, crest at the head");
        }

        static void Widened(Stage stage)
        {
            foreach (var tile in Climbing(stage.Graph))
            {
                stage.Replace(tile, Wide, TileSides.Opposite(StaircaseClimb.AscentOf(stage.Graph.Tiles, tile)));
            }

            stage.Told("the pack's wide flight in place of the narrow one, crest at the head");
        }

        static TileSide Quarter(TileSide side)
        {
            switch (side)
            {
                case TileSide.North:
                    return TileSide.East;
                case TileSide.East:
                    return TileSide.South;
                case TileSide.South:
                    return TileSide.West;
                default:
                    return TileSide.North;
            }
        }

        static bool StepsUpFrom(TileGrid tiles, TilePosition position)
        {
            foreach (var neighbour in tiles.Neighbours(position))
            {
                if (neighbour.Elevation < position.Elevation)
                {
                    return true;
                }
            }

            return false;
        }

        static TileSide AscentInto(TileGrid tiles, TilePosition position)
        {
            foreach (var neighbour in tiles.Neighbours(position))
            {
                if (neighbour.Elevation < position.Elevation)
                {
                    return TileSides.Opposite(TileSides.Between(position, neighbour));
                }
            }

            return TileSide.North;
        }

        static List<TilePosition> Climbing(LevelGraph graph)
        {
            var climbing = new List<TilePosition>();

            foreach (var tile in graph.Tiles.Tiles)
            {
                if (StaircaseClimb.Climbs(tile.Position))
                {
                    climbing.Add(tile.Position);
                }
            }

            return climbing;
        }

        static List<List<TilePosition>> Runs(LevelGraph graph)
        {
            var runs = new List<List<TilePosition>>();
            var seen = new HashSet<TilePosition>();

            foreach (var start in Climbing(graph))
            {
                if (seen.Contains(start))
                {
                    continue;
                }

                var run = new List<TilePosition> { start };
                seen.Add(start);

                for (var index = 0; index < run.Count; index++)
                {
                    foreach (var neighbour in graph.Tiles.Neighbours(run[index]))
                    {
                        if (neighbour.Elevation == run[index].Elevation && seen.Add(neighbour))
                        {
                            run.Add(neighbour);
                        }
                    }
                }

                runs.Add(run);
            }

            return runs;
        }

        static List<TilePosition> Longest(LevelGraph graph)
        {
            var runs = Runs(graph);
            var longest = runs[0];

            foreach (var run in runs)
            {
                if (run.Count > longest.Count)
                {
                    longest = run;
                }
            }

            return longest;
        }

        static WorldPoint Centre(IReadOnlyList<TilePosition> run)
        {
            var x = 0f;
            var y = 0f;
            var z = 0f;

            foreach (var position in run)
            {
                var point = IsoProjection.Of(position);
                x += point.X;
                y += point.Y;
                z += point.Z;
            }

            return new WorldPoint(x / run.Count, y / run.Count, z / run.Count);
        }

        static Camera Rig(WorldPoint target, float orthographicSize)
        {
            var framing = new CameraFraming(target, orthographicSize);
            var camera = new GameObject("PrototypeCamera").AddComponent<Camera>();
            camera.transform.rotation = Quaternion.Euler(
                IsoProjection.CameraPitch, IsoProjection.CameraYaw, IsoProjection.CameraRoll);
            camera.transform.position = new Vector3(
                framing.Position.X, framing.Position.Y, framing.Position.Z);
            camera.orthographic = true;
            camera.orthographicSize = framing.OrthographicSize;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = IsoProjection.CameraBack * 6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.09f);

            return camera;
        }

        static void Capture(Camera camera, string fullPath, string cropPath)
        {
            var aspect = camera.aspect;
            camera.aspect = (float)ScreenFrame.Width / ScreenFrame.Height;

            var target = new RenderTexture(
                ScreenFrame.Width,
                ScreenFrame.Height,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            camera.targetTexture = target;

            var request = new RenderPipeline.StandardRequest { destination = target };
            if (RenderPipeline.SupportsRenderRequest(camera, request))
            {
                camera.SubmitRenderRequest(request);
            }
            else
            {
                camera.Render();
            }

            var previous = RenderTexture.active;
            RenderTexture.active = target;

            var full = new Texture2D(ScreenFrame.Width, ScreenFrame.Height, TextureFormat.RGB24, false);
            full.ReadPixels(new Rect(0f, 0f, ScreenFrame.Width, ScreenFrame.Height), 0, 0);
            full.Apply();

            var crop = new Texture2D(CropEdge, CropEdge, TextureFormat.RGB24, false);
            crop.ReadPixels(
                new Rect(
                    (ScreenFrame.Width - CropEdge) * 0.5f,
                    (ScreenFrame.Height - CropEdge) * 0.5f,
                    CropEdge,
                    CropEdge),
                0,
                0);
            crop.Apply();

            RenderTexture.active = previous;

            Write(fullPath, full.EncodeToPNG());
            Write(cropPath, crop.EncodeToPNG());

            camera.targetTexture = null;
            camera.aspect = aspect;
            UnityEngine.Object.DestroyImmediate(full);
            UnityEngine.Object.DestroyImmediate(crop);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }

        static void Write(string path, byte[] bytes)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, bytes);
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

        sealed class Stage
        {
            readonly Dictionary<string, Transform> byName = new Dictionary<string, Transform>();

            string story = "as it ships today";

            public Stage(LevelGraph graph, GameObject root)
            {
                Graph = graph;
                Root = root;

                foreach (var node in root.GetComponentsInChildren<Transform>(true))
                {
                    byName[node.name] = node;
                }
            }

            public LevelGraph Graph { get; }

            public GameObject Root { get; }

            public void Told(string what)
            {
                story = what;
            }

            public string Story()
            {
                return story;
            }

            public Transform Find(string name)
            {
                Transform standing;
                return byName.TryGetValue(name, out standing) ? standing : null;
            }

            public void Replace(TilePosition position, string asset, TileSide ascent)
            {
                Transform standing;
                if (!byName.TryGetValue(PartNames.Stair(position), out standing))
                {
                    return;
                }

                var parent = standing.parent;
                var material = Material(standing);
                WorldObjects.Destroy(standing.gameObject);
                byName.Remove(PartNames.Stair(position));
                Fit(position, asset, ascent, parent, material);
            }

            public void Raise(TilePosition position, string asset, TileSide ascent)
            {
                Transform floor;
                if (!byName.TryGetValue(PartNames.Tile(position), out floor))
                {
                    return;
                }

                Fit(position, asset, ascent, floor.parent, Material(floor));
            }

            void Fit(TilePosition position, string asset, TileSide ascent, Transform parent, Material material)
            {
                var prefab = Resources.Load<GameObject>(WorldModels.ResourcesFolder + "/" + asset);
                if (prefab == null)
                {
                    return;
                }

                var instance = UnityEngine.Object.Instantiate(prefab);
                instance.name = PartNames.Stair(position);
                instance.transform.SetParent(parent, worldPositionStays: false);
                instance.transform.localScale = Vector3.one;
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localEulerAngles = new Vector3(0f, TileSides.InwardYaw(ascent), 0f);

                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterial = material;
                }

                WorldObjects.Destroy(instance.GetComponent<Collider>());

                var ground = IsoProjection.Of(position);
                var edge = IsoProjection.TileEdge;
                var step = IsoProjection.StepHeight;
                var wanted = new Bounds(
                    new Vector3(ground.X, ground.Y - step * 0.5f, ground.Z),
                    new Vector3(edge, step, edge));

                var box = World(instance.transform);
                instance.transform.localScale = new Vector3(
                    box.size.x <= 0f ? 1f : wanted.size.x / box.size.x,
                    box.size.y <= 0f ? 1f : wanted.size.y / box.size.y,
                    box.size.z <= 0f ? 1f : wanted.size.z / box.size.z);

                box = World(instance.transform);
                instance.transform.position += wanted.center - box.center;

                byName[instance.name] = instance.transform;
            }

            static Material Material(Transform instance)
            {
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.sharedMaterial != null)
                    {
                        return renderer.sharedMaterial;
                    }
                }

                return null;
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
        }
    }
}
