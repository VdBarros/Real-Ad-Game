using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Domain;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTooling
{
    public static class DungeonDressCheckCommand
    {
        const long Seed = 20250824L;

        const string BaseMap = "_BaseMap";

        const float Epsilon = DungeonPack.BoundsEpsilon;

        const float AngleEpsilon = 0.01f;

        const float SilhouetteRatio = 1.5f;

        const float OcclusionBound = 0.5f;

        const float FootprintShare = 0.3f;

        const float ToneContrast = 1.6f;

        const float ShotDistance = 60f;

        const int ShotThreshold = 8;

        const string AdditiveShot = "dev/scratch/t-30-additive.png";

        const string MultiplierShot = "dev/scratch/t-30-multiplier.png";

        const string FloorShot = "dev/scratch/t-30-floor-only.png";

        const int Complaints = 6;

        public static void Check()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var report = new StringBuilder("t-22/t-30 walls and props, ship seed ")
                .Append(Seed.ToString(CultureInfo.InvariantCulture))
                .Append(':');
            var failures = 0;

            using (var models = new WorldModels())
            {
                failures += NothingFallsBack(models, report);
                failures += Imported(models, report);
                failures += Measured(models, report);
                failures += Built(models, report);
            }

            report.Append("\n  t-22/t-30: ")
                .Append(failures == 0
                    ? "every assertion above held"
                    : failures + (failures == 1 ? " assertion" : " assertions") + " above failed");

            Debug.Log(report.ToString());

            if (failures > 0)
            {
                Debug.LogError(
                    "The dungeon dress check failed " + failures + " assertions. Read the report above.");
            }
        }

        static int NothingFallsBack(WorldModels models, StringBuilder report)
        {
            var failures = 0;
            var missing = new List<string>();
            var named = new List<string>();

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (model == PartModel.None)
                {
                    continue;
                }

                var path = WorldModels.AssetPathOf(model);
                var prefab = models.Of(model);
                named.Add(model + " to " + (path ?? "nothing") + " = "
                    + (prefab == null ? "NOTHING" : prefab.name));

                if (prefab == null)
                {
                    missing.Add(model.ToString());
                }
            }

            failures += Assert(
                report,
                missing.Count == 0,
                "every part model the table names resolves to a loadable mesh",
                string.Join("; ", named.ToArray()));

            var falling = new List<string>();
            var dressed = new List<string>();

            foreach (PartStyle style in Enum.GetValues(typeof(PartStyle)))
            {
                if (PartModels.Of(style) == PartModel.None)
                {
                    continue;
                }

                if (models.Dresses(style))
                {
                    dressed.Add(style.ToString());
                }
                else
                {
                    falling.Add(style.ToString());
                }
            }

            failures += Assert(
                report,
                falling.Count == 0,
                "no part style that wants a mesh falls back to its primitive",
                dressed.Count + " dressed: " + string.Join(", ", dressed.ToArray())
                + (falling.Count == 0 ? "" : "; falling back: " + string.Join(", ", falling.ToArray())));

            var primitive = new List<string>();

            foreach (PartStyle style in Enum.GetValues(typeof(PartStyle)))
            {
                if (PartModels.Of(style) == PartModel.None)
                {
                    primitive.Add(style.ToString());
                }
            }

            report.Append("\n  by design still primitive: ").Append(string.Join(", ", primitive.ToArray()));

            return failures;
        }

        static int Imported(WorldModels models, StringBuilder report)
        {
            var failures = 0;

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (model == PartModel.None)
                {
                    continue;
                }

                var path = "Assets/Resources/" + WorldModels.AssetPathOf(model) + ".fbx";
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;

                if (importer == null)
                {
                    failures += Assert(
                        report, false, "the " + model + " mesh has a model importer", path + " has none");
                    continue;
                }

                var settled = importer.materialImportMode == ModelImporterMaterialImportMode.None
                    && !importer.importAnimation
                    && importer.animationType == ModelImporterAnimationType.None
                    && !importer.importCameras
                    && !importer.importLights
                    && !importer.importBlendShapes
                    && !importer.isReadable
                    && importer.optimizeMeshVertices
                    && importer.useFileScale
                    && Math.Abs(importer.globalScale - DungeonPack.ImportScale) < 1e-6f;

                failures += Assert(
                    report,
                    settled,
                    "the postprocessor settled the " + model + " import from code",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "materials {0}, animation {1}, blend shapes {2}, readable {3}, "
                        + "optimised {4}, scale {5:0.####}, compression {6}",
                        importer.materialImportMode,
                        importer.importAnimation,
                        importer.importBlendShapes,
                        importer.isReadable,
                        importer.optimizeMeshVertices,
                        importer.globalScale,
                        importer.meshCompression));
            }

            return failures;
        }

        static int Measured(WorldModels models, StringBuilder report)
        {
            var failures = 0;

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (model == PartModel.None)
                {
                    continue;
                }

                var prefab = models.Of(model);
                if (prefab == null)
                {
                    failures += Assert(
                        report,
                        false,
                        "the " + model + " mesh matches its pinned pack height",
                        "there is no mesh to measure");
                    continue;
                }

                var box = Local(prefab);
                var wanted = DungeonPack.HeightOf(model);

                failures += Assert(
                    report,
                    Math.Abs(box.size.y - wanted) <= Epsilon,
                    "the imported " + model + " mesh stands the pinned "
                    + DungeonPack.PackHeightOf(model).ToString("0.####", CultureInfo.InvariantCulture)
                    + " pack units tall",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "it measures {0:0.#####} against {1:0.#####}, spanning {2:0.#####} by {3:0.#####} "
                        + "on the ground, its bounds centred on ({4:0.#####}, {5:0.#####}) with a base "
                        + "{6:0.#####} from the pivot",
                        box.size.y,
                        wanted,
                        box.size.x,
                        box.size.z,
                        box.center.x,
                        box.center.z,
                        box.min.y));
            }

            var wall = models.Of(PartModel.WallPanel);

            if (wall == null)
            {
                return failures + Assert(
                    report, false, "the wall panel sits on a tile edge", "there is no mesh to measure");
            }

            var panel = Local(wall);
            var edge = IsoProjection.TileEdge;

            failures += Assert(
                report,
                Math.Abs(panel.size.x - DungeonPack.WallPanelWidth) <= Epsilon
                && Math.Abs(DungeonPack.WallPanelWidth - edge) <= Epsilon,
                "the imported wall panel spans one tile edge at the pinned width, so it needs no stretching",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "it measures {0:0.#####} wide against the pinned {1:0.#####} and a tile edge of {2:0.#####}",
                    panel.size.x,
                    DungeonPack.WallPanelWidth,
                    edge));

            failures += Assert(
                report,
                panel.size.y < IsoProjection.WallHeight,
                "the imported wall panel is a parapet rather than a full-height wall",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "it stands {0:0.#####} tall against one wall height of {1:0.#####}",
                    panel.size.y,
                    IsoProjection.WallHeight));

            failures += Assert(
                report,
                panel.size.z < edge * 0.5f,
                "the imported wall panel is a panel rather than a block",
                string.Format(CultureInfo.InvariantCulture, "it is {0:0.#####} deep", panel.size.z));

            failures += Assert(
                report,
                Math.Abs(panel.center.x) <= Epsilon
                && Math.Abs(panel.center.z) <= Epsilon
                && Math.Abs(panel.min.y) <= Epsilon,
                "the wall panel pivots on the centre of its own base, so a tile-edge midpoint places it",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "its bounds sit at ({0:0.#####}, {1:0.#####}, {2:0.#####}) with a base at {3:0.#####}",
                    panel.center.x,
                    panel.center.y,
                    panel.center.z,
                    panel.min.y));

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

            failures += EveryPartWearsItsMesh(models, graph, byName, report);
            failures += EveryWallLandsOnItsTileEdge(graph, byName, report);
            failures += EveryPropStandsOnItsTile(graph, byName, report);
            failures += TheRewardsReadApart(graph, byName, report);
            failures += TheRewardsReadAgainstTheFloor(root, graph, byName, report);
            failures += TheMaterialsStayFlat(root, report);

            WorldObjects.Destroy(root);
            builder.Dispose();

            return failures;
        }

        static int EveryPartWearsItsMesh(
            WorldModels models,
            LevelGraph graph,
            IDictionary<string, Transform> byName,
            StringBuilder report)
        {
            var expected = new Dictionary<PartModel, HashSet<Mesh>>();

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (model == PartModel.None)
                {
                    continue;
                }

                var prefab = models.Of(model);
                var meshes = new HashSet<Mesh>();

                if (prefab != null)
                {
                    foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (filter.sharedMesh != null)
                        {
                            meshes.Add(filter.sharedMesh);
                        }
                    }
                }

                expected[model] = meshes;
            }

            var wanted = 0;
            var meshed = 0;
            var fellBack = new List<string>();

            foreach (var part in LevelBlueprintBuilder.Build(graph).AllParts)
            {
                if (part.Model == PartModel.None)
                {
                    continue;
                }

                wanted++;

                Transform instance;
                if (!byName.TryGetValue(part.Name, out instance))
                {
                    fellBack.Add(part.Name + " is not in the world");
                    continue;
                }

                var filters = instance.GetComponentsInChildren<MeshFilter>(true);
                var dressed = filters.Length > 0;

                foreach (var filter in filters)
                {
                    if (filter.sharedMesh == null || !expected[part.Model].Contains(filter.sharedMesh))
                    {
                        dressed = false;
                    }
                }

                if (dressed)
                {
                    meshed++;
                }
                else
                {
                    fellBack.Add(part.Name + " wearing "
                        + (filters.Length == 0 || filters[0].sharedMesh == null
                            ? "nothing"
                            : filters[0].sharedMesh.name));
                }
            }

            var failures = Assert(
                report,
                wanted > 0 && meshed == wanted,
                "no part fell back to a primitive while building a real level graph",
                meshed + " of " + wanted + " parts that want a mesh wear one"
                + (fellBack.Count == 0
                    ? ""
                    : "; fell back: " + string.Join(", ", fellBack.GetRange(0, Math.Min(Complaints, fellBack.Count)).ToArray())));

            var bare = 0;
            var total = 0;

            foreach (var part in LevelBlueprintBuilder.Build(graph).AllParts)
            {
                Transform instance;
                if (part.Model == PartModel.None || !byName.TryGetValue(part.Name, out instance))
                {
                    continue;
                }

                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    total++;
                    var material = renderer.sharedMaterial;

                    if (material == null
                        || !material.name.StartsWith(WorldMaterials.NamePrefix, StringComparison.Ordinal))
                    {
                        bare++;
                    }
                }
            }

            failures += Assert(
                report,
                total > 0 && bare == 0,
                "every mesh of a dressed part wears a world material, a prop's second mesh included",
                (total - bare) + " of " + total + " renderers do");

            return failures;
        }

        static int EveryWallLandsOnItsTileEdge(
            LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            var walls = 0;
            var placed = 0;
            var turned = 0;
            var spanning = 0;
            var standing = 0f;
            var edge = IsoProjection.TileEdge;
            var complaint = new List<string>();

            foreach (var tile in graph.Tiles.Tiles)
            {
                var here = IsoProjection.Of(tile.Position);

                foreach (var side in TileSides.All)
                {
                    var beyond = TileSides.Step(tile.Position, side);
                    if (graph.Tiles.ContainsPlace(beyond.X, beyond.Y))
                    {
                        continue;
                    }

                    var name = PartNames.Wall(tile.Position, side);
                    Transform instance;

                    if (!byName.TryGetValue(name, out instance))
                    {
                        complaint.Add(name + " is missing");
                        continue;
                    }

                    walls++;

                    var there = IsoProjection.Of(beyond);
                    var wanted = new Vector3(
                        (here.X + there.X) * 0.5f, here.Y, (here.Z + there.Z) * 0.5f);
                    var at = instance.position;

                    if ((at - wanted).magnitude <= Epsilon)
                    {
                        placed++;
                    }
                    else if (complaint.Count < Complaints)
                    {
                        complaint.Add(name + " stands at " + at + " rather than " + wanted);
                    }

                    var yaw = Mathf.DeltaAngle(instance.eulerAngles.y, TileSides.InwardYaw(side));

                    if (Math.Abs(yaw) <= AngleEpsilon)
                    {
                        turned++;
                    }
                    else if (complaint.Count < Complaints)
                    {
                        complaint.Add(name + " faces " + instance.eulerAngles.y + " rather than "
                            + TileSides.InwardYaw(side));
                    }

                    var along = World(instance);
                    var across = side == TileSide.North || side == TileSide.South
                        ? along.size.x
                        : along.size.z;

                    standing = Math.Max(standing, along.size.y);

                    if (Math.Abs(across - edge) <= Epsilon)
                    {
                        spanning++;
                    }
                    else if (complaint.Count < Complaints)
                    {
                        complaint.Add(name + " covers " + across.ToString("0.#####", CultureInfo.InvariantCulture)
                            + " of its edge");
                    }
                }
            }

            var failures = 0;

            failures += Assert(
                report,
                walls > 0 && placed == walls,
                "every wall stands on the midpoint of the tile side that faces outside the grid",
                placed + " of " + walls + " do");
            failures += Assert(
                report,
                walls > 0 && turned == walls,
                "every wall faces inwards at the yaw its side asks for",
                turned + " of " + walls + " do");
            failures += Assert(
                report,
                walls > 0 && spanning == walls,
                "every wall covers its whole tile edge, so neighbouring panels meet with no gap",
                spanning + " of " + walls + " do"
                + (complaint.Count == 0 ? "" : "; " + string.Join("; ", complaint.ToArray())));

            var hidden = IsoProjection.SightReach(standing);

            failures += Assert(
                report,
                walls > 0 && hidden <= edge * OcclusionBound,
                "a wall hides at most " + OcclusionBound.ToString("0.##", CultureInfo.InvariantCulture)
                + " of the tile edge behind it, so a near-side corridor floor stays readable",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "a wall standing {0:0.#####} tall hides {1:0.###} tile edges of ground at the camera's "
                    + "{2:0.#} degree pitch, against the bound of {3:0.###} and the {4:0.###} that a solid "
                    + "panel of one wall height hid",
                    standing,
                    hidden / edge,
                    IsoProjection.CameraPitch,
                    OcclusionBound,
                    IsoProjection.SightReach(IsoProjection.WallHeight) / edge));

            return failures;
        }

        static int EveryPropStandsOnItsTile(
            LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            var props = 0;
            var standing = 0;
            var sizes = new Dictionary<PartModel, string>();

            foreach (var node in graph.Decisions.Nodes)
            {
                WorldPart prop;
                if (!LevelBlueprintBuilder.TryProp(node, out prop) || prop.Model == PartModel.None)
                {
                    continue;
                }

                Transform instance;
                if (!byName.TryGetValue(prop.Name, out instance))
                {
                    continue;
                }

                props++;
                var box = World(instance);
                var top = IsoProjection.Of(node.Position).Y;

                if (Math.Abs(box.min.y - top) <= Epsilon)
                {
                    standing++;
                }

                if (!sizes.ContainsKey(prop.Model))
                {
                    sizes.Add(prop.Model, Silhouette(prop.Model, box));
                }
            }

            var shown = new List<string>(sizes.Values);

            report.Append("\n  silhouettes: ").Append(string.Join(", ", shown.ToArray()));

            return Assert(
                report,
                props > 0 && standing == props,
                "every content prop rests on its tile rather than floating at the cube's centre",
                standing + " of " + props + " do");
        }

        static int TheRewardsReadApart(
            LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            var chest = Footprint(graph, byName, PartModel.Chest);
            var hoard = Footprint(graph, byName, PartModels.Of(PartStyle.Multiplier));

            if (chest.size == Vector3.zero || hoard.size == Vector3.zero)
            {
                return Assert(
                    report,
                    false,
                    "the two reward kinds read apart by silhouette",
                    "the world raised no pair to compare");
            }

            var chestGround = chest.size.x * chest.size.z;
            var hoardGround = hoard.size.x * hoard.size.z;
            var tall = Math.Abs(chest.size.y - hoard.size.y) <= Epsilon;
            var apart = Math.Min(chestGround, hoardGround) > 0f
                && Math.Max(chestGround, hoardGround)
                >= Math.Min(chestGround, hoardGround) * SilhouetteRatio;
            var failures = 0;

            failures += Assert(
                report,
                tall && apart,
                "the two reward kinds stand the same height and cover different ground, so a scan tells "
                + "an additive from a multiplier by shape",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "the chest is {0:0.###} by {1:0.###} on the ground and {2:0.###} tall against the "
                    + "multiplier prop's {3:0.###} by {4:0.###} by {5:0.###}, a ground ratio of {6:0.##} "
                    + "against the {7:0.##} asked for",
                    chest.size.x,
                    chest.size.z,
                    chest.size.y,
                    hoard.size.x,
                    hoard.size.z,
                    hoard.size.y,
                    Math.Min(chestGround, hoardGround) <= 0f
                        ? 0f
                        : Math.Max(chestGround, hoardGround) / Math.Min(chestGround, hoardGround),
                    SilhouetteRatio));

            var edge = IsoProjection.TileEdge;
            var widest = Math.Max(
                Math.Max(chest.size.x, chest.size.z), Math.Max(hoard.size.x, hoard.size.z));

            failures += Assert(
                report,
                widest <= edge,
                "neither reward prop spills off the tile it stands on once the fit-to-cube rule has "
                + "stretched it",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "the wider of the two spans {0:0.###} of a {1:0.###} tile edge",
                    widest,
                    edge));

            return failures;
        }

        static int TheRewardsReadAgainstTheFloor(
            GameObject root, LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            PreviewFilm.Sun();
            Unbadge(root);

            var multiplier = Photograph(
                graph, byName, PartModels.Of(PartStyle.Multiplier), "multiplier", MultiplierShot, FloorShot);
            var additive = Photograph(
                graph, byName, PartModels.Of(PartStyle.Additive), "additive", AdditiveShot, null);

            if (multiplier.Pixels < 0 || additive.Pixels < 0)
            {
                return Assert(
                    report,
                    false,
                    "both reward props stand somewhere the gameplay camera can photograph them",
                    "the ship seed raised no pair to photograph");
            }

            var tile = ScreenFrame.TileGroundPixels(IsoProjection.OrthographicSize);
            var failures = 0;

            report.Append("\n  at gameplay zoom ")
                .Append(IsoProjection.OrthographicSize.ToString("0.#", CultureInfo.InvariantCulture))
                .Append(" one tile of ground covers ")
                .Append(tile.ToString("0", CultureInfo.InvariantCulture))
                .Append(" of the ")
                .Append(ScreenFrame.Width * ScreenFrame.Height)
                .Append(" pixels in frame");

            failures += ReadsAgainstTheFloor(report, multiplier, tile);
            failures += ReadsAgainstTheFloor(report, additive, tile);

            return failures;
        }

        static int ReadsAgainstTheFloor(StringBuilder report, Reading reading, float tile)
        {
            var failures = 0;
            var share = tile <= 0f ? 0f : reading.Pixels / tile;

            failures += Assert(
                report,
                share >= FootprintShare,
                "the " + reading.Name + " prop covers at least "
                + FootprintShare.ToString("0.##", CultureInfo.InvariantCulture)
                + " of a tile of ground at gameplay zoom, so it is a shape rather than a speck",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "it covers {0} pixels, {1:0.###} of a tile",
                    reading.Pixels,
                    share));

            failures += Assert(
                report,
                reading.Contrast >= ToneContrast,
                "the " + reading.Name + " prop reads at least "
                + ToneContrast.ToString("0.##", CultureInfo.InvariantCulture)
                + ":1 in tone against the floor it stands on, so it is not dark on dark",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "its lit pixels average {0:0.####} relative luminance against the {1:0.####} of the ground "
                    + "they hide, a contrast of {2:0.##}:1, and {3:0.####} across the rest of the frame "
                    + "at {4:0.##}:1",
                    reading.Prop,
                    reading.Behind,
                    reading.Contrast,
                    reading.Around,
                    ContrastOf(reading.Prop, reading.Around)));

            return failures;
        }

        static Reading Photograph(
            LevelGraph graph,
            IDictionary<string, Transform> byName,
            PartModel model,
            string name,
            string path,
            string barePath)
        {
            var reading = new Reading { Name = name, Pixels = -1 };
            var instance = PropOf(graph, byName, model);

            if (instance == null)
            {
                return reading;
            }

            var camera = Rig(instance.position);
            UnityEngine.Object.DestroyImmediate(PreviewFilm.Frame(camera));

            instance.gameObject.SetActive(false);
            var bare = PreviewFilm.Frame(camera);
            instance.gameObject.SetActive(true);
            var dressed = PreviewFilm.Frame(camera);

            var before = bare.GetPixels32();
            var after = dressed.GetPixels32();
            var covered = 0;
            var prop = 0.0;
            var behind = 0.0;
            var elsewhere = 0.0;

            for (var pixel = 0; pixel < after.Length; pixel++)
            {
                if (Apart(before[pixel], after[pixel]))
                {
                    covered++;
                    prop += Luminance(after[pixel]);
                    behind += Luminance(before[pixel]);
                }
                else
                {
                    elsewhere += Luminance(before[pixel]);
                }
            }

            reading.Pixels = covered;
            reading.Prop = covered == 0 ? 0.0 : prop / covered;
            reading.Behind = covered == 0 ? 0.0 : behind / covered;
            reading.Around = after.Length == covered ? 0.0 : elsewhere / (after.Length - covered);

            Write(dressed, path);
            Write(bare, barePath);

            UnityEngine.Object.DestroyImmediate(bare);
            UnityEngine.Object.DestroyImmediate(dressed);
            UnityEngine.Object.DestroyImmediate(camera.gameObject);

            return reading;
        }

        static bool Apart(Color32 before, Color32 after)
        {
            return Math.Abs(before.r - after.r) > ShotThreshold
                || Math.Abs(before.g - after.g) > ShotThreshold
                || Math.Abs(before.b - after.b) > ShotThreshold;
        }

        static double Luminance(Color32 colour)
        {
            return 0.2126 * Linear(colour.r) + 0.7152 * Linear(colour.g) + 0.0722 * Linear(colour.b);
        }

        static double Linear(byte channel)
        {
            var value = channel / 255.0;

            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        static double ContrastOf(double one, double other)
        {
            return (Math.Max(one, other) + 0.05) / (Math.Min(one, other) + 0.05);
        }

        static void Write(Texture2D frame, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, frame.EncodeToPNG());
        }

        static void Unbadge(GameObject root)
        {
            foreach (var node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == PartNames.BadgesGroup)
                {
                    node.gameObject.SetActive(false);
                }
            }
        }

        static Camera Rig(Vector3 centre)
        {
            var camera = new GameObject("DressCamera").AddComponent<Camera>();
            camera.transform.rotation = Quaternion.Euler(
                IsoProjection.CameraPitch, IsoProjection.CameraYaw, IsoProjection.CameraRoll);
            camera.transform.position = centre - camera.transform.forward * ShotDistance;
            camera.orthographic = true;
            camera.orthographicSize = IsoProjection.OrthographicSize;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = ShotDistance * 3f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.09f);

            return camera;
        }

        struct Reading
        {
            public string Name;

            public int Pixels;

            public double Prop;

            public double Behind;

            public double Around;

            public double Contrast
            {
                get { return ContrastOf(Prop, Behind); }
            }
        }

        static int TheMaterialsStayFlat(GameObject root, StringBuilder report)
        {
            var world = new List<Material>();
            var atlassed = 0;

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

            foreach (var material in world)
            {
                if (material.HasProperty(BaseMap) && material.GetTexture(BaseMap) != null)
                {
                    atlassed++;
                }
            }

            var dressed = 0;

            foreach (PartStyle style in Enum.GetValues(typeof(PartStyle)))
            {
                if (PartModels.Of(style) != PartModel.None)
                {
                    dressed++;
                }
            }

            var ceiling = Enum.GetValues(typeof(PartStyle)).Length;
            var failures = 0;

            failures += Assert(
                report,
                world.Count <= ceiling,
                "the built world's material count is within its ceiling of " + ceiling,
                world.Count + " distinct world materials for "
                + root.GetComponentsInChildren<Renderer>(true).Length + " renderers");

            failures += Assert(
                report,
                atlassed == dressed,
                "every dressed style binds the one pack atlas, so the whole dungeon is one texture",
                atlassed + " of " + world.Count + " world materials do against " + dressed
                + " styles that want a mesh");

            return failures;
        }

        static string Silhouette(PartModel model, Bounds box)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} is {1:0.###} by {2:0.###} by {3:0.###}",
                model,
                box.size.x,
                box.size.y,
                box.size.z);
        }

        static Bounds Footprint(
            LevelGraph graph, IDictionary<string, Transform> byName, PartModel model)
        {
            var instance = PropOf(graph, byName, model);

            return instance == null ? new Bounds(Vector3.zero, Vector3.zero) : World(instance);
        }

        static Transform PropOf(
            LevelGraph graph, IDictionary<string, Transform> byName, PartModel model)
        {
            foreach (var node in graph.Decisions.Nodes)
            {
                WorldPart prop;
                if (!LevelBlueprintBuilder.TryProp(node, out prop) || prop.Model != model)
                {
                    continue;
                }

                Transform instance;
                if (byName.TryGetValue(prop.Name, out instance))
                {
                    return instance;
                }
            }

            return null;
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
