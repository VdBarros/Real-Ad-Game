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

        const float CastContrast = 1.5f;

        const float ShotDistance = 60f;

        const int ChannelTolerance = 8;

        const float ColourTolerance = 1e-3f;

        const int AloneLayer = 31;

        const int Cursed = 0;

        const int Cleared = 1;

        const int Rounds = 2;

        const string AdditiveShot = "dev/scratch/t-30-additive.png";

        const string MultiplierShot = "dev/scratch/t-30-multiplier.png";

        const string ClearedShot = "dev/scratch/t-30-multiplier-cleared.png";

        const string FloorShot = "dev/scratch/t-30-floor-only.png";

        const int Complaints = 6;

        const float LandmarkFooting = 0.02f;

        const float LandmarkShare = 0.3f;

        const float LandmarkOutlineApart = 0.25f;

        const float LandmarkColourApart = 0.3f;

        const float LandmarkFit = 0.004f;

        static readonly float[] PanYaws =
        {
            IsoProjection.CameraYaw,
            IsoProjection.CameraYaw + 90f,
            IsoProjection.CameraYaw + 180f,
            IsoProjection.CameraYaw + 270f
        };

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

                var shipsWithTheCast = ArtPacks.ShipsWithTheCast(model);
                var settled = importer.materialImportMode == ModelImporterMaterialImportMode.None
                    && importer.importAnimation == CharacterArtPostprocessor.Animated(path)
                    && importer.animationType == (shipsWithTheCast
                        ? CharacterArtPostprocessor.Rig
                        : ModelImporterAnimationType.None)
                    && !importer.importCameras
                    && !importer.importLights
                    && !importer.importBlendShapes
                    && !importer.isReadable
                    && importer.optimizeMeshVertices
                    && importer.useFileScale
                    && Math.Abs(importer.globalScale - ArtPacks.ImportScaleFor(model)) < 1e-6f
                    && (!shipsWithTheCast
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

            failures += TheDungeonPlanIsMeasuredFromTheMeshes(models, report);

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
            failures += TheLandmarksMarkDecisionsWithoutBecomingOne(builder, graph, byName, report);
            failures += TheRewardsReadApart(graph, byName, report);
            failures += TheRewardsReadAgainstTheFloor(root, graph, byName, report);
            failures += TheLandmarksAreCutFromPackMeshes(models, graph, byName, report);
            failures += TheLandmarksStandWhereTheirReachAndHeightSayTheyDo(graph, byName, report);
            failures += NoLandmarkClipsWhatShipsBesideIt(graph, byName, report);
            failures += TheLandmarksReadApartFromEveryPanAngle(graph, byName, report);
            failures += TheCastReadsAgainstTheDungeon(root, graph, byName, report);
            failures += TheMaterialsStayFlat(root, report);
            failures += TheGatesAreCutFromPackMeshes(models, graph, byName, report);
            failures += TheGateWalkwayStaysClear(builder, graph, byName, report);

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

        static int TheGatesAreCutFromPackMeshes(
            WorldModels models,
            LevelGraph graph,
            IDictionary<string, Transform> byName,
            StringBuilder report)
        {
            var pack = new List<Mesh>();

            foreach (var mesh in PackMesh.Of(models.Of(GateArch.Masonry)))
            {
                pack.Add(mesh);
            }

            foreach (var mesh in PackMesh.Of(models.Of(GateArch.Pipwork)))
            {
                pack.Add(mesh);
            }

            var gates = 0;
            var meshed = 0;
            var counted = 0;
            var lit = 0;
            var dimmed = 0;
            var complaint = new List<string>();

            foreach (var node in graph.Decisions.Nodes)
            {
                if (node.Type != NodeType.Multiplier)
                {
                    continue;
                }

                Transform instance;
                if (!byName.TryGetValue(PartNames.Node(node.Id), out instance))
                {
                    complaint.Add(PartNames.Node(node.Id) + " is not in the world");
                    continue;
                }

                gates++;
                var pieces = GateArch.Pieces(node.Value);
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                var wearing = renderers.Length == pieces.Count;
                var strays = new List<string>();

                foreach (var renderer in renderers)
                {
                    var mesh = PackMesh.On(renderer);

                    if (mesh == null || !pack.Contains(mesh))
                    {
                        wearing = false;
                        strays.Add(renderer.name + " wears " + (mesh == null ? "nothing" : mesh.name));
                    }
                }

                var raised = 0;

                foreach (var piece in pieces)
                {
                    var child = instance.Find(piece.Name);

                    if (child != null && PackMesh.Around(child.gameObject).size.sqrMagnitude > 0f)
                    {
                        raised++;
                    }
                }

                if (wearing && raised == pieces.Count)
                {
                    meshed++;
                }
                else if (complaint.Count < Complaints)
                {
                    complaint.Add(instance.name + ": " + raised + " of " + pieces.Count
                        + " pieces carry a mesh, " + renderers.Length + " renderers"
                        + (strays.Count == 0 ? "" : "; " + string.Join(", ", strays.ToArray())));
                }

                var glow = instance.GetComponent<GateProp>();
                var target = instance.GetComponent<NodeTarget>();

                if (glow == null || target == null)
                {
                    continue;
                }

                if (glow.Pips == node.Value && PipsUnder(instance) == node.Value)
                {
                    counted++;
                }

                target.Wear(TargetMark.Idle, 0);

                if (Alike(glow.Colour, Tints.Of(GateLook.Of(node.Value))))
                {
                    lit++;
                }

                target.Wear(TargetMark.Unreachable, 0);

                if (Alike(
                    glow.Colour,
                    Tints.Of(GateLook.Washed(node.Value, TargetMarks.Look(TargetMark.Unreachable)))))
                {
                    dimmed++;
                }

                target.Wear(TargetMark.Idle, 0);
            }

            var failures = 0;

            failures += Assert(
                report,
                gates > 0 && meshed == gates,
                "every gate is cut from the " + GateArch.Masonry + " and " + GateArch.Pipwork
                + " meshes of the dungeon pack, with no primitive box left in the arch",
                meshed + " of " + gates + " are, from " + pack.Count + " pack meshes"
                + (complaint.Count == 0 ? "" : "; " + string.Join("; ", complaint.ToArray())));

            failures += Assert(
                report,
                gates > 0 && counted == gates,
                "every gate still counts its factor in a row of pips standing on its lintel, so the "
                + "factor is readable from the geometry without a badge",
                counted + " of " + gates + " do");

            failures += Assert(
                report,
                gates > 0 && lit == gates,
                "every gate still glows in the colour its factor was given",
                lit + " of " + gates + " do");

            return failures + Assert(
                report,
                gates > 0 && dimmed == gates,
                "every gate still washes and dims to the unreachable mark rather than holding its plain "
                + "colour",
                dimmed + " of " + gates + " do");
        }

        static int PipsUnder(Transform instance)
        {
            var pips = 0;

            foreach (var node in instance.GetComponentsInChildren<Transform>(true))
            {
                if (PartNames.IsGatePip(node.name))
                {
                    pips++;
                }
            }

            return pips;
        }

        static bool Alike(Color one, Color other)
        {
            return Math.Abs(one.r - other.r) <= ColourTolerance
                && Math.Abs(one.g - other.g) <= ColourTolerance
                && Math.Abs(one.b - other.b) <= ColourTolerance;
        }

        const int GateWalkSamples = 16;

        static int TheGateWalkwayStaysClear(
            WorldBuilder builder,
            LevelGraph graph,
            IDictionary<string, Transform> byName,
            StringBuilder report)
        {
            var gate = GateUnder(graph, byName);
            var power = builder.PlayerBadge;
            var player = builder.Player;

            if (gate == null || power == null || player == null)
            {
                return Assert(
                    report,
                    false,
                    "the ship level raises a gate and a player to walk it through",
                    (gate == null ? "no gate" : "a gate") + " and "
                    + (player == null ? "no player" : "a player"));
            }

            var animator = player.GetComponent<FigureAnimator>();

            if (animator == null)
            {
                return Assert(
                    report, false, "the player it walks through carries its own clips", "it carries none");
            }

            var triangles = new List<Vector3[]>();
            GateClearance.Gather(gate, gate, triangles);

            var opening = power.Power;
            var clear = 0;
            var tiers = 0;
            var narrowest = float.MaxValue;
            var readings = new List<string>();
            var clipping = new List<string>();

            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                PowerPump.Settle(power, PowerAt(tier));
                player.Sling(true);
                tiers++;

                var kit = SweptThroughTheWalk(player, animator);
                var ground = player.Ground.Y;
                var across = Math.Max(kit.size.x, kit.size.z);
                var low = kit.min.y - ground;
                var high = kit.max.y - ground;
                var half = new Vector3(across * 0.5f, (high - low) * 0.5f, GateArch.Depth);
                var centre = new Vector3(0f, -GateArch.Height * 0.5f + (high + low) * 0.5f, 0f);

                readings.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "tier {0} sweeps {1:0.###} across and reaches {2:0.###} up",
                    tier,
                    across,
                    high));

                narrowest = Math.Min(narrowest, GateArch.Walkway - across);

                if (!GateClearance.Blocked(triangles, centre, half))
                {
                    clear++;
                }
                else
                {
                    clipping.Add("tier " + tier + " clips the arch");
                }
            }

            player.Sling(false);
            PowerPump.Settle(power, opening);

            return Assert(
                report,
                tiers > 0 && clear == tiers,
                "the whole kit of a player of every tier sweeps through the gate's walkway across every "
                + "frame of the walk clip without touching a single triangle of its posts or its lintel, "
                + "read square on, the weapon it is carrying counted in rather than excused",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} of {1} tiers pass through the arch's {2} triangles; the widest leaves {3:0.###} "
                    + "of the {4:0.###} walkway spare; {5}{6}",
                    clear,
                    tiers,
                    triangles.Count,
                    narrowest,
                    GateArch.Walkway,
                    string.Join(", ", readings.ToArray()),
                    clipping.Count == 0 ? "" : "; " + string.Join(", ", clipping.ToArray())));
        }

        static Bounds SweptThroughTheWalk(PlayerFigure player, FigureAnimator animator)
        {
            animator.Cue(FigureCue.Looping(FigureAct.Walk));
            animator.Advance(0f);

            var facing = player.transform.rotation;
            player.transform.rotation = Quaternion.identity;
            var swept = World(player.transform);
            var seconds = animator.PlayingSeconds;

            if (animator.Playing == null || seconds <= 0f)
            {
                player.transform.rotation = facing;

                return swept;
            }

            var step = seconds / GateWalkSamples;

            for (var sample = 0; sample < GateWalkSamples; sample++)
            {
                animator.Advance(step);
                swept.Encapsulate(World(player.transform));
            }

            player.transform.rotation = facing;

            return swept;
        }

        static int PowerAt(int tier)
        {
            return tier == 0 ? 1 : PlayerTier.Thresholds[tier - 1];
        }

        static Transform GateUnder(LevelGraph graph, IDictionary<string, Transform> byName)
        {
            foreach (var node in graph.Decisions.Nodes)
            {
                Transform instance;

                if (node.Type == NodeType.Multiplier
                    && byName.TryGetValue(PartNames.Node(node.Id), out instance))
                {
                    return instance;
                }
            }

            return null;
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
                    WorldPart panel;

                    if (graph.Tiles.ContainsPlace(beyond.X, beyond.Y)
                        || !LevelBlueprintBuilder.TryWall(graph.Tiles, tile.Position, side, out panel))
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
                        (here.X + there.X) * 0.5f,
                        StaircaseFlight.HandsOverAt(graph.Tiles, tile.Position, side),
                        (here.Z + there.Z) * 0.5f);
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

        static int TheLandmarksMarkDecisionsWithoutBecomingOne(
            WorldBuilder builder,
            LevelGraph graph,
            IDictionary<string, Transform> byName,
            StringBuilder report)
        {
            var spots = Landmarks.Of(graph);
            var failures = 0;
            var standing = new List<string>();

            foreach (var spot in spots)
            {
                standing.Add(spot.ToString());
            }

            failures += Assert(
                report,
                spots.Count >= Landmarks.Fewest && spots.Count <= Landmarks.Most,
                "the ship level carries between " + Landmarks.Fewest + " and " + Landmarks.Most
                + " landmarks, so a screen has something to orient by without turning into a gallery",
                spots.Count + ": " + string.Join(", ", standing.ToArray()));

            var marking = 0;
            var kinds = new List<LandmarkKind>();

            foreach (var spot in spots)
            {
                if (Landmarks.MarksADecision(graph.Tiles, spot.Tile)
                    || Landmarks.Flanks(graph.Tiles, spot.Tile))
                {
                    marking++;
                }

                if (!kinds.Contains(spot.Kind))
                {
                    kinds.Add(spot.Kind);
                }
            }

            failures += Assert(
                report,
                spots.Count > 0 && marking == spots.Count && kinds.Count == spots.Count,
                "every landmark stands at a junction or an area entrance rather than mid-corridor, and no "
                + "two of them are the same kind",
                marking + " of " + spots.Count + " mark a decision, in " + kinds.Count + " distinct kinds");

            var onANode = new List<string>();
            var tappable = new List<string>();
            var solid = new List<string>();
            var raised = 0;
            var pieced = 0;

            foreach (var spot in spots)
            {
                var name = PartNames.Landmark(spot.Tile);
                Transform instance;

                if (!byName.TryGetValue(name, out instance))
                {
                    continue;
                }

                raised++;

                if (graph.Decisions.NodeAt(spot.Tile) != null)
                {
                    onANode.Add(name);
                }

                if (instance.GetComponentsInChildren<NodeTarget>(true).Length > 0)
                {
                    tappable.Add(name + " wears a node target");
                }

                if (instance.GetComponentInParent<NodeTarget>() != null)
                {
                    tappable.Add(name + " hangs under a node target");
                }

                foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                {
                    solid.Add(name + "/" + collider.name);
                }

                var prop = instance.GetComponent<LandmarkProp>();

                if (prop != null && prop.Pieces == LandmarkForm.Pieces(spot.Kind).Count
                    && prop.Kind == spot.Kind
                    && prop.Textured
                    && prop.Tint.Equals(LandmarkLook.Of(spot.Kind))
                    && instance.GetComponentsInChildren<Renderer>(true).Length >= prop.Pieces)
                {
                    pieced++;
                }
            }

            failures += Assert(
                report,
                raised == spots.Count && pieced == spots.Count,
                "every landmark the placement names is standing in the world, every course of it raised "
                + "and wearing the pack atlas under the wash its kind is given",
                pieced + " of " + spots.Count + " are, and " + raised + " were raised at all");

            failures += Assert(
                report,
                onANode.Count == 0,
                "no landmark stands on a tile the decision graph put a node on",
                onANode.Count == 0 ? "none does" : string.Join(", ", onANode.ToArray()));

            failures += Assert(
                report,
                spots.Count > 0 && tappable.Count == 0,
                "no landmark carries a node target, so a finger can never aim at one",
                tappable.Count == 0
                    ? "none of the " + spots.Count + " does"
                    : string.Join("; ", tappable.ToArray()));

            failures += Assert(
                report,
                spots.Count > 0 && solid.Count == 0,
                "no landmark carries a collider, so nothing the world casts against can ever hit one",
                solid.Count == 0
                    ? "none of the " + spots.Count + " does"
                    : string.Join(", ", solid.GetRange(0, Math.Min(Complaints, solid.Count)).ToArray()));

            var aimed = 0;

            foreach (var target in builder.Targets.Targets)
            {
                if (target != null && target.NodeId >= 0 && target.NodeId < graph.Decisions.Nodes.Count)
                {
                    aimed++;
                }
            }

            failures += Assert(
                report,
                aimed == builder.Targets.Targets.Count,
                "every target the board holds is a decision node, so the landmarks never entered the "
                + "decision graph",
                aimed + " of " + builder.Targets.Targets.Count + " targets name a node of the "
                + graph.Decisions.Nodes.Count + " this level has");

            var figure = FigureFit.SpreadOf(
                CharacterCast.MeshOf(PartStyle.Start), LevelBlueprintBuilder.FigureScale) * 0.5f;
            var tightest = float.MaxValue;
            var blocking = new List<string>();

            foreach (var spot in spots)
            {
                Transform instance;

                if (!byName.TryGetValue(PartNames.Landmark(spot.Tile), out instance))
                {
                    continue;
                }

                var box = World(instance);

                foreach (var tile in graph.Tiles.Tiles)
                {
                    var centre = IsoProjection.Of(tile.Position);
                    var apart = Math.Max(
                        Math.Max(box.min.x - centre.X, centre.X - box.max.x),
                        Math.Max(box.min.z - centre.Z, centre.Z - box.max.z));

                    if (apart < tightest)
                    {
                        tightest = apart;
                    }

                    if (apart <= figure)
                    {
                        blocking.Add(spot.Kind + " reaches " + tile.Position);
                    }
                }
            }

            failures += Assert(
                report,
                spots.Count > 0 && blocking.Count == 0,
                "no landmark reaches within the figure's own half-spread of a tile centre the walk runs "
                + "through, so a landmark never blocks the walk",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "the tightest leaves {0:0.###} against the figure's {1:0.###} half-spread{2}",
                    tightest,
                    figure,
                    blocking.Count == 0
                        ? ""
                        : "; " + string.Join(", ", blocking.GetRange(0, Math.Min(Complaints, blocking.Count)).ToArray())));

            var footed = 0;
            var overTheWalls = 0;

            foreach (var spot in spots)
            {
                Transform instance;

                if (!byName.TryGetValue(PartNames.Landmark(spot.Tile), out instance))
                {
                    continue;
                }

                var box = World(instance);

                if (Math.Abs(box.min.y - IsoProjection.Of(spot.Tile).Y) <= LandmarkFooting)
                {
                    footed++;
                }

                if (box.size.y > IsoProjection.WallHeight)
                {
                    overTheWalls++;
                }
            }

            return failures + Assert(
                report,
                spots.Count > 0 && footed == spots.Count && overTheWalls == spots.Count,
                "every landmark rests on the floor of its tile and stands taller than the masonry it "
                + "leans against, so it is visible over the walls between the player and it",
                footed + " of " + spots.Count + " are footed and " + overTheWalls + " clear the "
                + IsoProjection.WallHeight + " wall");
        }

        static int TheDungeonPlanIsMeasuredFromTheMeshes(WorldModels models, StringBuilder report)
        {
            var failures = 0;

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (model == PartModel.None || ArtPacks.Of(model) != ArtPack.Dungeon)
                {
                    continue;
                }

                var prefab = models.Of(model);

                if (prefab == null)
                {
                    failures += Assert(
                        report,
                        false,
                        "the " + model + " mesh matches its pinned pack plan and base",
                        "there is no mesh to measure");
                    continue;
                }

                var box = Local(prefab);
                var wide = DungeonPack.WidthOf(model);
                var deep = DungeonPack.DepthOf(model);
                var foot = DungeonPack.BaseOf(model);
                var across = DungeonPack.ShiftAcrossOf(model);
                var along = DungeonPack.ShiftAlongOf(model);

                failures += Assert(
                    report,
                    Math.Abs(box.size.x - wide) <= Epsilon
                    && Math.Abs(box.size.z - deep) <= Epsilon
                    && Math.Abs(box.min.y - foot) <= Epsilon
                    && Math.Abs(box.center.x - across) <= Epsilon
                    && Math.Abs(box.center.z - along) <= Epsilon,
                    "the imported " + model + " mesh spans the pinned "
                    + DungeonPack.PackWidthOf(model).ToString("0.####", CultureInfo.InvariantCulture)
                    + " by "
                    + DungeonPack.PackDepthOf(model).ToString("0.####", CultureInfo.InvariantCulture)
                    + " pack units off a base "
                    + DungeonPack.PackBaseOf(model).ToString("0.####", CultureInfo.InvariantCulture)
                    + " from its pivot, sitting "
                    + DungeonPack.PackShiftAcrossOf(model).ToString("0.####", CultureInfo.InvariantCulture)
                    + " by "
                    + DungeonPack.PackShiftAlongOf(model).ToString("0.####", CultureInfo.InvariantCulture)
                    + " off the pivot in plan",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "it measures {0:0.#####} by {1:0.#####} against {2:0.#####} by {3:0.#####}, "
                        + "its base {4:0.#####} from the pivot against {5:0.#####}, its plan centred on "
                        + "({6:0.#####}, {7:0.#####}) against ({8:0.#####}, {9:0.#####})",
                        box.size.x,
                        box.size.z,
                        wide,
                        deep,
                        box.min.y,
                        foot,
                        box.center.x,
                        box.center.z,
                        across,
                        along));
            }

            return failures;
        }

        static int TheLandmarksAreCutFromPackMeshes(
            WorldModels models,
            LevelGraph graph,
            IDictionary<string, Transform> byName,
            StringBuilder report)
        {
            var failures = 0;
            var pack = new Dictionary<PartModel, ISet<Mesh>>();
            var wanted = new List<PartModel>();
            var primitive = new List<string>();

            foreach (var kind in LandmarkForm.Kinds)
            {
                foreach (var piece in LandmarkForm.Pieces(kind))
                {
                    if (piece.Model == PartModel.None)
                    {
                        primitive.Add(kind + "/" + piece.Name + " names no mesh at all");
                        continue;
                    }

                    if (piece.Shape != PartShape.Landmark)
                    {
                        primitive.Add(kind + "/" + piece.Name + " is cut as a " + piece.Shape);
                    }

                    if (ArtPacks.Of(piece.Model) != ArtPack.Dungeon)
                    {
                        primitive.Add(kind + "/" + piece.Name + " reaches outside the dungeon pack");
                    }

                    if (!pack.ContainsKey(piece.Model))
                    {
                        pack.Add(piece.Model, PackMesh.Of(models.Of(piece.Model)));
                        wanted.Add(piece.Model);
                    }
                }
            }

            var names = new List<string>();

            foreach (var model in wanted)
            {
                names.Add(model + " = " + WorldModels.AssetPathOf(model));
            }

            failures += Assert(
                report,
                primitive.Count == 0,
                "every landmark course names a dungeon-pack mesh, so no primitive is left in LandmarkForm",
                wanted.Count + " meshes dress the " + LandmarkForm.Kinds.Count + " kinds: "
                + string.Join(", ", names.ToArray())
                + (primitive.Count == 0 ? "" : "; " + string.Join("; ", primitive.ToArray())));

            var everyMesh = new List<Mesh>();

            foreach (var model in wanted)
            {
                foreach (var mesh in pack[model])
                {
                    everyMesh.Add(mesh);
                }
            }

            var spots = Landmarks.Of(graph);
            var meshed = 0;
            var raised = 0;
            var strays = new List<string>();

            foreach (var spot in spots)
            {
                Transform instance;

                if (!byName.TryGetValue(PartNames.Landmark(spot.Tile), out instance))
                {
                    continue;
                }

                raised++;
                var pieces = LandmarkForm.Pieces(spot.Kind);
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                var wearing = renderers.Length >= pieces.Count;

                foreach (var renderer in renderers)
                {
                    var mesh = PackMesh.On(renderer);

                    if (mesh == null || !everyMesh.Contains(mesh))
                    {
                        wearing = false;

                        if (strays.Count < Complaints)
                        {
                            strays.Add(spot.Kind + "/" + renderer.name + " wears "
                                + (mesh == null ? "nothing" : mesh.name));
                        }
                    }
                }

                var carried = 0;

                foreach (var piece in pieces)
                {
                    var child = instance.Find(piece.Name);

                    if (child != null && PackMesh.Around(child.gameObject).size.sqrMagnitude > 0f)
                    {
                        carried++;
                    }
                }

                if (wearing && carried == pieces.Count)
                {
                    meshed++;
                }
            }

            return failures + Assert(
                report,
                raised > 0 && meshed == raised && raised == spots.Count,
                "every landmark standing in the world is cut from those pack meshes, with no primitive "
                + "block left in the build",
                meshed + " of " + raised + " raised, against " + spots.Count + " the placement names"
                + (strays.Count == 0 ? "" : "; " + string.Join(", ", strays.ToArray())));
        }

        static int TheLandmarksStandWhereTheirReachAndHeightSayTheyDo(
            LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            var readings = new List<string>();
            var lying = new List<string>();
            var measured = 0;

            foreach (var spot in Landmarks.Of(graph))
            {
                Transform instance;

                if (!byName.TryGetValue(PartNames.Landmark(spot.Tile), out instance))
                {
                    continue;
                }

                measured++;
                var box = World(instance);
                var standing = Landmarks.StandingOf(spot);
                var reach = Math.Max(
                    Math.Max(box.max.x - standing.X, standing.X - box.min.x),
                    Math.Max(box.max.z - standing.Z, standing.Z - box.min.z));
                var promised = LandmarkForm.ReachOf(spot.Kind);
                var tall = LandmarkForm.StandingHeightOf(spot.Kind);

                readings.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} reaches {1:0.####} of a promised {2:0.####} and stands {3:0.####} of a promised "
                    + "{4:0.####}",
                    spot.Kind,
                    reach,
                    promised,
                    box.size.y,
                    tall));

                if (Math.Abs(reach - promised) > LandmarkFit
                    || Math.Abs(box.size.y - tall) > LandmarkFit
                    || reach > LandmarkForm.Reach + LandmarkFit)
                {
                    lying.Add(spot.Kind.ToString());
                }
            }

            return Assert(
                report,
                measured > 0 && lying.Count == 0,
                "the reach and the standing height every kind answers with are the ones its pack meshes "
                + "actually take up in the world, and none of them claims more than the "
                + LandmarkForm.Reach.ToString("0.###", CultureInfo.InvariantCulture)
                + " footprint the placement budgets for, so placement and badge anchoring are told the truth",
                string.Join("; ", readings.ToArray())
                + (lying.Count == 0 ? "" : "; overstated by " + string.Join(", ", lying.ToArray())));
        }

        static int NoLandmarkClipsWhatShipsBesideIt(
            LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            var landmarks = new List<KeyValuePair<string, Bounds>>();

            foreach (var spot in Landmarks.Of(graph))
            {
                Transform instance;

                if (byName.TryGetValue(PartNames.Landmark(spot.Tile), out instance))
                {
                    landmarks.Add(new KeyValuePair<string, Bounds>(spot.Kind.ToString(), World(instance)));
                }
            }

            var others = new List<KeyValuePair<string, Bounds>>();

            foreach (var node in graph.Decisions.Nodes)
            {
                Transform instance;

                if (byName.TryGetValue(PartNames.Node(node.Id), out instance))
                {
                    others.Add(new KeyValuePair<string, Bounds>(
                        node.Type + " " + PartNames.Node(node.Id), World(instance)));
                }
            }

            var clipping = new List<string>();

            for (var one = 0; one < landmarks.Count; one++)
            {
                for (var other = one + 1; other < landmarks.Count; other++)
                {
                    if (landmarks[one].Value.Intersects(landmarks[other].Value))
                    {
                        clipping.Add(landmarks[one].Key + " into " + landmarks[other].Key);
                    }
                }

                foreach (var subject in others)
                {
                    if (landmarks[one].Value.Intersects(subject.Value))
                    {
                        clipping.Add(landmarks[one].Key + " into " + subject.Key);
                    }
                }
            }

            var failures = Assert(
                report,
                landmarks.Count > 0 && others.Count > 0 && clipping.Count == 0,
                "no landmark's bounds touch another landmark, a gate, a chest or a figure the level ships",
                landmarks.Count + " landmarks were held against " + others.Count + " node props"
                + (clipping.Count == 0
                    ? " and none of them meet"
                    : "; " + string.Join(", ", clipping.GetRange(0, Math.Min(Complaints, clipping.Count)).ToArray())));

            var walls = new List<KeyValuePair<string, Bounds>>();

            foreach (var tile in graph.Tiles.Tiles)
            {
                foreach (var side in TileSides.All)
                {
                    Transform panel;

                    if (byName.TryGetValue(PartNames.Wall(tile.Position, side), out panel))
                    {
                        walls.Add(new KeyValuePair<string, Bounds>(
                            PartNames.Wall(tile.Position, side), World(panel)));
                    }
                }
            }

            var leaning = new List<string>();
            var crowding = new List<string>();

            foreach (var spot in Landmarks.Of(graph))
            {
                Transform instance;

                if (!byName.TryGetValue(PartNames.Landmark(spot.Tile), out instance))
                {
                    continue;
                }

                var box = World(instance);
                var leant = PartNames.Wall(spot.Tile, spot.Against);
                var touched = 0;

                foreach (var panel in walls)
                {
                    if (!box.Intersects(panel.Value))
                    {
                        continue;
                    }

                    if (panel.Key == leant)
                    {
                        touched++;
                    }
                    else if (crowding.Count < Complaints)
                    {
                        crowding.Add(spot.Kind + " into " + panel.Key);
                    }
                }

                leaning.Add(spot.Kind + " leans on " + touched + " panel of its own side");
            }

            return failures + Assert(
                report,
                walls.Count > 0 && landmarks.Count > 0 && crowding.Count == 0,
                "the only masonry a landmark meets is the parapet of the very tile side it was stood "
                + "against, so it never pushes into a neighbouring panel",
                landmarks.Count + " landmarks were held against " + walls.Count + " panels; "
                + string.Join(", ", leaning.ToArray())
                + (crowding.Count == 0 ? "" : "; " + string.Join(", ", crowding.ToArray())));
        }

        static int TheLandmarksReadApartFromEveryPanAngle(
            LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            var failures = 0;

            failures += ReadApartAt(graph, byName, report, LevelFraming.PlaySize, "play");

            return failures + ReadApartAt(graph, byName, report, LevelFraming.CloseUpSize, "closed");
        }

        static int ReadApartAt(
            LevelGraph graph,
            IDictionary<string, Transform> byName,
            StringBuilder report,
            float framing,
            string framingName)
        {
            var readings = new List<LandmarkReading>();
            var tile = ScreenFrame.TileGroundPixels(framing);

            foreach (var spot in Landmarks.Of(graph))
            {
                Transform instance;

                if (!byName.TryGetValue(PartNames.Landmark(spot.Tile), out instance))
                {
                    continue;
                }

                for (var pan = 0; pan < PanYaws.Length; pan++)
                {
                    readings.Add(Photograph(instance, spot.Kind, framing, PanYaws[pan], pan, framingName));
                }
            }

            if (readings.Count < PanYaws.Length * 2)
            {
                return Assert(
                    report,
                    false,
                    "the ship level raises landmarks enough to compare against each other at the "
                    + framingName + " framing",
                    readings.Count + " were photographed");
            }

            var failures = 0;
            var specks = new List<string>();
            var shares = new List<string>();
            var thinnest = float.MaxValue;

            foreach (var reading in readings)
            {
                var share = tile <= 0f ? 0f : reading.Pixels / tile;

                thinnest = Math.Min(thinnest, share);

                if (reading.Pan == 0)
                {
                    shares.Add(string.Format(
                        CultureInfo.InvariantCulture, "{0} {1:0.##} tiles", reading.Kind, share));
                }

                if (share < LandmarkShare)
                {
                    specks.Add(reading.Kind + " covers only "
                        + share.ToString("0.###", CultureInfo.InvariantCulture) + " panned to "
                        + reading.Yaw.ToString("0", CultureInfo.InvariantCulture));
                }
            }

            failures += Assert(
                report,
                specks.Count == 0,
                "every landmark covers at least "
                + LandmarkShare.ToString("0.##", CultureInfo.InvariantCulture)
                + " of a tile of ground at the " + framingName
                + " framing from every pan angle, so it is a shape a player reads rather than a speck",
                string.Join(", ", shares.ToArray())
                + string.Format(
                    CultureInfo.InvariantCulture, "; the thinnest angle leaves {0:0.###}", thinnest)
                + (specks.Count == 0 ? "" : "; " + string.Join("; ", specks.ToArray())));

            var alike = new List<string>();
            var pairings = 0;
            var likest = 10f;
            var closest = 0f;
            var palest = 0f;
            var tightest = "no pairing at all";

            for (var one = 0; one < readings.Count; one++)
            {
                for (var other = one + 1; other < readings.Count; other++)
                {
                    if (readings[one].Kind == readings[other].Kind)
                    {
                        continue;
                    }

                    pairings++;
                    var apart = Disagreement(readings[one].Silhouette, readings[other].Silhouette);
                    var tone = Apart(readings[one].Colour, readings[other].Colour);
                    var pair = readings[one].Kind + " panned to "
                        + readings[one].Yaw.ToString("0", CultureInfo.InvariantCulture)
                        + " against " + readings[other].Kind + " panned to "
                        + readings[other].Yaw.ToString("0", CultureInfo.InvariantCulture);
                    var likeness = Math.Max(
                        apart / LandmarkOutlineApart, tone / LandmarkColourApart);

                    if (likeness < likest)
                    {
                        likest = likeness;
                        closest = apart;
                        palest = tone;
                        tightest = pair;
                    }

                    if (apart < LandmarkOutlineApart && tone < LandmarkColourApart)
                    {
                        alike.Add(pair);
                    }
                }
            }

            return failures + Assert(
                report,
                alike.Count == 0,
                "no two landmark kinds share both an outline and a colour at the " + framingName
                + " framing, at any pair of the " + PanYaws.Length
                + " pan angles the rig can look at them from, so a glance tells them apart",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "over {0} pairings the likest is {6}, which disagrees over {1:0.###} of its outline "
                    + "and {2:0.###} in colour, against the {3:0.##} / {4:0.##} either of which is "
                    + "enough{5}",
                    pairings,
                    closest,
                    palest,
                    LandmarkOutlineApart,
                    LandmarkColourApart,
                    alike.Count == 0
                        ? ""
                        : "; alike: " + string.Join(", ", alike.GetRange(0, Math.Min(Complaints, alike.Count)).ToArray()),
                    tightest));
        }

        struct LandmarkReading
        {
            public LandmarkKind Kind;

            public bool[] Silhouette;

            public int Pixels;

            public Color Colour;

            public float Yaw;

            public int Pan;
        }

        static LandmarkReading Photograph(
            Transform instance, LandmarkKind kind, float framing, float yaw, int pan, string framingName)
        {
            var camera = PreviewFilm.Rig(instance.position, ShotDistance, framing, yaw);
            PreviewFilm.Warm(camera);

            var was = new List<KeyValuePair<GameObject, int>>();

            foreach (var node in instance.GetComponentsInChildren<Transform>(true))
            {
                was.Add(new KeyValuePair<GameObject, int>(node.gameObject, node.gameObject.layer));
                node.gameObject.layer = AloneLayer;
            }

            camera.cullingMask = 1 << AloneLayer;
            camera.backgroundColor = Color.black;
            var onBlack = PreviewFilm.Frame(camera);
            camera.backgroundColor = Color.white;
            var onWhite = PreviewFilm.Frame(camera);

            foreach (var node in was)
            {
                node.Key.layer = node.Value;
            }

            var dark = onBlack.GetPixels32();
            var pale = onWhite.GetPixels32();
            var silhouette = new bool[dark.Length];
            var covered = 0;
            var red = 0.0;
            var green = 0.0;
            var blue = 0.0;

            for (var pixel = 0; pixel < silhouette.Length; pixel++)
            {
                silhouette[pixel] = !Changed(dark[pixel], pale[pixel]);

                if (!silhouette[pixel])
                {
                    continue;
                }

                covered++;
                red += dark[pixel].r;
                green += dark[pixel].g;
                blue += dark[pixel].b;
            }

            PreviewFilm.Save(onBlack, LandmarkShot(kind, framingName, pan));
            UnityEngine.Object.DestroyImmediate(onBlack);
            UnityEngine.Object.DestroyImmediate(onWhite);
            UnityEngine.Object.DestroyImmediate(camera.gameObject);

            return new LandmarkReading
            {
                Kind = kind,
                Silhouette = silhouette,
                Pixels = covered,
                Yaw = yaw,
                Pan = pan,
                Colour = covered == 0
                    ? Color.black
                    : new Color(
                        (float)(red / covered / 255.0),
                        (float)(green / covered / 255.0),
                        (float)(blue / covered / 255.0))
            };
        }

        static string LandmarkShot(LandmarkKind kind, string framingName, int pan)
        {
            return "dev/scratch/t-170-" + kind.ToString().ToLowerInvariant() + "-" + framingName + "-"
                + pan.ToString(CultureInfo.InvariantCulture) + ".png";
        }

        static float Disagreement(bool[] one, bool[] other)
        {
            var union = 0;
            var apart = 0;

            for (var pixel = 0; pixel < one.Length; pixel++)
            {
                if (one[pixel] || other[pixel])
                {
                    union++;
                }

                if (one[pixel] != other[pixel])
                {
                    apart++;
                }
            }

            return union == 0 ? 0f : (float)apart / union;
        }

        static float Apart(Color one, Color other)
        {
            return Math.Abs(one.r - other.r) + Math.Abs(one.g - other.g) + Math.Abs(one.b - other.b);
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
            var failures = PhotographedUnderTheBuildsLighting("the reward props are", report)
                + PhotographedInTheBuildsRoom("the reward props are", report);
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

            failures += Assert(
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

        static int TheCastReadsAgainstTheDungeon(
            GameObject root, LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            PreviewFilm.Sun();
            var lighting = PhotographedUnderTheBuildsLighting("the cast is", report)
                + PhotographedInTheBuildsRoom("the cast is", report);
            var badges = Unbadge(root);
            Unmark(root);

            var cast = CharacterCast.Roles;
            var rounds = new Reading[cast.Count][];

            for (var slot = 0; slot < cast.Count; slot++)
            {
                rounds[slot] = new Reading[Rounds];
                rounds[slot][Cursed] = Photograph(
                    graph, byName, cast[slot], cast[slot].ToString().ToLowerInvariant(), CastShot(cast[slot]), null);
            }

            Material cursed;
            var repainted = Repaint(root, out cursed);

            for (var slot = 0; slot < cast.Count; slot++)
            {
                rounds[slot][Cleared] = Photograph(
                    graph, byName, cast[slot], cast[slot].ToString().ToLowerInvariant(), null, null);
            }

            foreach (var renderer in repainted)
            {
                renderer.sharedMaterial = cursed;
            }

            foreach (var group in badges)
            {
                group.SetActive(true);
            }

            var failures = lighting;
            var photographed = 0;

            for (var slot = 0; slot < cast.Count; slot++)
            {
                if (!rounds[slot][Cursed].Found)
                {
                    report.Append("\n  no ").Append(cast[slot]).Append(" stands on the ship seed to photograph");
                    continue;
                }

                photographed++;
                failures += CastReadsAgainstTheFloor(report, rounds[slot]);
            }

            failures += Assert(
                report,
                photographed > 0,
                "at least one of the cast stands somewhere the gameplay camera can photograph it",
                photographed + " of " + cast.Count + " cast styles were raised by the ship seed");

            failures += TheTableSeparatesFigureFromSurface(report);

            return failures;
        }

        static int PhotographedUnderTheBuildsLighting(string subject, StringBuilder report)
        {
            var risen = PreviewFilm.SunsUp();

            return Assert(
                report,
                risen == 1,
                subject + " photographed under the one sun the build raises, so the tone measured below"
                + " is the tone that ships",
                "the frame is lit by " + risen
                + (risen == 1 ? " directional light" : " directional lights"));
        }

        static int PhotographedInTheBuildsRoom(string subject, StringBuilder report)
        {
            var elsewhere = PreviewFilm.RoomApartFromTheBuild();

            return Assert(
                report,
                elsewhere == null,
                subject + " photographed in the room the build ships, so the tone measured below is the"
                + " tone that ships",
                elsewhere ?? PreviewFilm.RoomAsPhotographed());
        }

        static int CastReadsAgainstTheFloor(StringBuilder report, Reading[] rounds)
        {
            var worst = Math.Min(rounds[Cursed].Contrast, rounds[Cleared].Contrast);

            return Assert(
                report,
                worst >= CastContrast,
                "the " + rounds[Cursed].Name + " figure reads at least "
                + CastContrast.ToString("0.##", CultureInfo.InvariantCulture)
                + ":1 in tone against the ground and masonry behind it, over a cursed floor and again "
                + "over a cleared one, so a greyscale frame keeps it as a silhouette",
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
        }

        static int TheTableSeparatesFigureFromSurface(StringBuilder report)
        {
            var worst = float.MaxValue;
            var pairing = string.Empty;

            foreach (PartStyle figure in Enum.GetValues(typeof(PartStyle)))
            {
                if (WorldTints.LayerOf(figure) != PartLayer.Figure)
                {
                    continue;
                }

                foreach (PartStyle surface in Enum.GetValues(typeof(PartStyle)))
                {
                    if (WorldTints.LayerOf(surface) != PartLayer.Surface)
                    {
                        continue;
                    }

                    var apart = Tint.Contrast(WorldTints.Of(figure), WorldTints.Of(surface));

                    if (WorldTints.Of(figure).Luminance >= WorldTints.Of(surface).Luminance)
                    {
                        apart = 0f;
                    }

                    if (apart >= worst)
                    {
                        continue;
                    }

                    worst = apart;
                    pairing = figure + " against " + surface;
                }
            }

            return Assert(
                report,
                worst >= WorldTints.LeastSeparation,
                "every figure style in the world palette stands at least "
                + WorldTints.LeastSeparation.ToString("0.##", CultureInfo.InvariantCulture)
                + ":1 darker than every surface style, so a greyscale frame keeps its silhouettes",
                "the closest pairing is " + pairing + " at "
                + worst.ToString("0.###", CultureInfo.InvariantCulture) + ":1");
        }

        static string CastShot(PartStyle style)
        {
            return "dev/scratch/t-141-" + style.ToString().ToLowerInvariant() + ".png";
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
