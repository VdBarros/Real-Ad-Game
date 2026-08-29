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

        const string BaseColour = "_BaseColor";

        const float Epsilon = DungeonPack.BoundsEpsilon;

        const float AngleEpsilon = 0.01f;

        const float SilhouetteRatio = 1.5f;

        const float OcclusionBound = IsoProjection.OcclusionBound;

        const float FootprintShare = 0.3f;

        const float ToneContrast = 1.5f;

        const float ShotDistance = 60f;

        const int ChannelTolerance = 8;

        const int AloneLayer = 31;

        const int Cursed = 0;

        const int Cleared = 1;

        const int Rounds = 2;

        const string AdditiveShot = "dev/scratch/t-30-additive.png";

        const string MultiplierShot = "dev/scratch/t-30-multiplier.png";

        const string ClearedShot = "dev/scratch/t-30-multiplier-cleared.png";

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

                var rigged = ArtPacks.IsRigged(model);
                var settled = importer.materialImportMode == ModelImporterMaterialImportMode.None
                    && importer.importAnimation == rigged
                    && importer.animationType == (rigged
                        ? CharacterArtPostprocessor.Rig
                        : ModelImporterAnimationType.None)
                    && !importer.importCameras
                    && !importer.importLights
                    && !importer.importBlendShapes
                    && !importer.isReadable
                    && importer.optimizeMeshVertices
                    && importer.useFileScale
                    && Math.Abs(importer.globalScale - ArtPacks.ImportScaleFor(model)) < 1e-6f
                    && (!rigged
                        || (importer.skinWeights == CharacterArtPostprocessor.SkinWeights
                            && importer.meshCompression == CharacterArtPostprocessor.Compression
                            && !importer.weldVertices
                            && !importer.optimizeGameObjects));

                failures += Assert(
                    report,
                    settled,
                    "the postprocessor settled the " + model + " import from code",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "materials {0}, animation {1}, rig {2}, blend shapes {3}, readable {4}, "
                        + "optimised {5}, welded {6}, scale {7:0.####}, compression {8}, skin weights {9}",
                        importer.materialImportMode,
                        importer.importAnimation,
                        importer.animationType,
                        importer.importBlendShapes,
                        importer.isReadable,
                        importer.optimizeMeshVertices,
                        importer.weldVertices,
                        importer.globalScale,
                        importer.meshCompression,
                        importer.skinWeights));
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
                var wanted = ArtPacks.HeightOf(model);

                failures += Assert(
                    report,
                    Math.Abs(box.size.y - wanted) <= Epsilon,
                    "the imported " + model + " mesh stands the pinned "
                    + ArtPacks.PackHeightOf(model).ToString("0.####", CultureInfo.InvariantCulture)
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
            var expected = new Dictionary<PartModel, ISet<Mesh>>();

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (model == PartModel.None)
                {
                    continue;
                }

                var prefab = models.Of(model);

                expected[model] = PackMesh.Of(prefab);
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

                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                var dressed = renderers.Length > 0;
                Mesh worn = null;

                foreach (var renderer in renderers)
                {
                    var mesh = PackMesh.On(renderer);
                    worn = worn ?? mesh;

                    if (mesh == null || !expected[part.Model].Contains(mesh))
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
                    fellBack.Add(part.Name + " wearing " + (worn == null ? "nothing" : worn.name));
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
            var cast = new List<string>();

            foreach (var node in graph.Decisions.Nodes)
            {
                WorldPart prop;
                if (!LevelBlueprintBuilder.TryProp(node, out prop) || prop.Model == PartModel.None)
                {
                    continue;
                }

                if (CharacterCast.IsRole(prop.Style))
                {
                    if (!cast.Contains(prop.Model.ToString()))
                    {
                        cast.Add(prop.Model.ToString());
                    }

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
            report.Append("\n  footed by the player tier check instead of here, because a rigged mesh's "
                + "bounds are padded below its feet: ")
                .Append(cast.Count == 0 ? "no cast mesh in this world" : string.Join(", ", cast.ToArray()));

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
            var taller = hoard.size.y >= chest.size.y * SilhouetteRatio;
            var standing = chestGround > 0f && hoardGround > 0f;
            var failures = 0;

            failures += Assert(
                report,
                taller && standing,
                "the two reward kinds are different physical things, so a scan tells an additive from a "
                + "multiplier by shape: the gain is a low box on the tile, the gate an arch over it",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "the chest is {0:0.###} by {1:0.###} on the ground and {2:0.###} tall against the "
                    + "gate arch's {3:0.###} by {4:0.###} by {5:0.###}, a ground ratio of {6:0.##} and a "
                    + "height ratio of {7:0.##} against the {8:0.##} asked for",
                    chest.size.x,
                    chest.size.z,
                    chest.size.y,
                    hoard.size.x,
                    hoard.size.z,
                    hoard.size.y,
                    Math.Min(chestGround, hoardGround) <= 0f
                        ? 0f
                        : Math.Max(chestGround, hoardGround) / Math.Min(chestGround, hoardGround),
                    chest.size.y <= 0f ? 0f : hoard.size.y / chest.size.y,
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
            var badges = Unbadge(root);
            Unmark(root);

            var multiplier = new Reading[Rounds];
            var additive = new Reading[Rounds];

            multiplier[Cursed] = Photograph(
                graph, byName, PartStyle.Multiplier, "multiplier", MultiplierShot, FloorShot);
            additive[Cursed] = Photograph(graph, byName, PartStyle.Additive, "additive", AdditiveShot, null);

            Material cursed;
            var repainted = Repaint(root, out cursed);

            multiplier[Cleared] = Photograph(
                graph, byName, PartStyle.Multiplier, "multiplier", ClearedShot, null);
            additive[Cleared] = Photograph(graph, byName, PartStyle.Additive, "additive", null, null);

            foreach (var renderer in repainted)
            {
                renderer.sharedMaterial = cursed;
            }

            foreach (var group in badges)
            {
                group.SetActive(true);
            }

            var failures = Assert(
                report,
                repainted.Count > 0,
                "the world offers a cleared floor to photograph a prop against as well as a cursed one",
                repainted.Count + " floor renderers were repainted cleared for a second round");

            if (!multiplier[Cursed].Found || !additive[Cursed].Found)
            {
                return failures + Assert(
                    report,
                    false,
                    "both reward props stand somewhere the gameplay camera can photograph them",
                    "the ship seed raised no pair to photograph");
            }

            var tile = ScreenFrame.TileGroundPixels(LevelFraming.PlaySize);

            report.Append("\n  at gameplay zoom ")
                .Append(LevelFraming.PlaySize.ToString("0.#", CultureInfo.InvariantCulture))
                .Append(" one tile of ground covers ")
                .Append(tile.ToString("0", CultureInfo.InvariantCulture))
                .Append(" of the ")
                .Append(ScreenFrame.Width * ScreenFrame.Height)
                .Append(" pixels in frame");

            failures += ReadsAgainstTheFloor(report, multiplier, tile);
            failures += ReadsAgainstTheFloor(report, additive, tile);

            return failures;
        }

        static int ReadsAgainstTheFloor(StringBuilder report, Reading[] rounds, float tile)
        {
            var reading = rounds[Cursed];
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
                    "its silhouette covers {0} pixels, {1:0.###} of a tile",
                    reading.Pixels,
                    share));

            var worst = Math.Min(rounds[Cursed].Contrast, rounds[Cleared].Contrast);

            failures += Assert(
                report,
                worst >= ToneContrast,
                "the " + reading.Name + " prop reads at least "
                + ToneContrast.ToString("0.##", CultureInfo.InvariantCulture)
                + ":1 in tone against the ground and masonry behind it, over a cursed floor and again "
                + "over a cleared one, so it is never lost in what it stands on",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "over a cursed floor its silhouette averages {0:0.####} relative luminance against the "
                    + "{1:0.####} of the ground it hides, {2:0.##}:1; over a cleared floor {3:0.####} against "
                    + "{4:0.####}, {5:0.##}:1",
                    rounds[Cursed].Prop,
                    rounds[Cursed].Ground,
                    rounds[Cursed].Contrast,
                    rounds[Cleared].Prop,
                    rounds[Cleared].Ground,
                    rounds[Cleared].Contrast));

            return failures;
        }

        static void Unmark(GameObject root)
        {
            foreach (var target in root.GetComponentsInChildren<NodeTarget>(true))
            {
                target.Wear(TargetMark.Idle, 0);
            }
        }

        static Reading Photograph(
            LevelGraph graph,
            IDictionary<string, Transform> byName,
            PartStyle style,
            string name,
            string path,
            string barePath)
        {
            var reading = new Reading { Name = name };
            var instance = PropOf(graph, byName, PartModels.Of(style));

            if (instance == null)
            {
                return reading;
            }

            var camera = PreviewFilm.Rig(instance.position, ShotDistance, LevelFraming.PlaySize);
            PreviewFilm.Warm(camera);

            var silhouette = Silhouette(instance, camera);
            PreviewFilm.Warm(camera);

            instance.gameObject.SetActive(false);
            var bare = PreviewFilm.Frame(camera);
            instance.gameObject.SetActive(true);
            var dressed = PreviewFilm.Frame(camera);

            var ground = bare.GetPixels32();
            var lit = dressed.GetPixels32();
            var covered = 0;
            var prop = 0.0;
            var under = 0.0;

            for (var pixel = 0; pixel < silhouette.Length; pixel++)
            {
                if (!silhouette[pixel])
                {
                    continue;
                }

                covered++;
                prop += Luminance(lit[pixel]);
                under += Luminance(ground[pixel]);
            }

            reading.Found = true;
            reading.Pixels = covered;
            reading.Prop = covered == 0 ? 0.0 : prop / covered;
            reading.Ground = covered == 0 ? 0.0 : under / covered;

            PreviewFilm.Save(dressed, path);
            PreviewFilm.Save(bare, barePath);

            UnityEngine.Object.DestroyImmediate(bare);
            UnityEngine.Object.DestroyImmediate(dressed);
            UnityEngine.Object.DestroyImmediate(camera.gameObject);

            return reading;
        }

        static bool[] Silhouette(Transform instance, Camera camera)
        {
            var was = new List<KeyValuePair<GameObject, int>>();

            foreach (var node in instance.GetComponentsInChildren<Transform>(true))
            {
                was.Add(new KeyValuePair<GameObject, int>(node.gameObject, node.gameObject.layer));
                node.gameObject.layer = AloneLayer;
            }

            var culling = camera.cullingMask;
            var background = camera.backgroundColor;
            camera.cullingMask = 1 << AloneLayer;

            camera.backgroundColor = Color.black;
            var onBlack = PreviewFilm.Frame(camera);
            camera.backgroundColor = Color.white;
            var onWhite = PreviewFilm.Frame(camera);

            camera.cullingMask = culling;
            camera.backgroundColor = background;

            foreach (var node in was)
            {
                node.Key.layer = node.Value;
            }

            var black = onBlack.GetPixels32();
            var white = onWhite.GetPixels32();
            var silhouette = new bool[black.Length];

            for (var pixel = 0; pixel < silhouette.Length; pixel++)
            {
                silhouette[pixel] = !Changed(black[pixel], white[pixel]);
            }

            UnityEngine.Object.DestroyImmediate(onBlack);
            UnityEngine.Object.DestroyImmediate(onWhite);

            return silhouette;
        }

        static List<Renderer> Repaint(GameObject root, out Material cursed)
        {
            var repainted = new List<Renderer>();
            var cleared = Painted(root, PartStyle.Cleared);
            cursed = Painted(root, PartStyle.Floor);

            if (cleared == null || cursed == null)
            {
                return repainted;
            }

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterial == cursed)
                {
                    renderer.sharedMaterial = cleared;
                    repainted.Add(renderer);
                }
            }

            return repainted;
        }

        static Material Painted(GameObject root, PartStyle style)
        {
            var wanted = WorldMaterials.NamePrefix + style;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var material = renderer.sharedMaterial;

                if (material != null && material.name == wanted)
                {
                    return material;
                }
            }

            return null;
        }

        static bool Changed(Color32 before, Color32 after)
        {
            return Math.Abs(before.r - after.r) > ChannelTolerance
                || Math.Abs(before.g - after.g) > ChannelTolerance
                || Math.Abs(before.b - after.b) > ChannelTolerance;
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

        static List<GameObject> Unbadge(GameObject root)
        {
            var hidden = new List<GameObject>();

            foreach (var node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == PartNames.BadgesGroup && node.gameObject.activeSelf)
                {
                    node.gameObject.SetActive(false);
                    hidden.Add(node.gameObject);
                }
            }

            return hidden;
        }

        struct Reading
        {
            public string Name;

            public bool Found;

            public int Pixels;

            public double Prop;

            public double Ground;

            public double Contrast
            {
                get { return ContrastOf(Prop, Ground); }
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
            var wrong = new List<string>();
            var missing = new List<string>();
            var read = 0;
            var flat = 0;
            var mistinted = new List<string>();

            foreach (PartStyle style in Enum.GetValues(typeof(PartStyle)))
            {
                var wants = PartModels.Of(style) != PartModel.None;

                if (wants)
                {
                    dressed++;
                }

                var material = Painted(root, style);

                if (material == null)
                {
                    if (wants)
                    {
                        missing.Add(style.ToString());
                    }

                    continue;
                }

                var wears = material.HasProperty(BaseMap) && material.GetTexture(BaseMap) != null;

                if (wants != wears)
                {
                    wrong.Add(style.ToString());
                }

                if (!material.HasProperty(BaseColour))
                {
                    continue;
                }

                read++;
                var wanted = WorldMaterials.ColourFor(style, wears);

                if (material.GetColor(BaseColour) == wanted)
                {
                    flat++;
                }
                else
                {
                    mistinted.Add(
                        style + " reads " + material.GetColor(BaseColour) + " against " + wanted);
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
                world.Count > 0 && wrong.Count == 0,
                "exactly the materials the built dungeon wears for a style with a mesh bind the one pack "
                + "atlas, so the whole dungeon is one texture",
                atlassed + " of " + world.Count + " world materials do, drawn from the " + dressed
                + " styles that want a mesh"
                + (wrong.Count == 0 ? "" : "; wrong on " + string.Join(", ", wrong.ToArray())));

            failures += Assert(
                report,
                read > 0 && flat == read,
                "a style whose mesh carries the pack atlas multiplies it by white so the texture shows, "
                + "and every other style still wears its palette colour, so a style whose atlas failed to "
                + "load stays a visible flat colour rather than turning plain white",
                flat + " of " + read + " do"
                + (mistinted.Count == 0 ? "" : "; " + string.Join("; ", mistinted.ToArray())));

            var explained = new List<string> { PartStyle.Staircase.ToString() };

            failures += Assert(
                report,
                missing.Count == explained.Count && missing.TrueForAll(explained.Contains),
                "every style that wants a mesh wears one somewhere in the built world, bar the staircase, "
                + "whose flight the floor state paints as ground the moment the run opens",
                (missing.Count == 0 ? "nothing is missing" : "missing " + string.Join(", ", missing.ToArray()))
                + " against the " + string.Join(", ", explained.ToArray()) + " this level explains");

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
            return PackMesh.Bare(prefab);
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
