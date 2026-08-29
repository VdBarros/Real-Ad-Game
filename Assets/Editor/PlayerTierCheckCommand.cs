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

namespace Game.EditorTooling
{
    public static class PlayerTierCheckCommand
    {
        const long Seed = 20250824L;

        const float Epsilon = DungeonPack.BoundsEpsilon;

        const float AngleEpsilon = 0.01f;

        const float PortraitSize = 2.2f;

        const string BaseColour = "_BaseColor";

        const string BaseMap = "_BaseMap";

        const string LevelPath = "dev/scratch/t-32-player-level.png";

        const string PortraitPath = "dev/scratch/t-32-player-tier-";

        const string SilhouettePath = "dev/scratch/t-132-player-silhouette-";

        const float ShapeStep = 0.04f;

        const float ShapeSpread = 0.15f;

        static readonly int[] Climb = { 9, 40, 140, 420 };

        static readonly PartStyle[] StillPrimitive =
        {
            PartStyle.Pillar, PartStyle.Trail, PartStyle.Spark
        };

        public static void Check()
        {
            Wipe(LevelPath);

            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                Wipe(PortraitPath + tier + ".png");
                Wipe(SilhouettePath + tier + ".png");
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var warnings = new List<string>();
            Application.LogCallback watcher = (message, trace, type) =>
            {
                if (message != null && (message.Contains("falls back") || message.Contains("resolves to nothing")))
                {
                    warnings.Add(type + ": " + message);
                }
            };
            Application.logMessageReceived += watcher;

            var graph = LevelGenerator.Generate(Seed, MazePreset.Ship).Graph;
            var rig = CameraRig.Raise();
            var lens = rig.GetComponent<Camera>();
            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.backgroundColor = new Color(0.06f, 0.07f, 0.09f);

            var builder = new WorldBuilder();
            var root = builder.Build(graph);
            var power = builder.PlayerBadge;
            var player = root.GetComponentInChildren<PlayerFigure>(true);
            var enemies = root.GetComponentsInChildren<EnemyFigure>(true);
            var site = DeathSite(graph);
            var worn = CharacterCast.MeshOf(PartStyle.Start);
            var pack = PackMesh.Of(
                worn == PartModel.None ? null : Resources.Load<GameObject>(WorldModels.AssetPathOf(worn)));

            PreviewFilm.Sun();
            rig.Begin(graph);
            rig.Skip();
            PreviewFilm.Warm(lens);
            PreviewFilm.Shoot(lens, LevelPath);

            var report = new StringBuilder("t-32 the player figure wears a character mesh, ship seed ")
                .Append(Seed.ToString(CultureInfo.InvariantCulture))
                .Append(", ")
                .Append(enemies.Length.ToString(CultureInfo.InvariantCulture))
                .Append(" enemies:");
            var failures = 0;

            failures += WearsTheCastMesh(player, worn, pack, report);
            failures += MeasuredAgainstThePinnedFootprint(worn, pack, report);
            failures += FittedToTheTile(player, worn, pack, report);
            failures += HidesLittleGround(player, worn, pack, report);
            failures += EverythingElseStillFallsBack(root, graph, report);
            failures += LooksUpEachMeshOnce(report);

            Portrait(rig, lens, root, player, pack, power.Look.Tier);

            report.Append("\n  tier climb:").Append(Row(power, player, enemies, pack));
            var heights = new List<float> { Standing(player, pack) };
            var hides = new List<float> { Hiding(player, pack) };
            var tints = new List<Color> { Painted(player) };
            var overrides = new List<int> { Overrides(player) };
            var states = new List<Kitted> { Kit(player) };
            var opening = power.Power;

            foreach (var target in Climb)
            {
                power.DropWeaponFrom(site);
                report.Append(Walk(power, player, target)).Append(Row(power, player, enemies, pack));
                heights.Add(Standing(player, pack));
                hides.Add(Hiding(player, pack));
                tints.Add(Painted(player));
                overrides.Add(Overrides(player));
                states.Add(Kit(player));
                Portrait(rig, lens, root, player, pack, power.Look.Tier);
            }

            failures += TheTierStillReads(worn, heights, hides, tints, report);
            failures += TheMeshKeepsItsPackTexture(player, tints, overrides, report);
            failures += EachTierWearsAKitOfItsOwn(states, report);
            failures += TheSilhouetteChangesShapeAndNotOnlySize(states, report);
            failures += ThePropsComeOffTheWayTheyWentOn(power, player, opening, report);

            Application.logMessageReceived -= watcher;

            failures += Assert(
                report,
                warnings.Count == 0,
                "nothing fell back to a primitive while the world was built and the player climbed "
                + "every tier, so the warn-once path stayed silent",
                warnings.Count == 0
                    ? "the model cache logged no fallback"
                    : string.Join(" | ", warnings.ToArray()));

            failures += NoPropOutlivesTheLevelItWasPlantedIn(root, report);

            report.Append("\n  t-32: ")
                .Append(failures == 0
                    ? "every assertion above held"
                    : failures + (failures == 1 ? " assertion" : " assertions") + " above failed");

            Debug.Log(report.ToString());

            if (failures > 0)
            {
                Debug.LogError(
                    "The player tier check failed " + failures + " assertions. Read the report above.");
            }

            builder.Dispose();
        }

