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
    public static class DungeonDressCheckCommand
    {
        const long Seed = 20250824L;

        const string BaseMap = "_BaseMap";

        const float Epsilon = DungeonPack.BoundsEpsilon;

        const float AngleEpsilon = 0.01f;

        const float SilhouetteRatio = 1.5f;

        const int Complaints = 6;

        public static void Check()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var report = new StringBuilder("t-22 walls and props, ship seed ")
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

            report.Append("\n  t-22: ")
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
                Math.Abs(panel.size.x - edge) <= Epsilon
                && Math.Abs(panel.size.y - IsoProjection.WallHeight) <= Epsilon,
                "the imported wall panel spans one tile edge by one wall height",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "it measures {0:0.#####} wide by {1:0.#####} tall against {2:0.#####} by {3:0.#####}",
                    panel.size.x,
                    panel.size.y,
                    edge,
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

            report.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  a solid panel of one wall height hides {0:0.###} tile edges of ground behind it at "
                + "the camera's {1:0.#} degree pitch, so the tile behind a near-side wall is {2}",
                IsoProjection.SightReach(IsoProjection.WallHeight) / edge,
                IsoProjection.CameraPitch,
                IsoProjection.SightReach(IsoProjection.WallHeight) >= edge
                    ? "wholly out of sight"
                    : "partly in sight");

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
            var candles = Footprint(graph, byName, PartModel.Candles);

            if (chest.size == Vector3.zero || candles.size == Vector3.zero)
            {
                return Assert(
                    report,
                    false,
                    "the two reward kinds read apart by silhouette",
                    "the world raised no pair to compare");
            }

            var tall = Math.Abs(chest.size.y - candles.size.y) <= Epsilon;
            var wide = chest.size.x >= candles.size.x * SilhouetteRatio;

            return Assert(
                report,
                tall && wide,
                "the two reward kinds stand the same height and read apart by width alone",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "the chest is {0:0.###} wide and {1:0.###} tall against the candle group's "
                    + "{2:0.###} by {3:0.###}, a width ratio of {4:0.##} against the {5:0.##} asked for",
                    chest.size.x,
                    chest.size.y,
                    candles.size.x,
                    candles.size.y,
                    candles.size.x <= 0f ? 0f : chest.size.x / candles.size.x,
                    SilhouetteRatio));
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
                    return World(instance);
                }
            }

            return new Bounds(Vector3.zero, Vector3.zero);
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
