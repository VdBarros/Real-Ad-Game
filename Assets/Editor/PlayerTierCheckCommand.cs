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

        const string GuisePath = "dev/scratch/t-189-player-guise-";

        const float ShapeStep = 0.04f;

        const float ShapeSpread = 0.15f;

        const float SlotSlack = 0.05f;

        const int ClipSamples = 16;

        const float ClipTravel = 0.1f;

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

            foreach (var guise in PlayerGuises.All)
            {
                Wipe(GuisePath + guise + ".png");
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
            var worn = PlayerKit.BodyOf(PlayerKit.GuiseOf(0));
            var pack = MeshesOf(worn);

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
            failures += MeasuredAgainstThePinnedFootprint(report);
            failures += FittedToTheTile(player, worn, pack, report);
            failures += HidesLittleGround(player, worn, pack, report);
            failures += EverythingElseStillFallsBack(root, graph, report);
            failures += LooksUpEachMeshOnce(report);

            var filmed = new List<PlayerGuise>();
            Portrait(rig, lens, root, player, power.Look.Tier, filmed);

            report.Append("\n  tier climb:").Append(Row(power, player, enemies));
            var heights = new List<float> { Standing(player) };
            var hides = new List<float> { Hiding(player) };
            var tints = new List<Color> { Painted(player) };
            var overrides = new List<int> { Overrides(player) };
            var states = new List<Kitted> { Kit(player) };
            var opening = power.Power;

            foreach (var target in Climb)
            {
                power.DropWeaponFrom(site);
                report.Append(Walk(power, player, target)).Append(Row(power, player, enemies));
                heights.Add(Standing(player));
                hides.Add(Hiding(player));
                tints.Add(Painted(player));
                overrides.Add(Overrides(player));
                states.Add(Kit(player));
                Portrait(rig, lens, root, player, power.Look.Tier, filmed);
            }

            failures += TheTierStillReads(heights, hides, tints, report);
            failures += TheMeshKeepsItsPackTexture(player, tints, overrides, report);
            failures += EachTierWearsAKitOfItsOwn(states, report);
            failures += TheSilhouetteChangesShapeAndNotOnlySize(states, report);
            failures += ThePropsComeOffTheWayTheyWentOn(power, player, opening, report);
            failures += TheWeaponIsAPackMeshOnTheHand(power, player, report);
            failures += TheCloakIsThePacksOwnCloth(power, player, report);
            failures += TheFinisherSwingsWhatTheHandHolds(power, player, report);
            failures += TheWeaponSitsWhereThePackHangsItsOwn(power, player, report);
            failures += TheMountedKitMeasuresWhatTheKitTablePins(power, player, report);
            failures += TheWeaponRidesTheHandThroughEveryClip(power, player, report);
            failures += TheStowedKitWalksThroughAGate(power, player, report);
            failures += EveryTierStandsInTheBodyItsGuiseNames(power, player, report);
            failures += ASwapKeepsWhatTheHeroWasDoingAndCarrying(power, player, report);
            failures += TheDropCarriesTheWeaponTheHeroIsAboutToGrip(power, player, site, report);
            failures += AWeaponArrivingWhileTheHeroIsStowedGoesDownHisSpine(power, player, site, report);
            failures += EveryGuiseSatForAPortrait(filmed, report);

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

        static int MeasuredAgainstThePinnedFootprint(StringBuilder report)
        {
            var failures = 0;
            var measured = 0;
            var pinned = 0;
            var readings = new List<string>();

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (!AdventurerPack.Carries(model) && !WeaponsPack.Carries(model))
                {
                    continue;
                }

                measured++;
                var path = WorldModels.AssetPathOf(model);
                var prefab = path == null ? null : Resources.Load<GameObject>(path);

                if (prefab == null)
                {
                    readings.Add(model + " loads nothing from Resources/" + (path ?? "nothing"));
                    continue;
                }

                var box = PackMesh.Bare(prefab);
                var scale = ArtPacks.ImportScaleFor(model);

                if (Math.Abs(box.size.x - ArtPacks.WidthOf(model)) <= Epsilon
                    && Math.Abs(box.size.z - ArtPacks.DepthOf(model)) <= Epsilon
                    && Math.Abs(box.size.y - ArtPacks.HeightOf(model)) <= Epsilon
                    && Math.Abs(box.min.y - ArtPacks.BaseOf(model)) <= Epsilon)
                {
                    pinned++;
                    continue;
                }

                readings.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} measures {1:0.#####} by {2:0.#####} by {3:0.#####} based at {4:0.#####} against "
                    + "the pinned {5:0.#####} by {6:0.#####} by {7:0.#####} and {8:0.#####}",
                    model,
                    box.size.x / scale,
                    box.size.y / scale,
                    box.size.z / scale,
                    box.min.y / scale,
                    ArtPacks.PackWidthOf(model),
                    ArtPacks.PackHeightOf(model),
                    ArtPacks.PackDepthOf(model),
                    ArtPacks.PackBaseOf(model)));
            }

            failures += Assert(
                report,
                measured > 1 && pinned == measured,
                "every mesh the player's own packs ship measures the footprint its pack constants pin, "
                + "unrotated and unscaled",
                pinned + " of " + measured + " do"
                + (readings.Count == 0 ? "" : "; " + string.Join("; ", readings.ToArray())));

            return failures;
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

            var top = PlayerLook.Of(Climb[Climb.Length - 1]);
            var tallest = FigureFit.StandingHeight(PlayerKit.BodyOf(top.Guise), top.Scale);

            failures += Assert(
                report,
                heights[heights.Count - 1] > 0f
                && Math.Abs(heights[heights.Count - 1] - tallest) <= Epsilon,
                "the topmost tier stands exactly as tall as the pure fit says it should, whichever "
                + "guise the ramp has dressed it in by then",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "the {0} measures {1:0.#####} against {2:0.#####}",
                    top.Guise,
                    heights[heights.Count - 1],
                    tallest));

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
            return Enclosing(player.transform);
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
                if (Dressed(renderer) && !Propped(renderer.transform))
                {
                    found++;
                }
            }

            return found;
        }

        static bool Propped(Transform node)
        {
            for (var walk = node; walk != null; walk = walk.parent)
            {
                if (PartNames.IsWorn(walk.name))
                {
                    return true;
                }
            }

            return false;
        }

        static int WantedProps(int tier)
        {
            var wanted = PlayerLook.Of(PowerAt(tier)).Trophies;
            var weapon = PlayerKit.WeaponOf(tier);

            if (weapon != PlayerWeapon.None)
            {
                wanted += 1;
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
            var bare = 0;
            var opening = new Dictionary<PlayerGuise, int>();
            var layered = new List<string>();

            for (var tier = 0; tier < states.Count; tier++)
            {
                var guise = PlayerKit.GuiseOf(tier);

                if (!opening.ContainsKey(guise))
                {
                    opening.Add(guise, states[tier].Dressed);

                    if (states[tier].Dressed > 0)
                    {
                        bare++;
                    }
                }

                if (states[tier].Dressed == opening[guise])
                {
                    steady++;
                }
                else
                {
                    layered.Add("tier " + tier + " stands " + states[tier].Dressed + " meshes as the "
                        + guise + " against the " + opening[guise] + " it first stood in");
                }
            }

            var census = new List<string>();

            foreach (var guise in PlayerGuises.All)
            {
                census.Add(guise + " " + (opening.ContainsKey(guise) ? opening[guise] : 0));
            }

            failures += Assert(
                report,
                states.Count > 1 && steady == states.Count && bare == opening.Count && opening.Count > 1,
                "swapping a prop adds no renderer to the body itself, so the count of meshes wearing the "
                + "pack material is the same at every tier a guise holds and nothing is layered over the "
                + "figure; a change of body is the only thing that may change it, because each guise is "
                + "cut from a different number of pieces",
                steady + " of " + states.Count + " tiers keep the count their guise opened on ("
                + string.Join(", ", census.ToArray()) + ")"
                + (layered.Count == 0 ? "" : "; " + string.Join("; ", layered.ToArray())));

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
            int tier,
            ICollection<PlayerGuise> filmed)
        {
            if (player == null)
            {
                return;
            }

            var ground = player.Ground;
            var standing = Standing(player);
            var size = (standing > 0f ? standing : IsoProjection.TileEdge) * PortraitSize;
            rig.Hold(new CameraFraming(
                new WorldPoint(ground.X, ground.Y + size * 0.25f, ground.Z), size));
            PreviewFilm.Shoot(lens, PortraitPath + tier + ".png");

            if (!filmed.Contains(player.Wearing))
            {
                filmed.Add(player.Wearing);
                PreviewFilm.Shoot(lens, GuisePath + player.Wearing + ".png");
            }

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

        static float Standing(PlayerFigure player)
        {
            return player == null ? 0f : PackMesh.Wearing(player.transform, Worn(player)).size.y;
        }

        static float Hiding(PlayerFigure player)
        {
            if (player == null)
            {
                return 0f;
            }

            var box = PackMesh.Wearing(player.transform, Worn(player));

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

            int blocked;
            var blade = Blade(player, out blocked);

            failures += Assert(
                report,
                blade > 0 && blocked == 0,
                "the weapon in the hand shows the adventurers atlas through the same world material the "
                + "body wears, so the prop the tier ramp swaps is pack art and not a washed box",
                blade + " weapon meshes wear the atlas and " + blocked + " carry a colour over it");

            return failures;
        }

        static int Blade(PlayerFigure player, out int blocked)
        {
            blocked = 0;

            var held = player == null ? null : player.Wielding;

            if (held == null)
            {
                return 0;
            }

            var found = 0;

            foreach (var renderer in held.GetComponentsInChildren<Renderer>(true))
            {
                var material = renderer.sharedMaterial;

                if (material == null
                    || !material.HasProperty(BaseMap)
                    || material.GetTexture(BaseMap) == null
                    || !Dressed(renderer))
                {
                    continue;
                }

                found++;

                if (renderer.HasPropertyBlock())
                {
                    blocked++;
                }
            }

            return found;
        }

        static readonly Dictionary<PartModel, ISet<Mesh>> cut = new Dictionary<PartModel, ISet<Mesh>>();

        static ISet<Mesh> MeshesOf(PartModel model)
        {
            ISet<Mesh> found;

            if (cut.TryGetValue(model, out found))
            {
                return found;
            }

            var path = WorldModels.AssetPathOf(model);
            found = PackMesh.Of(path == null ? null : Resources.Load<GameObject>(path));
            cut[model] = found;

            return found;
        }

        static ISet<Mesh> Worn(PlayerFigure player)
        {
            return MeshesOf(PlayerKit.BodyOf(player.Wearing));
        }

        static ISet<Mesh> EveryGuiseMesh()
        {
            var meshes = new HashSet<Mesh>();

            foreach (var guise in PlayerGuises.All)
            {
                foreach (var mesh in MeshesOf(PlayerKit.BodyOf(guise)))
                {
                    meshes.Add(mesh);
                }
            }

            return meshes;
        }

        static ISet<Mesh> EveryWeaponMesh()
        {
            var meshes = new HashSet<Mesh>();

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (!AdventurerPack.Wields(model) && !WeaponsPack.Wields(model))
                {
                    continue;
                }

                foreach (var mesh in MeshesOf(model))
                {
                    meshes.Add(mesh);
                }
            }

            return meshes;
        }

        static Bounds Armed(PlayerFigure player, ICollection<Mesh> body, Transform held)
        {
            var box = new Bounds();
            var first = true;

            foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
            {
                var mesh = PackMesh.On(renderer);
                var mine = mesh != null
                    && Drawn(renderer)
                    && (body.Contains(mesh) || (held != null && renderer.transform.IsChildOf(held)));

                if (!mine)
                {
                    continue;
                }

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

        static Bounds Enclosing(Transform node)
        {
            var box = new Bounds();
            var first = true;

            foreach (var renderer in node.GetComponentsInChildren<Renderer>(true))
            {
                if (!Drawn(renderer))
                {
                    continue;
                }

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

        static int TheWeaponIsAPackMeshOnTheHand(
            PowerBadge power, PlayerFigure player, StringBuilder report)
        {
            if (power == null || player == null)
            {
                return Assert(
                    report, false, "the world raised a player the tier ramp can arm", "it raised none");
            }

            var body = EveryGuiseMesh();
            var blades = EveryWeaponMesh();
            var mounted = 0;
            var boxed = 0;
            var tiers = 0;
            var strayed = new List<string>();
            var slabbed = new List<string>();

            report.Append("\n  hands:");

            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                PowerPump.Settle(power, PowerAt(tier));
                tiers++;

                var weapon = PlayerKit.WeaponOf(tier);
                var held = player.Wielding;

                if (weapon == PlayerWeapon.None)
                {
                    if (held == null)
                    {
                        mounted++;
                    }
                    else
                    {
                        strayed.Add("tier " + tier + " holds " + held.name + " in an empty hand");
                    }
                }
                else if (held == null)
                {
                    strayed.Add("tier " + tier + " holds nothing where a " + weapon + " belongs");
                }
                else
                {
                    var pack = MeshesOf(PlayerKit.ModelOf(weapon));
                    var slot = held.parent;
                    var meshes = 0;
                    var strangers = 0;

                    foreach (var renderer in held.GetComponentsInChildren<Renderer>(true))
                    {
                        var mesh = PackMesh.On(renderer);

                        if (mesh != null && pack.Contains(mesh))
                        {
                            meshes++;
                        }
                        else
                        {
                            strangers++;
                        }
                    }

                    var slotted = slot != null
                        && slot.name.StartsWith(ArtPacks.CastSlotNode, StringComparison.Ordinal);

                    if (held.name == PartNames.Held(weapon) && slotted && meshes > 0 && strangers == 0)
                    {
                        mounted++;
                    }
                    else
                    {
                        strayed.Add(string.Format(
                            CultureInfo.InvariantCulture,
                            "tier {0} hangs {1} off {2} wearing {3} pack meshes and {4} strangers",
                            tier,
                            held.name,
                            slot == null ? "nothing" : slot.name,
                            meshes,
                            strangers));
                    }

                    report.Append("\n    tier ")
                        .Append(tier)
                        .Append(": ")
                        .Append(weapon)
                        .Append(" is ")
                        .Append(PlayerKit.ModelOf(weapon))
                        .Append(" of ")
                        .Append(meshes + strangers)
                        .Append(" meshes on ")
                        .Append(slot == null ? "nothing" : slot.name);
                }

                var slabs = 0;

                foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
                {
                    var mesh = PackMesh.On(renderer);

                    if (mesh == null || body.Contains(mesh) || blades.Contains(mesh))
                    {
                        continue;
                    }

                    slabs++;
                }

                var cloth = PlayerLook.Of(PowerAt(tier)).Trophies;

                if (slabs == cloth)
                {
                    boxed++;
                }
                else
                {
                    slabbed.Add("tier " + tier + " stands " + slabs + " primitives against " + cloth);
                }
            }

            var failures = 0;

            failures += Assert(
                report,
                tiers == PlayerTier.Count && mounted == tiers,
                "every tier that carries a weapon carries it as a mesh a pack that ships with the cast "
                + "hands over, hung off the rig's own hand slot, and the empty-handed tier carries "
                + "nothing at all",
                mounted + " of " + tiers + " do"
                + (strayed.Count == 0 ? "" : "; " + string.Join("; ", strayed.ToArray())));

            failures += Assert(
                report,
                tiers == PlayerTier.Count && boxed == tiers,
                "the only primitives left on the figure at any tier are the trophies #132 hangs off its "
                + "shoulders, so neither the weapon nor the cloak is a box any more",
                boxed + " of " + tiers + " tiers are"
                + (slabbed.Count == 0 ? "" : "; " + string.Join("; ", slabbed.ToArray())));

            return failures;
        }

        static int TheWeaponSitsWhereThePackHangsItsOwn(
            PowerBadge power, PlayerFigure player, StringBuilder report)
        {
            if (player == null)
            {
                return Assert(
                    report,
                    false,
                    "the pack's own slot accessories say where a weapon hangs",
                    "there is no rig to read them off");
            }

            var authored = 0;
            var squared = 0;
            var rested = 0;
            var hung = new List<string>();
            var strays = new List<string>();

            foreach (var guise in PlayerGuises.All)
            {
                var path = WorldModels.AssetPathOf(PlayerKit.BodyOf(guise));
                var prefab = path == null ? null : Resources.Load<GameObject>(path);

                if (prefab == null)
                {
                    strays.Add(guise + " loads no body to read accessories off");
                    continue;
                }

                var sample = UnityEngine.Object.Instantiate(prefab);
                sample.transform.position = Vector3.zero;
                sample.transform.rotation = Quaternion.identity;
                sample.transform.localScale = Vector3.one;

                foreach (var node in sample.GetComponentsInChildren<Transform>(true))
                {
                    if (!node.name.StartsWith(ArtPacks.CastSlotNode, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    for (var slot = 0; slot < node.childCount; slot++)
                    {
                        var accessory = node.GetChild(slot);
                        authored++;

                        if (Squared(accessory))
                        {
                            squared++;
                        }

                        if (accessory.localPosition.magnitude <= SlotSlack)
                        {
                            rested++;
                        }
                        else
                        {
                            strays.Add(guise + "'s " + Posed(accessory));
                        }

                        hung.Add(guise + "'s " + Posed(accessory));
                    }
                }

                WorldObjects.Destroy(sample);
            }

            var seated = 0;
            var armed = 0;
            var loose = new List<string>();

            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                PowerPump.Settle(power, PowerAt(tier));

                var weapon = PlayerKit.WeaponOf(tier);

                if (weapon == PlayerWeapon.None)
                {
                    continue;
                }

                armed++;
                var mounted = player.Wielding;
                var mesh = PlayerKit.ModelOf(weapon);
                var declared = Quaternion.Euler(
                    0f, ArtPacks.MountTurnOf(mesh), ArtPacks.MountRollOf(mesh));

                if (mounted != null && Seated(mounted, declared))
                {
                    seated++;
                }
                else
                {
                    loose.Add(mounted == null
                        ? "tier " + tier + " hangs nothing"
                        : "tier " + tier + "'s " + Posed(mounted) + " against a declared "
                            + Quaternion.Angle(declared, Quaternion.identity).ToString(
                                "0.###", CultureInfo.InvariantCulture) + " degree mount");
                }
            }

            var failures = 0;

            failures += Assert(
                report,
                authored > 0 && squared == authored,
                "the pack hangs every accessory of every guise off a cast slot unturned and unscaled, "
                + "so an identity mount is the pack's own convention rather than a guess",
                squared + " of " + authored + " do: " + string.Join(", ", hung.ToArray()));

            failures += Assert(
                report,
                authored > 0 && rested >= authored - 1,
                "and all of them but at most one hand prop rest within a hair of the slot's own origin, "
                + "which is what makes the origin the place a weapon belongs",
                rested + " of " + authored + " rest within "
                + SlotSlack.ToString("0.###", CultureInfo.InvariantCulture)
                + (strays.Count == 0 ? "" : "; the strays are " + string.Join("; ", strays.ToArray())));

            failures += Assert(
                report,
                armed == PlayerTier.Count - 1 && seated == armed,
                "so every weapon the tier ramp hangs there sits at the same origin unscaled, turned by "
                + "exactly the mount its own pack declares and by nothing else, which is what makes the "
                + "grip read as a grip rather than a mesh floating beside a hand",
                seated + " of " + armed + " sit there"
                + (loose.Count == 0 ? "" : "; " + string.Join("; ", loose.ToArray())));

            return failures;
        }

        static bool Seated(Transform node, Quaternion mount)
        {
            return Quaternion.Angle(node.localRotation, mount) <= AngleEpsilon
                && Math.Abs(node.localScale.x - 1f) <= Epsilon
                && Math.Abs(node.localScale.y - 1f) <= Epsilon
                && Math.Abs(node.localScale.z - 1f) <= Epsilon
                && node.localPosition.magnitude <= Epsilon;
        }

        static bool Squared(Transform node)
        {
            return Quaternion.Angle(node.localRotation, Quaternion.identity) <= AngleEpsilon
                && Math.Abs(node.localScale.x - 1f) <= Epsilon
                && Math.Abs(node.localScale.y - 1f) <= Epsilon
                && Math.Abs(node.localScale.z - 1f) <= Epsilon;
        }

        static string Posed(Transform node)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} at ({1:0.#####}, {2:0.#####}, {3:0.#####}) turned {4:0.###} scaled "
                + "({5:0.#####}, {6:0.#####}, {7:0.#####})",
                node.name,
                node.localPosition.x,
                node.localPosition.y,
                node.localPosition.z,
                Quaternion.Angle(node.localRotation, Quaternion.identity),
                node.localScale.x,
                node.localScale.y,
                node.localScale.z);
        }

        static int TheMountedKitMeasuresWhatTheKitTablePins(
            PowerBadge power, PlayerFigure player, StringBuilder report)
        {
            if (power == null || player == null)
            {
                return Assert(
                    report, false, "there is a mounted kit to measure", "there is no figure wearing one");
            }

            var armed = 0;
            var pinned = 0;
            var planted = 0;
            var gripped = 0;
            var anchored = 0;
            var climbed = 0;
            var reached = 0f;
            var strayed = new List<string>();

            report.Append("\n  reach, in figure units where an adventurer stands ")
                .Append(AdventurerPack.StandingScales.ToString("0.##", CultureInfo.InvariantCulture))
                .Append(':');

            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                PowerPump.Settle(power, PowerAt(tier));

                var guise = PlayerKit.GuiseOf(tier);
                var worn = PlayerKit.BodyOf(guise);
                var body = MeshesOf(worn);
                var scale = power.Look.Scale;
                var ground = player.Ground;
                var hand = CharacterDress.Hand(player.gameObject);
                var grip = hand == null ? 0f : (hand.position.y - ground.Y) / scale;

                if (Math.Abs(grip - PlayerKit.GripHeightOf(guise)) <= Epsilon)
                {
                    gripped++;
                }
                else
                {
                    strayed.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "tier {0} as the {1} grips at {2:0.#####} against the pinned {3:0.#####}",
                        tier,
                        guise,
                        grip,
                        PlayerKit.GripHeightOf(guise)));
                }

                var anchor = BadgeMetrics.AnchorAbove(ground.Y + FigureFit.StandingHeight(worn, scale));

                if (Math.Abs(power.transform.localPosition.y - anchor) <= Epsilon)
                {
                    anchored++;
                }
                else
                {
                    strayed.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "tier {0} anchors its badge at {1:0.#####} against the {2:0.#####} the body asks for",
                        tier,
                        power.transform.localPosition.y,
                        anchor));
                }

                var kit = Silhouette(player);
                var weapon = PlayerKit.WeaponOf(tier);
                var sweep = Armed(player, body, player.Wielding);
                var across = Math.Max(sweep.size.x, sweep.size.z);

                report.Append("\n    tier ")
                    .Append(tier)
                    .Append(" as the ")
                    .Append(guise)
                    .AppendFormat(
                        CultureInfo.InvariantCulture,
                        ": {0} at scale {1:0.####} gripped at {8:0.#####} against the pinned {9:0.#####}, "
                        + "body and weapon {2:0.####} across and {3:0.####} tall "
                        + "off the floor, whole kit {4:0.####} across and {5:0.####} tall, against the "
                        + "gate's {6:0.####} walkway under a {7:0.####} lintel",
                        weapon,
                        scale,
                        across,
                        sweep.max.y - ground.Y,
                        Math.Max(kit.size.x, kit.size.z),
                        kit.max.y - ground.Y,
                        GateArch.Walkway,
                        GateArch.PostHeight,
                        grip,
                        PlayerKit.GripHeightOf(guise));

                if (weapon == PlayerWeapon.None)
                {
                    continue;
                }

                armed++;
                var held = player.Wielding;

                if (held == null)
                {
                    strayed.Add("tier " + tier + " holds nothing to measure");
                    continue;
                }

                var box = Enclosing(held);
                var tip = (box.max.y - ground.Y) / scale;
                var foot = (box.min.y - ground.Y) / scale;
                var breadth = Math.Max(box.size.x, box.size.z) / scale;

                report.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "; the {0} tips at {1:0.#####} and spans {2:0.#####} against the pinned {3:0.#####} "
                    + "and {4:0.#####}, its butt {5:0.#####} off the floor",
                    weapon,
                    tip,
                    breadth,
                    PlayerKit.TipOf(weapon),
                    PlayerKit.BreadthOf(weapon),
                    foot);

                if (Math.Abs(tip - PlayerKit.TipOf(weapon)) <= Epsilon
                    && Math.Abs(breadth - PlayerKit.BreadthOf(weapon)) <= Epsilon)
                {
                    pinned++;
                }

                if (foot > 0f)
                {
                    planted++;
                }

                if (PlayerKit.ReachOf(weapon) > reached)
                {
                    climbed++;
                }

                reached = PlayerKit.ReachOf(weapon);
            }

            var failures = 0;

            failures += Assert(
                report,
                armed == PlayerTier.Count - 1 && pinned == armed,
                "the tip and the breadth the kit table pins for every weapon are the ones the mounted "
                + "mesh actually measures on the rig, so nothing downstream reads a box that is gone",
                pinned + " of " + armed + " weapons measure what they pin"
                + (strayed.Count == 0 ? "" : "; " + string.Join("; ", strayed.ToArray())));

            failures += Assert(
                report,
                gripped == PlayerTier.Count && anchored == PlayerTier.Count,
                "the pinned grip height is the height the rig's own hand slot rides at, at every tier, "
                + "and the badge still anchors off the body rather than off what the hand carries",
                gripped + " of " + PlayerTier.Count + " tiers grip where the kit says and "
                + anchored + " anchor where the metrics say");

            failures += Assert(
                report,
                armed > 0 && planted == armed,
                "no weapon the ramp hands out dips below the floor its figure stands on",
                planted + " of " + armed + " keep their butt off the ground");

            failures += Assert(
                report,
                armed > 0 && climbed == armed && reached > PlayerKit.ReachOf(PlayerKit.WeaponOf(1)) * 4f / 3f,
                "every weapon is a longer object across its own diagonal than the one the tier below "
                + "swung, off the same pack footprints this run measured, and the last is half again the "
                + "first, which is the escalation the closed framing has to read",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} of {1} do, from {2:0.####} to {3:0.####}",
                    climbed,
                    armed,
                    PlayerKit.ReachOf(PlayerKit.WeaponOf(1)),
                    reached));

            return failures;
        }

        static int TheWeaponRidesTheHandThroughEveryClip(
            PowerBadge power, PlayerFigure player, StringBuilder report)
        {
            PowerPump.Settle(power, PowerAt(PlayerTier.Count - 1));

            var animator = player == null ? null : player.Acting;
            var hand = player == null ? null : CharacterDress.Hand(player.gameObject);
            var held = player == null ? null : player.Wielding;

            if (animator == null || hand == null || held == null)
            {
                return Assert(
                    report,
                    false,
                    "the top tier stands rigged, armed and driveable clip by clip",
                    (animator == null ? "no animator" : "an animator") + ", "
                    + (hand == null ? "no hand slot" : "a hand slot") + ", "
                    + (held == null ? "no weapon" : "a weapon"));
            }

            var worn = PlayerKit.BodyOf(player.Wearing);
            var body = MeshesOf(worn);
            var standing = FigureFit.StandingHeight(worn, power.Look.Scale);
            var clips = 0;
            var locked = 0;
            var moved = 0;
            var farthest = 0f;
            var slipped = new List<string>();

            report.Append("\n  clips:");

            foreach (FigureAct act in Enum.GetValues(typeof(FigureAct)))
            {
                animator.Cue(FigureCue.Looping(act));
                animator.Advance(0f);

                var seconds = animator.PlayingSeconds;

                if (animator.Playing == null || seconds <= 0f)
                {
                    slipped.Add(act + " loads no clip to sample");
                    continue;
                }

                clips++;
                var step = seconds / ClipSamples;
                var travelled = 0f;
                var drift = 0f;
                var turned = 0f;
                var widest = 0f;
                var wasHand = hand.position;
                var wasWeapon = held.position;

                for (var sample = 0; sample < ClipSamples; sample++)
                {
                    animator.Advance(step);

                    var now = hand.position;
                    var mine = held.position;

                    travelled += Vector3.Distance(now, wasHand);
                    drift = Math.Max(drift, Vector3.Distance(mine, now));
                    drift = Math.Max(drift, Vector3.Distance(mine - wasWeapon, now - wasHand));
                    turned = Math.Max(turned, Quaternion.Angle(held.rotation, hand.rotation));

                    var sweep = Armed(player, body, held);
                    widest = Math.Max(widest, Math.Max(sweep.size.x, sweep.size.z));

                    wasHand = now;
                    wasWeapon = mine;
                }

                var rode = drift <= Epsilon
                    && turned <= AngleEpsilon
                    && ReferenceEquals(held.parent, hand)
                    && ReferenceEquals(player.Wielding, held);

                if (rode)
                {
                    locked++;
                }
                else
                {
                    slipped.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} drifts {1:0.#####} and turns {2:0.###} off the hand",
                        act,
                        drift,
                        turned));
                }

                if (travelled > Epsilon)
                {
                    moved++;
                }

                if (travelled > farthest)
                {
                    farthest = travelled;
                }

                report.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "\n    {0} runs {1:0.###}s of {2}: the hand travels {3:0.#####}, the weapon drifts "
                    + "{4:0.#####} and turns {5:0.###}, body and weapon sweeping at most {6:0.####} of "
                    + "the gate's {7:0.####} walkway",
                    act,
                    seconds,
                    animator.Playing.name,
                    travelled,
                    drift,
                    turned,
                    widest,
                    GateArch.Walkway);
            }

            var failures = 0;

            failures += Assert(
                report,
                clips == AdventurerClips.Count && locked == clips,
                "the weapon holds the hand's own place and heading at every frame of every clip the "
                + "figure plays, walk, idle, take and the fight clips alike",
                locked + " of " + clips + " clips hold it, of the " + AdventurerClips.Count
                + " the pack is narrowed to"
                + (slipped.Count == 0 ? "" : "; " + string.Join("; ", slipped.ToArray())));

            failures += Assert(
                report,
                clips > 0 && moved == clips && farthest > standing * ClipTravel,
                "and the hand is genuinely swinging while it does, so the lock is the rig carrying the "
                + "weapon rather than a clip that never moved",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} of {1} clips move the hand, the busiest by {2:0.#####} against the {3:0.#####} "
                    + "a tenth of the figure's {4:0.#####} standing height asks for",
                    moved,
                    clips,
                    farthest,
                    standing * ClipTravel,
                    standing));

            return failures;
        }

        static bool Drawn(Renderer renderer)
        {
            return renderer.enabled && renderer.gameObject.activeInHierarchy;
        }

        struct Swept
        {
            public float Across;

            public float Kit;

            public float Crest;
        }

        static Swept SweptThroughTheWalk(
            PlayerFigure player, FigureAnimator animator, ICollection<Mesh> body)
        {
            var swept = default(Swept);

            animator.Cue(FigureCue.Looping(FigureAct.Walk));
            animator.Advance(0f);

            var seconds = animator.PlayingSeconds;

            if (animator.Playing == null || seconds <= 0f)
            {
                return swept;
            }

            var step = seconds / ClipSamples;
            var ground = player.Ground.Y;

            for (var sample = 0; sample < ClipSamples; sample++)
            {
                animator.Advance(step);

                var arms = Armed(player, body, player.Wielding);
                var whole = Silhouette(player);

                swept.Across = Math.Max(swept.Across, Math.Max(arms.size.x, arms.size.z));
                swept.Kit = Math.Max(swept.Kit, Math.Max(whole.size.x, whole.size.z));
                swept.Crest = Math.Max(swept.Crest, whole.max.y - ground);
            }

            return swept;
        }

        static int TheStowedKitWalksThroughAGate(
            PowerBadge power, PlayerFigure player, StringBuilder report)
        {
            if (power == null || player == null || player.Acting == null)
            {
                return Assert(
                    report, false, "there is a driveable figure to walk at a gate", "there is none");
            }

            var tiers = 0;
            var cleared = 0;
            var housed = 0;
            var narrowed = 0;
            var gripping = 0;
            var redrawn = 0;
            var strayed = new List<string>();

            report.Append("\n  the swept corridor across ")
                .Append(AdventurerClips.Walk)
                .Append(", against a walkway of ")
                .Append(GateArch.Walkway.ToString("0.####", CultureInfo.InvariantCulture))
                .Append(" under a lintel at ")
                .Append(GateArch.PostHeight.ToString("0.####", CultureInfo.InvariantCulture))
                .Append(':');

            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                PowerPump.Settle(power, PowerAt(tier));
                tiers++;

                var weapon = PlayerKit.WeaponOf(tier);
                var guise = PlayerKit.GuiseOf(tier);
                var animator = player.Acting;
                var body = MeshesOf(PlayerKit.BodyOf(guise));

                player.Sling(false);
                var held = SweptThroughTheWalk(player, animator, body);

                player.Sling(true);
                var away = SweptThroughTheWalk(player, animator, body);
                var stowed = player.Wielding;
                var gripped = player.Gripping;

                if (gripped == weapon
                    && FigureCues.FinisherOf(gripped) == FigureCues.FinisherOf(guise, weapon)
                    && (weapon == PlayerWeapon.None
                        || (stowed != null && stowed.name == PartNames.Held(weapon))))
                {
                    gripping++;
                }
                else
                {
                    strayed.Add("tier " + tier + " stows and forgets it grips " + weapon);
                }

                if (away.Across <= GateArch.Walkway && away.Kit <= GateArch.Walkway)
                {
                    cleared++;
                }
                else
                {
                    strayed.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "tier {0} sweeps {1:0.####} of a {2:0.####} walkway",
                        tier,
                        Math.Max(away.Across, away.Kit),
                        GateArch.Walkway));
                }

                if (away.Crest <= GateArch.PostHeight)
                {
                    housed++;
                }
                else
                {
                    strayed.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "tier {0} reaches {1:0.####} into a {2:0.####} lintel",
                        tier,
                        away.Crest,
                        GateArch.PostHeight));
                }

                if (away.Across <= held.Across + Epsilon)
                {
                    narrowed++;
                }

                report.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "\n    tier {0} as the {7} at scale {1:0.####} sweeps {2:0.####} holding its {3} out "
                    + "and {4:0.####} with it stowed, whole kit {5:0.####}, crest {6:0.####}",
                    tier,
                    power.Look.Scale,
                    held.Across,
                    weapon,
                    away.Across,
                    away.Kit,
                    away.Crest,
                    guise);

                player.Sling(false);
                var hand = CharacterDress.Hand(player.gameObject);
                var back = player.Wielding;

                if (weapon == PlayerWeapon.None
                    ? back == null
                    : back != null && ReferenceEquals(back.parent, hand))
                {
                    redrawn++;
                }
                else
                {
                    strayed.Add("tier " + tier + " never gets its " + weapon + " back into the hand");
                }
            }

            player.Sling(false);

            var failures = 0;

            failures += Assert(
                report,
                tiers == PlayerTier.Count && cleared == tiers && housed == tiers,
                "the whole kit of a player of every tier fits the gate's walkway and stays under its "
                + "lintel across every frame of the walk clip, which is the corridor a figure actually "
                + "sweeps through an arch rather than the pose it stands in",
                cleared + " of " + tiers + " clear the posts and " + housed + " the lintel"
                + (strayed.Count == 0 ? "" : "; " + string.Join("; ", strayed.ToArray())));

            failures += Assert(
                report,
                tiers > 0 && narrowed == tiers,
                "and stowing never widens a tier that was already narrow enough, so the beat costs "
                + "nothing at the tiers that never needed it",
                narrowed + " of " + tiers + " sweep no wider stowed than held");

            failures += Assert(
                report,
                tiers > 0 && gripping == tiers && redrawn == tiers,
                "a stowed weapon is still the weapon the figure grips, so the finisher a fight picks off "
                + "it is unchanged by where it is hanging, and drawing puts the same mesh back on the "
                + "rig's own hand slot",
                gripping + " of " + tiers + " keep their grip stowed and " + redrawn + " draw it again");

            return failures;
        }

        static int TheFinisherSwingsWhatTheHandHolds(
            PowerBadge power, PlayerFigure player, StringBuilder report)
        {
            if (power == null || player == null)
            {
                return Assert(report, false, "there is a figure to arm and to swing", "there is none");
            }

            var chosen = 0;
            var loaded = 0;
            var tiers = 0;
            var strayed = new List<string>();

            report.Append("\n  finishers:");

            using (var clips = new WorldModels())
            {
                for (var tier = 0; tier < PlayerTier.Count; tier++)
                {
                    PowerPump.Settle(power, PowerAt(tier));
                    tiers++;

                    var gripped = player.Gripping;
                    var wanted = PlayerKit.WeaponOf(tier);
                    var worn = PlayerKit.BodyOf(player.Wearing);
                    var act = FigureCues.FinisherOf(player.Wearing, gripped);
                    var named = AdventurerClips.NameOf(act);
                    var clip = clips.ClipOf(worn, named);
                    var held = player.Wielding;
                    var mounted = gripped == PlayerWeapon.None
                        ? held == null
                        : held != null && held.name == PartNames.Held(gripped);

                    if (gripped == wanted && player.Wearing == PlayerKit.GuiseOf(tier) && mounted)
                    {
                        chosen++;
                    }
                    else
                    {
                        strayed.Add(string.Format(
                            CultureInfo.InvariantCulture,
                            "tier {0} grips {1} against the {2} the ramp dresses it in, holding {3}",
                            tier,
                            gripped,
                            wanted,
                            held == null ? "nothing" : held.name));
                    }

                    if (clip != null)
                    {
                        loaded++;
                    }

                    report.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "\n    tier {0} stands as the {5} in {6} and grips {1} as {2}, finishing on {3}, "
                        + "which is {4}",
                        tier,
                        gripped,
                        gripped == PlayerWeapon.None ? "an empty hand" : PlayerKit.ModelOf(gripped).ToString(),
                        act,
                        clip == null ? named + ", which loads nothing" : clip.name,
                        player.Wearing,
                        worn);
                }
            }

            var failures = 0;

            failures += Assert(
                report,
                tiers == PlayerTier.Count && chosen == tiers,
                "the weapon the fight reads off the figure to pick its finisher is the same one the hand "
                + "is holding a mesh of, at every tier, so the mount and the swing are one notion of what "
                + "the player grips and never two",
                chosen + " of " + tiers + " tiers agree"
                + (strayed.Count == 0 ? "" : "; " + string.Join("; ", strayed.ToArray())));

            failures += Assert(
                report,
                tiers == PlayerTier.Count && loaded == tiers,
                "and the finisher each of those weapons picks resolves to a clip the rig actually carries, "
                + "so no tier reaches its execution with nothing to play",
                loaded + " of " + tiers + " do");

            return failures;
        }

        static int TheCloakIsThePacksOwnCloth(
            PowerBadge power, PlayerFigure player, StringBuilder report)
        {
            if (power == null || player == null)
            {
                return Assert(report, false, "there is a figure to drape", "there is none");
            }

            var draped = 0;
            var tiers = 0;
            var strayed = new List<string>();
            var cloth = new List<string>();

            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                PowerPump.Settle(power, PowerAt(tier));
                tiers++;

                var guise = PlayerKit.GuiseOf(tier);
                var body = MeshesOf(PlayerKit.BodyOf(guise));
                var named = PlayerGuises.CapeOf(guise);
                var owns = PlayerGuises.Drapes(guise);
                var cape = CharacterDress.Cloak(player.gameObject, guise);
                var wanted = PlayerKit.CloakedAt(tier);
                var renderer = cape == null ? null : cape.GetComponent<Renderer>();
                var mesh = renderer == null ? null : PackMesh.On(renderer);
                var stands = owns
                    ? cape != null && mesh != null && body.Contains(mesh)
                        && cape.gameObject.activeSelf == wanted
                    : cape == null && !wanted;

                if (stands && player.IsCloaked == wanted)
                {
                    draped++;
                }
                else
                {
                    strayed.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "tier {0} as the {1} wears {2} {3} against the {4} the ramp asks of a guise that "
                        + "{5} a cape",
                        tier,
                        guise,
                        cape == null ? "no cloth at all" : cape.name,
                        cape != null && cape.gameObject.activeSelf ? "shown" : "hidden",
                        wanted ? "shown" : "hidden",
                        owns ? "owns" : "owns no"));
                }

                cloth.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "tier {0} as the {1} {2} on {3}",
                    tier,
                    guise,
                    player.IsCloaked ? "cloaked" : "bare",
                    owns ? named : "no cape node at all"));
            }

            return Assert(
                report,
                tiers == PlayerTier.Count && draped == tiers,
                "the cloak is the cape the pack bolts to the bones of the guise now standing there, shown "
                + "from the threshold that dresses it in one and hidden below it, and a guise that owns no "
                + "cape node never drapes and never reports itself cloaked",
                draped + " of " + tiers + " tiers do: " + string.Join(", ", cloth.ToArray())
                + (strayed.Count == 0 ? "" : "; " + string.Join("; ", strayed.ToArray())));
        }

        static int EveryTierStandsInTheBodyItsGuiseNames(
            PowerBadge power, PlayerFigure player, StringBuilder report)
        {
            if (power == null || player == null)
            {
                return Assert(report, false, "there is a figure to look at", "there is none");
            }

            var tiers = 0;
            var bodied = 0;
            var caped = 0;
            var slotted = 0;
            var strayed = new List<string>();

            report.Append("\n  guises:");

            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                PowerPump.Settle(power, PowerAt(tier));
                tiers++;

                var guise = PlayerKit.GuiseOf(tier);
                var mesh = PlayerKit.BodyOf(guise);
                var pack = MeshesOf(mesh);
                var stranger = new List<string>();
                var wearing = 0;

                foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
                {
                    var cut = PackMesh.On(renderer);

                    if (cut == null || !Dressed(renderer) || Propped(renderer.transform))
                    {
                        continue;
                    }

                    if (pack.Contains(cut))
                    {
                        wearing++;
                    }
                    else
                    {
                        stranger.Add(renderer.name + " wearing " + cut.name);
                    }
                }

                var raised = player.Body;

                if (player.Wearing == guise
                    && raised != null
                    && raised.name == PartNames.Guised(guise)
                    && wearing > 0
                    && stranger.Count == 0)
                {
                    bodied++;
                }
                else
                {
                    strayed.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "tier {0} stands as the {1} in {2} wearing {3} of its meshes and {4} strangers: {5}",
                        tier,
                        player.Wearing,
                        raised == null ? "nothing" : raised.name,
                        wearing,
                        stranger.Count,
                        string.Join(", ", stranger.ToArray())));
                }

                var owns = PlayerGuises.Drapes(guise);
                var cape = ClothOn(player.Body);

                if (owns == (cape != null)
                    && (cape == null
                        || (cape.name == PlayerGuises.CapeOf(guise)
                            && ReferenceEquals(cape, CharacterDress.Cloak(player.gameObject, guise))
                            && cape.gameObject.activeSelf == PlayerKit.CloakedAt(tier))))
                {
                    caped++;
                }
                else
                {
                    strayed.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "tier {0} declares {1} and the {2} mesh carries {3}",
                        tier,
                        owns ? PlayerGuises.CapeOf(guise) : "no cape",
                        mesh,
                        cape == null ? "none" : cape.name));
                }

                var weapon = PlayerKit.WeaponOf(tier);
                var held = player.Wielding;
                var hand = held == null ? null : held.parent;

                if (weapon == PlayerWeapon.None
                    ? held == null
                    : held != null
                        && hand != null
                        && hand.name.StartsWith(ArtPacks.CastSlotNode, StringComparison.Ordinal)
                        && ReferenceEquals(hand, CharacterDress.Hand(player.gameObject))
                        && Seated(held, Quaternion.Euler(
                            0f,
                            ArtPacks.MountTurnOf(PlayerKit.ModelOf(weapon)),
                            ArtPacks.MountRollOf(PlayerKit.ModelOf(weapon)))))
                {
                    slotted++;
                }
                else
                {
                    strayed.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "tier {0} hangs {1} off {2}",
                        tier,
                        held == null ? "nothing" : Posed(held),
                        hand == null ? "nothing" : hand.name));
                }

                report.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "\n    tier {0} is the {1} in {2} of {3} meshes, cape {4}, holding {5}",
                    tier,
                    guise,
                    mesh,
                    wearing,
                    cape == null ? "none declared and none on the mesh" : cape.name + " "
                        + (cape.gameObject.activeSelf ? "shown" : "hidden"),
                    held == null ? "nothing" : held.name);
            }

            var failures = 0;

            failures += Assert(
                report,
                tiers == PlayerTier.Count && bodied == tiers,
                "the mesh the figure is actually built out of at every tier is the one that tier's guise "
                + "names, and nothing of the guise it used to be is left standing on it",
                bodied + " of " + tiers + " tiers do"
                + (strayed.Count == 0 ? "" : "; " + string.Join("; ", strayed.ToArray())));

            failures += Assert(
                report,
                tiers == PlayerTier.Count && caped == tiers,
                "the cape node is on the mesh exactly when the guise declares one, and shown exactly when "
                + "the look says cloaked",
                caped + " of " + tiers + " tiers do");

            failures += Assert(
                report,
                tiers == PlayerTier.Count && slotted == tiers,
                "the weapon hangs off the rig's own hand slot at the slot's own origin, unscaled, turned "
                + "only by the mount its pack declares, at every tier, so a change of body never leaves "
                + "the grip behind",
                slotted + " of " + tiers + " tiers do");

            return failures;
        }

        static Transform ClothOn(GameObject body)
        {
            if (body == null)
            {
                return null;
            }

            foreach (var node in body.GetComponentsInChildren<Transform>(true))
            {
                if (node.name.EndsWith(AdventurerPack.CloakSuffix, StringComparison.Ordinal))
                {
                    return node;
                }
            }

            return null;
        }

        static int ASwapKeepsWhatTheHeroWasDoingAndCarrying(
            PowerBadge power, PlayerFigure player, StringBuilder report)
        {
            if (power == null || player == null)
            {
                return Assert(report, false, "there is a figure to swap", "there is none");
            }

            var below = 0;
            var above = 0;

            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                if (PlayerKit.GuiseOf(tier) == PlayerKit.GuiseOf(tier - 1))
                {
                    continue;
                }

                below = tier - 1;
                above = tier;
                break;
            }

            if (above == 0)
            {
                return Assert(
                    report, false, "the ramp changes guise somewhere", "every tier wears the same body");
            }

            PowerPump.Settle(power, PowerAt(above));
            var carried = player.Carrying;
            PowerPump.Settle(power, PowerAt(below));

            var animator = player.Acting;
            animator.Cue(FigureCue.Looping(FigureAct.Walk));
            animator.Advance(animator.PlayingSeconds * 0.5f);

            player.Face(new WorldPoint(1f, 0f, 0f));

            for (var frame = 0; frame < 240 && player.IsTurning; frame++)
            {
                player.Turn(PowerPump.Frame);
            }

            var wasGuise = player.Wearing;
            var wasAct = animator.Act;
            var wasPhase = animator.Phase;
            var wasYaw = player.transform.localEulerAngles.y;
            var wasTrophies = player.Carrying;

            PowerPump.Settle(power, PowerAt(above));

            var now = player.Acting;
            var failures = 0;

            report.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  the swap from tier {0} to tier {1}: the {2} was walking {3:0.####}s in at {4:0.###} "
                + "degrees carrying {5}; the {6} is playing {7} {8:0.####}s in at {9:0.###} degrees "
                + "carrying {10} of the {11} that tier wants",
                below,
                above,
                wasGuise,
                wasPhase,
                wasYaw,
                wasTrophies,
                player.Wearing,
                now == null ? "nothing" : now.Act.ToString(),
                now == null ? 0f : now.Phase,
                player.transform.localEulerAngles.y,
                player.Carrying,
                carried);

            failures += Assert(
                report,
                player.Wearing != wasGuise && player.Wearing == PlayerKit.GuiseOf(above),
                "the promotion across the guise seam really does put a different body on the tile",
                wasGuise + " became " + player.Wearing);

            failures += Assert(
                report,
                now != null && !ReferenceEquals(now, animator) && now.Act == wasAct
                && Math.Abs(now.Phase - wasPhase) <= Epsilon && now.Playing != null,
                "the new body picks the walk up at the phase the old one had reached rather than "
                + "restarting it, so a promotion mid-journey does not reset the stride",
                now == null
                    ? "there is no animator on the new body"
                    : string.Format(
                        CultureInfo.InvariantCulture,
                        "it plays {0} at {1:0.#####}s against the {2} at {3:0.#####}s it took over",
                        now.Act,
                        now.Phase,
                        wasAct,
                        wasPhase));

            failures += Assert(
                report,
                Math.Abs(Mathf.DeltaAngle(player.transform.localEulerAngles.y, wasYaw)) <= AngleEpsilon,
                "and it faces exactly the way the body it replaced was facing, so the swap never spins "
                + "the figure",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "it faces {0:0.###} against {1:0.###}",
                    player.transform.localEulerAngles.y,
                    wasYaw));

            failures += Assert(
                report,
                player.Carrying == carried && Planted(player) == WantedProps(above),
                "every trophy the hero had collected survives the change of costume, and the props "
                + "hanging off it are exactly the ones the tier calls for and no spares",
                player.Carrying + " trophies in " + Planted(player) + " props against the "
                + WantedProps(above) + " tier " + above + " wants");

            return failures;
        }

        sealed class Drop
        {
            public string Name = "nothing";
            public int Frames;
            public int OwnMeshes;
            public int Strangers;
            public int OutgoingMeshes;
            public float Scale;
            public float Spin;
            public float First;
            public float Last;
            public float Highest = float.MinValue;
        }

        static Drop Dropped(
            PowerBadge power,
            PlayerFigure player,
            WorldPoint site,
            int target,
            ICollection<Mesh> arriving,
            ICollection<Mesh> leaving)
        {
            var drop = new Drop();
            var opening = 0f;

            power.DropWeaponFrom(site);
            power.Show(target);

            for (var frame = 0; frame < PowerPump.Ceiling && (!power.IsSettled || player.IsFlying); frame++)
            {
                power.Advance(PowerPump.Frame);

                var falling = player.Dropping;

                if (falling == null)
                {
                    continue;
                }

                if (drop.Frames == 0)
                {
                    drop.Name = falling.name;
                    drop.Scale = falling.lossyScale.x;
                    drop.First = falling.position.y;
                    opening = falling.localEulerAngles.y;

                    foreach (var renderer in falling.GetComponentsInChildren<Renderer>(true))
                    {
                        var mesh = PackMesh.On(renderer);

                        if (mesh != null && arriving.Contains(mesh))
                        {
                            drop.OwnMeshes++;
                            continue;
                        }

                        drop.Strangers++;

                        if (mesh != null && leaving.Contains(mesh))
                        {
                            drop.OutgoingMeshes++;
                        }
                    }
                }

                drop.Frames++;
                drop.Last = falling.position.y;
                drop.Highest = falling.position.y > drop.Highest ? falling.position.y : drop.Highest;
                var turned = Math.Abs(Mathf.DeltaAngle(opening, falling.localEulerAngles.y));
                drop.Spin = turned > drop.Spin ? turned : drop.Spin;
            }

            return drop;
        }

        static int StillHangingOffTheLevel(PlayerFigure player)
        {
            var parent = player.transform.parent;

            if (parent == null)
            {
                return 0;
            }

            var left = 0;

            foreach (var node in parent.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(node.name, PartNames.Weapon, StringComparison.Ordinal))
                {
                    left++;
                }
            }

            return left;
        }

        static ISet<Mesh> Bladed(PlayerWeapon weapon)
        {
            return weapon == PlayerWeapon.None
                ? new HashSet<Mesh>()
                : MeshesOf(PlayerKit.ModelOf(weapon));
        }

        static int TheDropCarriesTheWeaponTheHeroIsAboutToGrip(
            PowerBadge power, PlayerFigure player, WorldPoint site, StringBuilder report)
        {
            if (power == null || player == null)
            {
                return Assert(
                    report,
                    false,
                    "there is a hero for a dying enemy to throw a weapon at",
                    "the world raised none");
            }

            var failures = 0;
            var carried = 0;
            var sized = 0;
            var arced = 0;
            var armed = 0;
            var matched = 0;
            var released = 0;
            var strayed = new List<string>();
            var hanging = new List<string>();

            report.Append("\n  drops, each one thrown at the rung above or below the hero's own:");

            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                var from = tier == 0 ? 1 : tier - 1;
                PowerPump.Settle(power, PowerAt(from));

                var leaving = PlayerKit.WeaponOf(from);
                var arriving = PlayerLook.Of(PowerAt(tier));
                var drop = Dropped(
                    power, player, site, PowerAt(tier), Bladed(arriving.Weapon), Bladed(leaving));

                report.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "\n    tier {0} <- tier {1}: the {2} leaves a {3} for the {4}'s {5}, {6} pack meshes "
                    + "and {7} strangers over {8} frames, uniform scale {9:0.#####} against the "
                    + "{10:0.#####} the drop pins, rising {11:0.####} above its ends and turning "
                    + "{12:0.#} degrees",
                    tier,
                    from,
                    leaving,
                    drop.Name,
                    arriving.Guise,
                    arriving.Weapon,
                    drop.OwnMeshes,
                    drop.Strangers,
                    drop.Frames,
                    drop.Scale,
                    WeaponDrop.CarriesAMesh(arriving) ? WeaponDrop.ScaleOf(arriving) : 0f,
                    drop.Highest - Math.Max(drop.First, drop.Last),
                    drop.Spin);

                var left = StillHangingOffTheLevel(player);

                if (player.Dropping == null && left == 0)
                {
                    released++;
                }
                else
                {
                    hanging.Add(
                        "tier " + tier + " left " + left + " of them hanging off the level");
                }

                if (drop.Frames > 1 && drop.Spin > 0f
                    && drop.Highest > Math.Max(drop.First, drop.Last))
                {
                    arced++;
                }
                else
                {
                    strayed.Add("tier " + tier + " never arced or never spun");
                }

                if (!WeaponDrop.CarriesAMesh(arriving))
                {
                    if (drop.OwnMeshes == 0 && drop.Frames > 0)
                    {
                        carried++;
                        sized++;
                    }
                    else
                    {
                        strayed.Add("tier " + tier + " flew a mesh at a rung that grips nothing");
                    }

                    continue;
                }

                armed++;

                if (drop.OwnMeshes > 0 && drop.Strangers == 0 && drop.OutgoingMeshes == 0)
                {
                    carried++;
                }
                else
                {
                    strayed.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "tier {0} flew {1} meshes of the {2} it is about to grip, {3} strangers and {4} "
                        + "off the {5} it is leaving",
                        tier,
                        drop.OwnMeshes,
                        arriving.Weapon,
                        drop.Strangers,
                        drop.OutgoingMeshes,
                        leaving));
                }

                if (Math.Abs(drop.Scale - WeaponDrop.ScaleOf(arriving)) <= Epsilon)
                {
                    sized++;
                }
                else
                {
                    strayed.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "tier {0} flew at {1:0.#####} against the {2:0.#####} the drop pins",
                        tier,
                        drop.Scale,
                        WeaponDrop.ScaleOf(arriving)));
                }

                var gripped = player.Wielding;

                if (gripped != null && Math.Abs(gripped.lossyScale.x - drop.Scale) <= Epsilon)
                {
                    matched++;
                }
                else
                {
                    strayed.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "tier {0} landed at {1:0.#####} in a hand holding it at {2:0.#####}",
                        tier,
                        drop.Scale,
                        gripped == null ? 0f : gripped.lossyScale.x));
                }
            }

            failures += Assert(
                report,
                carried == PlayerTier.Count,
                "every drop flew the mesh of the weapon the rung it lands on grips, and never the one "
                + "the hero is leaving behind",
                carried + " of " + PlayerTier.Count + " drops carried what they should"
                + (strayed.Count == 0 ? "" : ": " + string.Join(", ", strayed.ToArray())));

            failures += Assert(
                report,
                sized == PlayerTier.Count && matched == armed && armed > 0,
                "a weapon is the same size in the air as it is in the hand it lands in",
                sized + " drops flew at the pinned scale and " + matched + " of " + armed
                + " armed rungs landed at the scale the hand holds them at");

            failures += Assert(
                report,
                arced == PlayerTier.Count,
                "carrying a real mesh leaves the arc and the spin alone",
                arced + " of " + PlayerTier.Count + " drops rose above both ends of their flight while "
                + "turning");

            failures += Assert(
                report,
                released == PlayerTier.Count,
                "a settling flight destroys the weapon it raised on the path the Editor actually runs, "
                + "so no drop is left hanging off the level once the hero has it",
                released + " of " + PlayerTier.Count + " drops cleared themselves away"
                + (hanging.Count == 0 ? "" : ": " + string.Join(", ", hanging.ToArray())));

            return failures;
        }

        static int AWeaponArrivingWhileTheHeroIsStowedGoesDownHisSpine(
            PowerBadge power, PlayerFigure player, WorldPoint site, StringBuilder report)
        {
            if (power == null || player == null)
            {
                return Assert(report, false, "there is a hero to stow a weapon on", "the world raised none");
            }

            var spined = 0;
            var drawn = 0;
            var armed = 0;
            var strayed = new List<string>();

            report.Append("\n  drops landing on a hero walking a gate with his weapon down his spine:");

            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                var arriving = PlayerLook.Of(PowerAt(tier));

                if (!WeaponDrop.CarriesAMesh(arriving))
                {
                    continue;
                }

                armed++;
                PowerPump.Settle(power, PowerAt(tier - 1));
                player.Sling(true);

                var away = player.IsStowed;
                var drop = Dropped(
                    power, player, site, PowerAt(tier), Bladed(arriving.Weapon), new HashSet<Mesh>());

                var stayed = player.IsStowed;
                var held = player.Wielding;
                var spine = held != null && held.parent == player.transform;

                player.Sling(false);

                var back = player.Wielding;
                var hand = !player.IsStowed
                    && back != null
                    && back.parent != null
                    && back.parent.name.StartsWith(ArtPacks.CastSlotNode, StringComparison.Ordinal);

                report.Append("\n    tier ")
                    .Append(tier)
                    .Append(": the ")
                    .Append(arriving.Weapon)
                    .Append(" flew ")
                    .Append(drop.Frames)
                    .Append(" frames onto a hero whose stow was ")
                    .Append(away ? "on" : "off")
                    .Append(" and stayed ")
                    .Append(stayed ? "on" : "off")
                    .Append(", came to rest on ")
                    .Append(held == null ? "nothing" : spine ? "the spine" : held.parent.name)
                    .Append(" and drew back to ")
                    .Append(back == null || back.parent == null ? "nothing" : back.parent.name);

                if (away && stayed && drop.Frames > 0 && spine)
                {
                    spined++;
                }
                else
                {
                    strayed.Add("tier " + tier + " lost the stow the drop arrived into");
                }

                if (hand)
                {
                    drawn++;
                }
                else
                {
                    strayed.Add("tier " + tier + " never drew the delivered weapon back into the hand");
                }
            }

            var failures = Assert(
                report,
                armed > 0 && spined == armed,
                "a weapon landing while the hero walks a gate stowed hangs down his spine instead of "
                + "fighting the stow for his hand",
                spined + " of " + armed + " drops landed into a stow and stayed in it"
                + (strayed.Count == 0 ? "" : ": " + string.Join(", ", strayed.ToArray())));

            failures += Assert(
                report,
                armed > 0 && drawn == armed,
                "drawing again past the gate puts the weapon the drop delivered back in the hand",
                drawn + " of " + armed + " drops ended up gripped once the gate was behind him");

            return failures;
        }

        static int EveryGuiseSatForAPortrait(IReadOnlyList<PlayerGuise> filmed, StringBuilder report)
        {
            var shot = 0;
            var missing = new List<string>();

            foreach (var guise in PlayerGuises.All)
            {
                if (File.Exists(GuisePath + guise + ".png"))
                {
                    shot++;
                }
                else
                {
                    missing.Add(guise + " never sat for one");
                }
            }

            return Assert(
                report,
                shot == PlayerGuises.Count && filmed.Count == PlayerGuises.Count,
                "the climb films a portrait of every guise it dresses the hero in, so the scratch folder "
                + "carries one run artifact per body and not only one per tier",
                shot + " of " + PlayerGuises.Count + " guises are on film under " + GuisePath
                + (missing.Count == 0 ? "" : "; " + string.Join("; ", missing.ToArray())));
        }

        static bool Dressed(Renderer renderer)
        {
            var material = renderer.sharedMaterial;

            return material != null
                && material.name.StartsWith(WorldMaterials.NamePrefix, StringComparison.Ordinal);
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
            IReadOnlyList<EnemyFigure> enemies)
        {
            var counts = new int[4];
            foreach (var enemy in enemies)
            {
                counts[(int)enemy.Band]++;
            }

            var row = new StringBuilder();
            row.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  power {0} is tier {1} as the {2} at scale {3:0.###} standing {4:0.###} carrying {5} ->",
                power.Power,
                power.Look.Tier,
                player.Wearing,
                player.transform.localScale.x,
                Standing(player),
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