        static int WearsTheCastMesh(
            PlayerFigure player, PartModel worn, ICollection<Mesh> pack, StringBuilder report)
        {
            var failures = 0;

            failures += Assert(
                report,
                player != null,
                "the world raised a player figure",
                player == null ? "it raised none" : player.name);

            if (player == null)
            {
                return failures;
            }

            failures += Assert(
                report,
                worn != PartModel.None,
                "the cast names a mesh for the player rather than leaving it on its primitive",
                PartStyle.Start + " wants " + worn);

            var skins = player.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var filters = player.GetComponentsInChildren<MeshFilter>(true);
            var bones = 0;
            var rooted = 0;
            var meshes = new List<string>();

            foreach (var skin in skins)
            {
                bones += skin.bones == null ? 0 : skin.bones.Length;
                rooted += skin.rootBone == null ? 0 : 1;

                if (skin.sharedMesh != null)
                {
                    meshes.Add(skin.sharedMesh.name);
                }
            }

            failures += Assert(
                report,
                skins.Length > 0 && meshes.Count == skins.Length,
                "the player renders a skinned mesh rather than a primitive capsule",
                skins.Length + " skinned renderers wearing " + string.Join(", ", meshes.ToArray())
                + ", " + filters.Length + " mesh filters left");

            failures += Assert(
                report,
                skins.Length > 0 && bones > 0 && rooted == skins.Length,
                "the skinned mesh is rigged, so every skin is driven by bones from one root",
                bones + " bone bindings across " + skins.Length + " skins, " + rooted + " rooted");

            failures += Assert(
                report,
                CharacterDress.Bare(player.gameObject) == 0,
                "the pack's slot accessories are stripped, so the player carries no spare swords or shields",
                filters.Length + " static meshes remain, the helmet and cape the pack bolts to its bones");

            var renderers = player.GetComponentsInChildren<Renderer>(true);
            var wearing = 0;
            var stranger = new List<string>();

            foreach (var renderer in renderers)
            {
                var mesh = PackMesh.On(renderer);

                if (mesh != null && pack.Contains(mesh))
                {
                    wearing++;
                }
                else
                {
                    stranger.Add(renderer.name + " wearing " + (mesh == null ? "nothing" : mesh.name));
                }
            }

            failures += Assert(
                report,
                wearing > 0 && stranger.Count == 0,
                "every mesh the player wears comes from the " + worn + " asset the model table names",
                wearing + " of " + renderers.Length + " renderers do, from Resources/"
                + (WorldModels.AssetPathOf(worn) ?? "nothing")
                + (stranger.Count == 0 ? "" : "; strangers: " + string.Join(", ", stranger.ToArray())));

            var dressed = 0;

            foreach (var renderer in renderers)
            {
                var material = renderer.sharedMaterial;
                if (material != null
                    && material.name == WorldMaterials.NamePrefix + PartStyle.Start
                    && material.HasProperty(BaseMap)
                    && material.GetTexture(BaseMap) != null)
                {
                    dressed++;
                }
            }

            failures += Assert(
                report,
                renderers.Length > 0 && dressed == renderers.Length,
                "every mesh of the player wears the one world material bound to the adventurers atlas",
                dressed + " of " + renderers.Length + " do");

            var forward = IsoProjection.CameraForward;
            var toward = (float)((Math.Atan2(-forward.X, -forward.Z) * 180.0 / Math.PI + 360.0) % 360.0);

            failures += Assert(
                report,
                Math.Abs(Mathf.DeltaAngle(player.transform.localEulerAngles.y, toward)) <= AngleEpsilon
                && Math.Abs(Mathf.DeltaAngle(AdventurerPack.Facing, toward)) <= AngleEpsilon,
                "the figure turns its face along the line back to the camera the projection puts it on",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "it faces {0:0.###} at a pinned {1:0.###} against the {2:0.###} the camera sits on",
                    player.transform.localEulerAngles.y,
                    AdventurerPack.Facing,
                    toward));

            return failures;
        }

        static int MeasuredAgainstThePinnedFootprint(
            PartModel worn, ICollection<Mesh> pack, StringBuilder report)
        {
            var path = worn == PartModel.None ? null : WorldModels.AssetPathOf(worn);
            var prefab = path == null ? null : Resources.Load<GameObject>(path);


            if (prefab == null)
            {
                return Assert(
                    report,
                    false,
                    "the asset the model table names measures the footprint the pack constants pin",
                    "Resources/" + (path ?? "nothing") + " loads nothing to measure");
            }

            var box = PackMesh.Bare(prefab);

            return Assert(
                report,
                Math.Abs(box.size.x - AdventurerPack.WidthOf(worn)) <= Epsilon
                && Math.Abs(box.size.z - AdventurerPack.DepthOf(worn)) <= Epsilon
                && Math.Abs(box.size.y - AdventurerPack.HeightOf(worn)) <= Epsilon
                && Math.Abs(box.min.y - AdventurerPack.BaseOf(worn)) <= Epsilon,
                "the asset the model table names measures the footprint the pack constants pin, "
                + "unrotated and unscaled",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "it measures {0:0.#####} by {1:0.#####} by {2:0.#####} with a base at {3:0.#####}, "
                    + "against the pinned {4:0.#####} by {5:0.#####} by {6:0.#####} and {7:0.#####}",
                    box.size.x,
                    box.size.y,
                    box.size.z,
                    box.min.y,
                    AdventurerPack.WidthOf(worn),
                    AdventurerPack.HeightOf(worn),
                    AdventurerPack.DepthOf(worn),
                    AdventurerPack.BaseOf(worn)));
        }

        static int FittedToTheTile(
            PlayerFigure player, PartModel worn, ICollection<Mesh> pack, StringBuilder report)
        {
            if (player == null || worn == PartModel.None)
            {
                return Assert(
                    report,
                    false,
                    "the player mesh is fitted to the tile grid",
                    player == null ? "there is no figure" : "the figure wears no mesh to measure");
            }

            var box = PackMesh.Wearing(player.transform, pack);
            var scale = LevelBlueprintBuilder.FigureScale;
            var wanted = FigureFit.StandingHeight(worn, scale);
            var ground = player.Ground;
            var failures = 0;

            failures += Assert(
                report,
                Math.Abs(box.size.y - wanted) <= Epsilon,
                "the imported figure stands the "
                + AdventurerPack.PackHeightOf(worn).ToString("0.####", CultureInfo.InvariantCulture)
                + " pinned pack units the fit asks for",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "it measures {0:0.#####} against {1:0.#####}, which is {2:0.###} of the capsule's "
                    + "{3:0.#####} at the same figure scale",
                    box.size.y,
                    wanted,
                    box.size.y / FigureFit.StandingHeight(PartModel.None, scale),
                    FigureFit.StandingHeight(PartModel.None, scale)));

            failures += Assert(
                report,
                Math.Abs(box.min.y - (ground.Y + AdventurerPack.BaseOf(worn) * ScaleOn(player))) <= Epsilon,
                "the figure's measured base sits where the pinned pack base puts it above the tile top",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "its bounds start {0:0.#####} above the tile top of {1:0.#####}, against the pinned "
                    + "{2:0.#####} the pack authored below its own feet",
                    box.min.y - ground.Y,
                    ground.Y,
                    AdventurerPack.BaseOf(worn) * ScaleOn(player)));

            var half = IsoProjection.TileEdge * 0.5f;

            failures += Assert(
                report,
                box.min.x >= ground.X - half - Epsilon
                && box.max.x <= ground.X + half + Epsilon
                && box.min.z >= ground.Z - half - Epsilon
                && box.max.z <= ground.Z + half + Epsilon,
                "the figure's whole footprint lies inside the square of the tile it stands on",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "it covers x {0:0.#####} to {1:0.#####} and z {2:0.#####} to {3:0.#####} inside a tile "
                    + "spanning x {4:0.#####} to {5:0.#####} and z {6:0.#####} to {7:0.#####}",
                    box.min.x,
                    box.max.x,
                    box.min.z,
                    box.max.z,
                    ground.X - half,
                    ground.X + half,
                    ground.Z - half,
                    ground.Z + half));

            var spread = Math.Max(box.size.x, box.size.z);

            failures += Assert(
                report,
                spread <= FigureFit.SpreadOf(worn, scale) + Epsilon,
                "the figure spreads no wider than the pinned pack footprint allows it to at any yaw",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "it spans {0:0.#####} by {1:0.#####} at its {2:0.#} degree yaw, inside the {3:0.#####} "
                    + "diagonal of the pinned {4:0.#####} by {5:0.#####} footprint",
                    box.size.x,
                    box.size.z,
                    AdventurerPack.Facing,
                    FigureFit.SpreadOf(worn, scale),
                    FigureFit.WidthOf(worn, scale),
                    FigureFit.DepthOf(worn, scale)));

            return failures;
        }

        static int HidesLittleGround(
            PlayerFigure player, PartModel worn, ICollection<Mesh> pack, StringBuilder report)
        {
            if (player == null || worn == PartModel.None)
            {
                return Assert(
                    report,
                    false,
                    "the player mesh hides little ground",
                    player == null ? "there is no figure" : "the figure wears no mesh to measure");
            }

            var box = PackMesh.Wearing(player.transform, pack);
            var scale = LevelBlueprintBuilder.FigureScale;
            var depth = IsoProjection.SightReach(box.size.y);
            var hidden = box.size.x * depth;
            var capsule = FigureFit.HiddenGroundOf(PartModel.None, scale);
            var bound = IsoProjection.TileEdge * IsoProjection.OcclusionBound * IsoProjection.TileEdge;
            var parapet = IsoProjection.TileEdge
                * IsoProjection.SightReach(DungeonPack.HeightOf(PartModel.WallPanel));
            var failures = 0;

            failures += Assert(
                report,
                hidden <= FigureFit.HiddenSpreadOf(worn, scale) + Epsilon,
                "the measured ground the figure hides stays inside what the pure fit guarantees",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "measured {0:0.#####} against the {1:0.#####} the fit allows at any yaw and the "
                    + "{2:0.#####} it predicts face on",
                    hidden,
                    FigureFit.HiddenSpreadOf(worn, scale),
                    FigureFit.HiddenGroundOf(worn, scale)));

            failures += Assert(
                report,
                hidden <= bound,
                "the figure hides at most " + IsoProjection.OcclusionBound.ToString(
                    "0.##", CultureInfo.InvariantCulture)
                + " of a tile of ground, which is the wall work's bound read as an area rather than a "
                + "depth, because a wall spans a whole tile edge and a figure spans a fraction of one",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "standing {0:0.#####} tall and {1:0.#####} wide it hides {2:0.#####} tiles of ground "
                    + "at the camera's {3:0.#} degree pitch, {4:0.#####} deep, against the bound of "
                    + "{5:0.#####} and the {6:0.#####} a parapet hides across its whole edge",
                    box.size.y,
                    box.size.x,
                    hidden,
                    IsoProjection.CameraPitch,
                    depth,
                    bound,
                    parapet));

            failures += Assert(
                report,
                hidden <= capsule && depth <= IsoProjection.SightReach(
                    FigureFit.StandingHeight(PartModel.None, scale)),
                "the figure hides no more ground than the capsule it replaces, in depth or in area",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:0.#####} tiles against the capsule's {1:0.#####}, reaching {2:0.#####} back "
                    + "against the capsule's {3:0.#####}",
                    hidden,
                    capsule,
                    depth,
                    IsoProjection.SightReach(FigureFit.StandingHeight(PartModel.None, scale))));

            return failures;
        }

        static int EverythingElseStillFallsBack(GameObject root, LevelGraph graph, StringBuilder report)
        {
            var failures = 0;
            var named = new List<string>();

            foreach (var style in StillPrimitive)
            {
                named.Add(style + " wants " + PartModels.Of(style));
            }

            var still = 0;

            foreach (var style in StillPrimitive)
            {
                if (PartModels.Of(style) == PartModel.None)
                {
                    still++;
                }
            }

            failures += Assert(
                report,
                still == StillPrimitive.Length,
                "every style this ticket did not touch still wants no mesh",
                string.Join(", ", named.ToArray()));

            var byName = new Dictionary<string, Transform>();
            foreach (var node in root.GetComponentsInChildren<Transform>(true))
            {
                byName[node.name] = node;
            }

            var adversaries = 0;
            var dressed = 0;
            var complaint = new List<string>();

            foreach (var node in graph.Decisions.Nodes)
            {
                if (node.Type != NodeType.Enemy && node.Type != NodeType.Boss)
                {
                    continue;
                }

                WorldPart prop;
                if (!LevelBlueprintBuilder.TryProp(node, out prop))
                {
                    continue;
                }

                Transform instance;
                if (!byName.TryGetValue(prop.Name, out instance))
                {
                    complaint.Add(prop.Name + " is not in the world");
                    continue;
                }

                adversaries++;

                var filter = instance.GetComponent<MeshFilter>();
                var skinned = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

                if (skinned.Length > 0 && filter == null)
                {
                    dressed++;
                }
                else if (complaint.Count < 6)
                {
                    complaint.Add(prop.Name + " still stands as a primitive");
                }
            }

            failures += Assert(
                report,
                adversaries > 0 && dressed == adversaries,
                "every enemy and the boss stands as a rigged cast mesh rather than the primitive "
                + "their part shape names",
                dressed + " of " + adversaries + " do"
                + (complaint.Count == 0 ? "" : "; " + string.Join("; ", complaint.ToArray())));

            return failures;
        }

        static int LooksUpEachMeshOnce(StringBuilder report)
        {
            var asked = new List<string>();
            var cached = 0;
            var models = 0;

            using (var cache = new WorldModels())
            {
                foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
                {
                    if (model == PartModel.None)
                    {
                        continue;
                    }

                    models++;
                    var first = cache.Of(model);
                    var second = cache.Of(model);

                    if (first == null)
                    {
                        asked.Add(model + " resolved to nothing at all");
                    }
                    else if (!ReferenceEquals(first, second))
                    {
                        asked.Add(model + " answered a different object the second time");
                    }
                    else
                    {
                        cached++;
                    }
                }
            }

            return Assert(
                report,
                models > 0 && cached == models,
                "the model cache resolves each mesh once and hands back the same object after, "
                + "so a missing one would warn once rather than once per part",
                cached + " of " + models + " do"
                + (asked.Count == 0 ? "" : "; " + string.Join("; ", asked.ToArray())));
        }

        static int TheTierStillReads(
            PartModel worn,
            IReadOnlyList<float> heights,
            IReadOnlyList<float> hides,
            IReadOnlyList<Color> tints,
            StringBuilder report)
        {
            var failures = 0;
            var grew = 0;
            var repainted = 0;
            var stepped = new List<string>();

            for (var step = 1; step < heights.Count; step++)
            {
                var wanted = heights[step - 1] * PlayerLook.Growth;

                if (Math.Abs(heights[step] - wanted) <= Epsilon)
                {
                    grew++;
                }
                else
                {
                    stepped.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "tier {0} stands {1:0.#####} against {2:0.#####}",
                        step,
                        heights[step],
                        wanted));
                }

                if (tints[step] != tints[step - 1])
                {
                    repainted++;
                }
            }

            failures += Assert(
                report,
                heights.Count == PlayerTier.Count && heights[0] > 0f && grew == heights.Count - 1,
                "the mesh grows by the tier seam's own step of "
                + PlayerLook.Growth.ToString("0.##", CultureInfo.InvariantCulture) + " at every tier",
                grew + " of " + (heights.Count - 1) + " steps do"
                + (stepped.Count == 0 ? "" : "; " + string.Join("; ", stepped.ToArray())));

            failures += Assert(
                report,
                tints.Count > 1 && repainted == 0,
                "the mesh keeps the colour it opened on at every tier, so the tier seam moves the size "
                + "and nothing else",
                repainted + " of " + (tints.Count - 1) + " steps repainted it");

            failures += Assert(
                report,
                heights[heights.Count - 1] > 0f && Math.Abs(heights[heights.Count - 1]
                    - FigureFit.StandingHeight(worn, PlayerLook.Of(Climb[Climb.Length - 1]).Scale)) <= Epsilon,
                "the topmost tier stands exactly as tall as the pure fit says it should",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "it measures {0:0.#####} against {1:0.#####}",
                    heights[heights.Count - 1],
                    FigureFit.StandingHeight(worn, PlayerLook.Of(Climb[Climb.Length - 1]).Scale)));

            var kinder = 0;
            var greedy = new List<string>();
            var looks = new List<float> { LevelBlueprintBuilder.FigureScale };

            foreach (var target in Climb)
            {
                looks.Add(PlayerLook.Of(target).Scale);
            }

            for (var tier = 0; tier < hides.Count && tier < looks.Count; tier++)
            {
                var capsule = FigureFit.HiddenGroundOf(PartModel.None, looks[tier]);

                if (hides[tier] <= capsule)
                {
                    kinder++;
                }
                else
                {
                    greedy.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "tier {0} hides {1:0.#####} against the capsule's {2:0.#####}",
                        tier,
                        hides[tier],
                        capsule));
                }
            }

            failures += Assert(
                report,
                hides.Count > 0 && hides[0] > 0f && kinder == hides.Count,
                "the mesh hides less ground than the capsule would at every tier the seam grows it to",
                kinder + " of " + hides.Count + " tiers do"
                + (greedy.Count == 0 ? "" : "; " + string.Join("; ", greedy.ToArray())));

            return failures;
        }

        struct Kitted
        {
            public PlayerWeapon Weapon;

            public bool Cloak;

            public int Trophies;

            public int Props;

            public int Dressed;

            public float Height;

            public float Breadth;

            public float Aspect
            {
                get { return Height <= 0f ? 0f : Breadth / Height; }
            }

            public override string ToString()
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}{1} over {2} trophies in {3} objects, {4:0.####} by {5:0.####} at aspect {6:0.####}",
                    Weapon,
                    Cloak ? " cloaked" : " uncloaked",
                    Trophies,
                    Props,
                    Breadth,
                    Height,
                    Aspect);
            }
        }

        static Kitted Kit(PlayerFigure player)
        {
            var kit = new Kitted();

            if (player == null)
            {
                return kit;
            }

            kit.Weapon = player.Gripping;
            kit.Cloak = player.IsCloaked;
            kit.Trophies = player.Carrying;
            kit.Props = Planted(player);
            kit.Dressed = Wearing(player);

            var box = Silhouette(player);
            kit.Height = box.size.y;
            kit.Breadth = Math.Max(box.size.x, box.size.z);

            return kit;
        }

        static Bounds Silhouette(PlayerFigure player)
        {
            var box = new Bounds();
            var first = true;

            foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
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

        static int Planted(PlayerFigure player)
        {
            var found = 0;

            foreach (var node in player.GetComponentsInChildren<Transform>(true))
            {
                if (PartNames.IsWorn(node.name))
                {
                    found++;
                }
            }

            return found;
        }

        static int Wearing(PlayerFigure player)
        {
            var found = 0;

            foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
            {
                if (Dressed(renderer))
                {
                    found++;
                }
            }

            return found;
        }

        static int WantedProps(int tier)
        {
            var wanted = PlayerLook.Of(PowerAt(tier)).Trophies;
            var weapon = PlayerKit.WeaponOf(tier);

            if (weapon != PlayerWeapon.None)
            {
                wanted += 1 + PlayerKit.LimbsOf(weapon).Count;
            }

            if (PlayerKit.CloakedAt(tier))
            {
                wanted += 1 + PlayerKit.CloakLimbs.Count;
            }

            return wanted;
        }

        static int PowerAt(int tier)
        {
            return tier == 0 ? 1 : PlayerTier.Thresholds[tier - 1];
        }

        static int EachTierWearsAKitOfItsOwn(IReadOnlyList<Kitted> states, StringBuilder report)
        {
            var failures = 0;
            var matched = 0;
            var strayed = new List<string>();

            report.Append("\n  kits:");

            for (var tier = 0; tier < states.Count; tier++)
            {
                report.Append("\n    tier ").Append(tier).Append(": ").Append(states[tier].ToString());
            }

            for (var tier = 0; tier < states.Count; tier++)
            {
                var wanted = PlayerKit.WeaponOf(tier);
                var cloaked = PlayerKit.CloakedAt(tier);
                var props = WantedProps(tier);

                if (states[tier].Weapon == wanted
                    && states[tier].Cloak == cloaked
                    && states[tier].Props == props)
                {
                    matched++;
                }
                else
                {
                    strayed.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "tier {0} wears {1} against the {2}{3} in {4} objects the kit calls for",
                        tier,
                        states[tier],
                        wanted,
                        cloaked ? " cloaked" : " uncloaked",
                        props));
                }
            }

            failures += Assert(
                report,
                states.Count == PlayerTier.Count && matched == states.Count,
                "every tier the climb crosses into wears exactly the weapon, cloak and trophies its "
                + "threshold dresses it in, in exactly that many objects and no spares",
                matched + " of " + states.Count + " do"
                + (strayed.Count == 0 ? "" : "; " + string.Join("; ", strayed.ToArray())));

            var distinct = 0;
            var twinned = new List<string>();

            for (var tier = 0; tier < states.Count; tier++)
            {
                var same = false;

                for (var other = 0; other < tier; other++)
                {
                    if (states[other].Weapon == states[tier].Weapon
                        && states[other].Cloak == states[tier].Cloak
                        && states[other].Trophies == states[tier].Trophies)
                    {
                        same = true;
                        twinned.Add("tier " + tier + " reads as tier " + other);
                    }
                }

                if (!same)
                {
                    distinct++;
                }
            }

            failures += Assert(
                report,
                distinct >= 4 && distinct == states.Count,
                "the climb passes through at least four states no two of which carry the same weapon, "
                + "cloak and trophy count",
                distinct + " of " + states.Count + " are their own"
                + (twinned.Count == 0 ? "" : "; " + string.Join("; ", twinned.ToArray())));

            var held = 0;
            var carried = new List<PlayerWeapon>();

            for (var tier = 0; tier < states.Count; tier++)
            {
                if (states[tier].Weapon != PlayerWeapon.None && !carried.Contains(states[tier].Weapon))
                {
                    carried.Add(states[tier].Weapon);
                    held++;
                }
            }

            failures += Assert(
                report,
                held >= 3 && held <= 4,
                "the climb swaps between three or four weapon props rather than growing one",
                held + " weapons: " + string.Join(", ", Named(carried)));

            var steady = 0;

            for (var tier = 1; tier < states.Count; tier++)
            {
                if (states[tier].Dressed == states[0].Dressed)
                {
                    steady++;
                }
            }

            failures += Assert(
                report,
                states.Count > 1 && steady == states.Count - 1 && states[0].Dressed > 0,
                "swapping a prop adds no renderer to the body itself, so the count of meshes wearing the "
                + "pack material is the same at every tier and nothing is layered over the figure",
                steady + " of " + (states.Count - 1) + " tiers keep the opening " + states[0].Dressed);

            return failures;
        }

        static int TheSilhouetteChangesShapeAndNotOnlySize(
            IReadOnlyList<Kitted> states, StringBuilder report)
        {
            var failures = 0;
            var reshaped = 0;
            var flat = new List<string>();

            for (var tier = 1; tier < states.Count; tier++)
            {
                var change = Math.Abs(states[tier].Aspect - states[tier - 1].Aspect);

                if (change > ShapeStep)
                {
                    reshaped++;
                }
                else
                {
                    flat.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "tier {0} reads {1:0.####} against the {2:0.####} of tier {3}",
                        tier,
                        states[tier].Aspect,
                        states[tier - 1].Aspect,
                        tier - 1));
                }
            }

            failures += Assert(
                report,
                states.Count > 1 && reshaped == states.Count - 1,
                "every promotion changes the outline's proportions by more than "
                + ShapeStep.ToString("0.##", CultureInfo.InvariantCulture)
                + ", so a tier is not just a scaled copy of the one below it, which uniform growth alone "
                + "would leave it as",
                reshaped + " of " + (states.Count - 1) + " promotions do"
                + (flat.Count == 0 ? "" : "; " + string.Join("; ", flat.ToArray())));

            var widest = 0f;
            var apart = "nothing to compare";

            for (var tier = 0; tier < states.Count; tier++)
            {
                for (var other = 0; other < tier; other++)
                {
                    var gap = Math.Abs(states[tier].Aspect - states[other].Aspect);

                    if (gap > widest)
                    {
                        widest = gap;
                        apart = string.Format(
                            CultureInfo.InvariantCulture,
                            "tier {0} at {1:0.####} against tier {2} at {3:0.####}",
                            tier,
                            states[tier].Aspect,
                            other,
                            states[other].Aspect);
                    }
                }
            }

            failures += Assert(
                report,
                widest > ShapeSpread,
                "two of the states stand apart in outline proportion by more than "
                + ShapeSpread.ToString("0.##", CultureInfo.InvariantCulture)
                + ", which is the silhouette reading the badges cannot supply",
                string.Format(CultureInfo.InvariantCulture, "{0:0.####} apart at their widest, {1}", widest, apart));

            var grew = 0;

            for (var tier = 1; tier < states.Count; tier++)
            {
                if (states[tier].Height > states[tier - 1].Height)
                {
                    grew++;
                }
            }

            failures += Assert(
                report,
                states.Count > 1 && grew == states.Count - 1,
                "the whole silhouette, props included, still stands taller at every tier than at the one "
                + "below it",
                grew + " of " + (states.Count - 1) + " do");

            return failures;
        }

        static int ThePropsComeOffTheWayTheyWentOn(
            PowerBadge power, PlayerFigure player, int opening, StringBuilder report)
        {
            if (player == null)
            {
                return Assert(report, false, "the props come off again", "there is no figure to strip");
            }

            var top = Kit(player);
            PowerPump.Settle(power, opening);
            var back = Kit(player);
            PowerPump.Settle(power, Climb[Climb.Length - 1]);
            var again = Kit(player);

            var failures = 0;

            failures += Assert(
                report,
                back.Weapon == PlayerKit.WeaponOf(0)
                && back.Cloak == PlayerKit.CloakedAt(0)
                && back.Props == WantedProps(0),
                "falling back to the opening power destroys every prop the climb planted rather than "
                + "leaving them hanging on the figure",
                "it reads " + back + " against the " + WantedProps(0) + " objects tier 0 wants");

            failures += Assert(
                report,
                again.Weapon == top.Weapon && again.Cloak == top.Cloak && again.Props == top.Props,
                "climbing the same tier a second time plants the same props again rather than doubling "
                + "them up",
                "it reads " + again + " against the opening climb's " + top);

            return failures;
        }

        static int NoPropOutlivesTheLevelItWasPlantedIn(GameObject root, StringBuilder report)
        {
            var standing = 0;

            foreach (var node in root.GetComponentsInChildren<Transform>(true))
            {
                if (PartNames.IsWorn(node.name))
                {
                    standing++;
                }
            }

            WorldObjects.Destroy(root);

            var left = new List<string>();

            foreach (var survivor in UnityEngine.SceneManagement.SceneManager
                .GetActiveScene().GetRootGameObjects())
            {
                foreach (var node in survivor.GetComponentsInChildren<Transform>(true))
                {
                    if (PartNames.IsWorn(node.name) && left.Count < 8)
                    {
                        left.Add(node.name + " under " + survivor.name);
                    }
                }
            }

            return Assert(
                report,
                standing > 0 && left.Count == 0,
                "tearing the level down takes every prop with it, so nothing the climb planted survives "
                + "into the level that replaces it",
                standing + " props stood in the level and " + left.Count + " outlived it"
                + (left.Count == 0 ? "" : ": " + string.Join(", ", left.ToArray())));
        }

        static string[] Named(IReadOnlyList<PlayerWeapon> weapons)
        {
            var named = new string[weapons.Count];

            for (var slot = 0; slot < weapons.Count; slot++)
            {
                named[slot] = weapons[slot].ToString();
            }

            return named;
        }

        static void Portrait(
            CameraRig rig,
            Camera lens,
            GameObject root,
            PlayerFigure player,
            ICollection<Mesh> pack,
            int tier)
        {
            if (player == null)
            {
                return;
            }

            var ground = player.Ground;
            var standing = Standing(player, pack);
            var size = (standing > 0f ? standing : IsoProjection.TileEdge) * PortraitSize;
            rig.Hold(new CameraFraming(
                new WorldPoint(ground.X, ground.Y + size * 0.25f, ground.Z), size));
            PreviewFilm.Shoot(lens, PortraitPath + tier + ".png");

            var badges = Badges(root);

            foreach (var group in badges)
            {
                group.SetActive(false);
            }

            PreviewFilm.Shoot(lens, SilhouettePath + tier + ".png");

            foreach (var group in badges)
            {
                group.SetActive(true);
            }
        }

        static List<GameObject> Badges(GameObject root)
        {
            var groups = new List<GameObject>();

            if (root == null)
            {
                return groups;
            }

            foreach (var node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == PartNames.BadgesGroup && node.gameObject.activeSelf)
                {
                    groups.Add(node.gameObject);
                }
            }

            return groups;
        }

        static float Standing(PlayerFigure player, ICollection<Mesh> pack)
        {
            return player == null ? 0f : PackMesh.Wearing(player.transform, pack).size.y;
        }

        static float Hiding(PlayerFigure player, ICollection<Mesh> pack)
        {
            if (player == null)
            {
                return 0f;
            }

            var box = PackMesh.Wearing(player.transform, pack);

            return Math.Max(box.size.x, box.size.z) * IsoProjection.SightReach(box.size.y);
        }

        static int TheMeshKeepsItsPackTexture(
            PlayerFigure player,
            IReadOnlyList<Color> tints,
            IReadOnlyList<int> overrides,
            StringBuilder report)
        {
            var skin = Skin(player);
            var atlas = skin != null && skin.HasProperty(BaseMap) ? skin.GetTexture(BaseMap) : null;
            var overridden = 0;

            for (var step = 0; step < overrides.Count; step++)
            {
                overridden += overrides[step];
            }

            var flat = 0;

            for (var step = 0; step < tints.Count; step++)
            {
                if (tints[step] == Color.white)
                {
                    flat++;
                }
            }

            var failures = 0;

            failures += Assert(
                report,
                skin != null && atlas != null,
                "the player's mesh wears one material bound to the adventurers atlas, so what shows is the "
                + "pack's own texture",
                skin == null
                    ? "the player carries no skinned renderer"
                    : "it wears " + skin.name + " bound to "
                        + (atlas == null ? "no texture at all" : atlas.name));

            failures += Assert(
                report,
                tints.Count > 0 && flat == tints.Count,
                "that material multiplies the atlas by white at every tier rather than by a palette colour, "
                + "so nothing tints the texture away",
                flat + " of " + tints.Count + " readings are white; they read "
                + string.Join(", ", Readings(tints)));

            failures += Assert(
                report,
                overridden == 0,
                "no renderer wearing that material carries a property block at any tier, so no second "
                + "colour is laid over the texture either",
                overridden + " overrides across " + overrides.Count + " tiers");

            int tinted;
            var carried = Trophies(player, out tinted);
            var wanted = PlayerLook.Of(Climb[Climb.Length - 1]).Trophies;

            failures += Assert(
                report,
                carried == wanted && tinted == carried && carried > 0,
                "the trophy primitives hanging off the same figure are still washed with the steel tint "
                + "they have always worn, because a primitive carries no texture for a tint to hide",
                tinted + " of " + carried + " are, against the " + wanted + " the top tier carries");

            int washed;
            int textured;
            var props = Kitting(player, out washed, out textured);

            failures += Assert(
                report,
                props > 0 && washed == props && textured == 0,
                "the weapon and cloak the top tier wears are washed primitives of their own, carrying no "
                + "texture the wash could be hiding, so the progression is a prop and never a layer of "
                + "colour over the body",
                washed + " of " + props + " prop meshes are washed and " + textured + " carry a texture");

            return failures;
        }

        static bool Dressed(Renderer renderer)
        {
            var material = renderer.sharedMaterial;

            return material != null
                && material.name.StartsWith(WorldMaterials.NamePrefix, StringComparison.Ordinal);
        }

        static int Kitting(PlayerFigure player, out int tinted, out int textured)
        {
            tinted = 0;
            textured = 0;

            if (player == null)
            {
                return 0;
            }

            var found = 0;

            foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
            {
                if (Dressed(renderer) || PartNames.IsTrophy(renderer.name) || !PartNames.IsWorn(renderer.name))
                {
                    continue;
                }

                found++;

                if (renderer.HasPropertyBlock())
                {
                    tinted++;
                }

                var material = renderer.sharedMaterial;

                if (material != null && material.HasProperty(BaseMap) && material.GetTexture(BaseMap) != null)
                {
                    textured++;
                }
            }

            return found;
        }

        static int Trophies(PlayerFigure player, out int tinted)
        {
            tinted = 0;

            if (player == null)
            {
                return 0;
            }

            var found = 0;

            foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
            {
                if (Dressed(renderer) || !PartNames.IsTrophy(renderer.name))
                {
                    continue;
                }

                found++;

                if (renderer.HasPropertyBlock())
                {
                    tinted++;
                }
            }

            return found;
        }

        static string[] Readings(IReadOnlyList<Color> tints)
        {
            var read = new string[tints.Count];

            for (var step = 0; step < tints.Count; step++)
            {
                read[step] = tints[step].ToString();
            }

            return read;
        }

        static SkinnedMeshRenderer Skinned(PlayerFigure player)
        {
            if (player == null)
            {
                return null;
            }

            foreach (var renderer in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                return renderer;
            }

            return null;
        }

        static Material Skin(PlayerFigure player)
        {
            var renderer = Skinned(player);

            return renderer == null ? null : renderer.sharedMaterial;
        }

        static int Overrides(PlayerFigure player)
        {
            if (player == null)
            {
                return 0;
            }

            var found = 0;

            foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
            {
                if (Dressed(renderer) && renderer.HasPropertyBlock())
                {
                    found++;
                }
            }

            return found;
        }

        static Color Painted(PlayerFigure player)
        {
            var skin = Skin(player);

            if (skin == null || !skin.HasProperty(BaseColour))
            {
                return Color.black;
            }

            return skin.GetColor(BaseColour);
        }

        static float ScaleOn(PlayerFigure player)
        {
            return player == null ? 1f : player.transform.localScale.x;
        }

        static string Walk(PowerBadge power, PlayerFigure player, int target)
        {
            var opening = player.transform.localScale.x;
            var from = power.Power;
            var landedOn = -1;
            var grewOn = -1;
            var carriedOn = -1;
            var carried = player.Carrying;

            power.Show(target);

            for (var frame = 1; frame <= PowerPump.Ceiling; frame++)
            {
                power.Advance(PowerPump.Frame);

                if (landedOn < 0 && power.HasLanded)
                {
                    landedOn = frame;
                }

                if (grewOn < 0 && player.transform.localScale.x > opening + 1e-4f)
                {
                    grewOn = frame;
                }

                if (carriedOn < 0 && player.Carrying > carried)
                {
                    carriedOn = frame;
                }

                if (power.IsSettled && !player.IsFlying)
                {
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "\n  {0} -> {1}: number landed on frame {2}, body grew from frame {3}, "
                        + "trophy planted on frame {4}, beat done on frame {5}",
                        from,
                        target,
                        landedOn,
                        grewOn,
                        carriedOn,
                        frame);
                }
            }

            return "\n  " + from + " -> " + target + ": the beat never settled";
        }

        static string Row(
            PowerBadge power,
            PlayerFigure player,
            IReadOnlyList<EnemyFigure> enemies,
            ICollection<Mesh> pack)
        {
            var counts = new int[4];
            foreach (var enemy in enemies)
            {
                counts[(int)enemy.Band]++;
            }

            var row = new StringBuilder();
            row.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  power {0} is tier {1} at scale {2:0.###} standing {3:0.###} carrying {4} ->",
                power.Power,
                power.Look.Tier,
                player.transform.localScale.x,
                Standing(player, pack),
                player.Carrying);

            for (var band = 0; band < counts.Length; band++)
            {
                row.Append(' ')
                    .Append(((EnemyBand)band).ToString())
                    .Append(' ')
                    .Append(counts[band].ToString(CultureInfo.InvariantCulture));
            }

            return row.ToString();
        }

        static WorldPoint DeathSite(LevelGraph graph)
        {
            foreach (var node in graph.Decisions.Nodes)
            {
                if (node.Type != NodeType.Enemy)
                {
                    continue;
                }

                var tile = IsoProjection.Of(node.Position);
                return new WorldPoint(tile.X, tile.Y + LevelBlueprintBuilder.FigureScale, tile.Z);
            }

            return new WorldPoint(0f, 0f, 0f);
        }

        static int Assert(StringBuilder report, bool held, string claim, string detail)
        {
            report.Append("\n  ").Append(held ? "ok   " : "FAIL ").Append(claim).Append(" - ").Append(detail);

            return held ? 0 : 1;
        }

        static void Wipe(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
