using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Domain;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Game.EditorTooling
{
    public static class EnemyCastCheckCommand
    {
        const long Seed = 20250824L;

        const float Epsilon = DungeonPack.BoundsEpsilon;

        const float PortraitSize = 2.4f;

        const int RichPower = 20000;

        const string BaseColour = "_BaseColor";

        const string BaseMap = "_BaseMap";

        const string LevelPath = "dev/scratch/t-33-enemy-cast-level.png";

        const string PortraitPath = "dev/scratch/t-33-cast-";

        const string SilhouettePath = "dev/scratch/t-131-enemy-silhouette-tier-";

        const string AlivePath = "dev/scratch/t-43-enemy-cast-alive.png";

        const string IdlePath = "dev/scratch/t-43-enemy-idle-";

        const int Sequence = 8;

        const float CloseRange = 6f;

        const float CloseFraming = 0.9f;

        const float Frame = 1f / 60f;

        const float StirFloor = 1f;

        const int CostFrames = 60;

        const int CostRounds = 3;

        static readonly PartStyle[] StillPrimitive =
        {
            PartStyle.Pillar, PartStyle.Trail, PartStyle.Spark, PartStyle.Multiplier, PartStyle.Landmark
        };

        public static void Check()
        {
            Wipe(LevelPath);
            Wipe(AlivePath);

            for (var shot = 0; shot < Sequence; shot++)
            {
                Wipe(IdlePath + shot.ToString("00", CultureInfo.InvariantCulture) + ".png");
            }

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                Wipe(PortraitPath + model + ".png");
            }

            for (var tier = 0; tier < EnemyTier.Count; tier++)
            {
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

            var builder = new WorldBuilder();
            var root = builder.Build(graph);
            var power = builder.PlayerBadge;
            var byName = new Dictionary<string, Transform>();

            foreach (var node in root.GetComponentsInChildren<Transform>(true))
            {
                byName[node.name] = node;
            }

            var packs = new Dictionary<PartModel, ISet<Mesh>>();

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (!ArtPacks.ShipsWithTheCast(model))
                {
                    continue;
                }

                var path = WorldModels.AssetPathOf(model);
                packs.Add(model, PackMesh.Of(path == null ? null : Resources.Load<GameObject>(path)));
            }

            PreviewFilm.Sun();
            rig.Begin(graph);
            rig.Skip();
            PreviewFilm.Warm(lens);
            PreviewFilm.Shoot(lens, LevelPath);

            var report = new StringBuilder("t-33 enemies and the boss wear character meshes, ship seed ")
                .Append(Seed.ToString(CultureInfo.InvariantCulture))
                .Append(':');
            var failures = 0;

            failures += OnlyEffectsAndThePillarFallBack(report);
            failures += EveryCastMeshMeasuresItsPinnedFootprint(packs, report);
            failures += EveryAdversaryWearsTheMeshItsNumberNames(graph, byName, packs, report);
            failures += TheBossCannotBeMistakenForAnEnemy(graph, byName, packs, report);
            failures += ThreeEnemySilhouettesStandApartOnTheLevel(graph, byName, report);
            failures += EveryFigureIsFittedToItsOwnTile(graph, byName, packs, report);
            failures += NoFigureHidesMoreGroundThanItsCapsule(graph, byName, packs, report);
            failures += TheStripTakesTheSparesAndLeavesTheSilhouette(graph, byName, report);
            failures += BadgesStayLegibleOverTheFigures(graph, byName, packs, report);
            failures += EveryAdversaryReadsAsACreatureAtEveryBand(graph, byName, packs, report);

            Portraits(rig, lens, graph, byName, packs);
            var silhouettes = Silhouettes(rig, lens, graph, byName, root);

            failures += EveryRiggedFigureIsPosedByAnAnimator(root, byName, rig, lens, graph, report);
            failures += TheBandMovesTheScaleButNeverTheMeshNorTheColour(
                graph, byName, packs, power, report);
            failures += EveryAdversaryStillReadsOnceThePlayerHasOutgrownIt(graph, byName, packs, report);
            failures += EveryAdversaryShowsThePacksOwnTexture(graph, byName, report);
            failures += EveryBandIsPhotographedWithItsBadgeCovered(silhouettes, report);

            Application.logMessageReceived -= watcher;

            failures += Assert(
                report,
                warnings.Count == 0,
                "nothing fell back to a primitive while the world was built and every band was read, "
                + "so the warn-once path stayed silent",
                warnings.Count == 0
                    ? "the model cache logged no fallback"
                    : string.Join(" | ", warnings.ToArray()));

            report.Append("\n  t-33: ")
                .Append(failures == 0
                    ? "every assertion above held"
                    : failures + (failures == 1 ? " assertion" : " assertions") + " above failed");

            Debug.Log(report.ToString());

            if (failures > 0)
            {
                Debug.LogError(
                    "The enemy cast check failed " + failures + " assertions. Read the report above.");
            }

            WorldObjects.Destroy(root);
            builder.Dispose();
        }

        static int OnlyEffectsAndThePillarFallBack(StringBuilder report)
        {
            var primitive = new List<string>();
            var dressed = new List<string>();
            var falling = new List<string>();

            using (var cache = new WorldModels())
            {
                foreach (PartStyle style in Enum.GetValues(typeof(PartStyle)))
                {
                    if (PartModels.Of(style) == PartModel.None)
                    {
                        primitive.Add(style.ToString());
                        continue;
                    }

                    if (cache.Dresses(style))
                    {
                        dressed.Add(style.ToString());
                    }
                    else
                    {
                        falling.Add(style.ToString());
                    }
                }
            }

            var expected = new List<string>();

            foreach (var style in StillPrimitive)
            {
                expected.Add(style.ToString());
            }

            primitive.Sort(StringComparer.Ordinal);
            expected.Sort(StringComparer.Ordinal);

            var failures = 0;

            failures += Assert(
                report,
                string.Join(", ", primitive.ToArray()) == string.Join(", ", expected.ToArray()),
                "exactly the cutscene pillar, the two effects, the multiplier gate and the navigation "
                + "landmark the builder assembles out of primitives want no pack mesh, and nothing "
                + "else does",
                "still primitive: " + string.Join(", ", primitive.ToArray())
                + "; expected exactly: " + string.Join(", ", expected.ToArray()));

            failures += Assert(
                report,
                falling.Count == 0 && dressed.Count > 0,
                "every style that wants a mesh finds every mesh it can wear",
                dressed.Count + " dressed: " + string.Join(", ", dressed.ToArray())
                + (falling.Count == 0 ? "" : "; falling back: " + string.Join(", ", falling.ToArray())));

            var worn = new List<string>();

            foreach (var role in CharacterCast.Roles)
            {
                var meshes = CharacterCast.MeshesOf(role);
                var named = new List<string>();

                for (var slot = 0; slot < meshes.Count; slot++)
                {
                    named.Add(meshes[slot].ToString());
                }

                worn.Add(role + " -> " + string.Join("/", named.ToArray()));
            }

            report.Append("\n  the cast: ").Append(string.Join(", ", worn.ToArray()));

            var census = new List<string>();

            for (var tier = 0; tier < EnemyTier.Count; tier++)
            {
                census.Add("tier " + tier + " -> " + CharacterCast.TierMeshOf(tier));
            }

            report.Append("\n  the enemy silhouette ramp: ").Append(string.Join(", ", census.ToArray()));

            return failures;
        }

        static int EveryCastMeshMeasuresItsPinnedFootprint(
            IDictionary<PartModel, ISet<Mesh>> packs, StringBuilder report)
        {
            var failures = 0;

            foreach (var pair in packs)
            {
                var model = pair.Key;
                var path = WorldModels.AssetPathOf(model);
                var prefab = path == null ? null : Resources.Load<GameObject>(path);

                if (prefab == null)
                {
                    failures += Assert(
                        report,
                        false,
                        "the " + model + " asset measures the footprint its pack pins",
                        "Resources/" + (path ?? "nothing") + " loads nothing to measure");
                    continue;
                }

                var box = PackMesh.Bare(prefab);

                failures += Assert(
                    report,
                    Math.Abs(box.size.x - ArtPacks.WidthOf(model)) <= Epsilon
                    && Math.Abs(box.size.y - ArtPacks.HeightOf(model)) <= Epsilon
                    && Math.Abs(box.size.z - ArtPacks.DepthOf(model)) <= Epsilon
                    && Math.Abs(box.min.y - ArtPacks.BaseOf(model)) <= Epsilon,
                    "the " + model + " asset measures the footprint the " + ArtPacks.Of(model)
                    + " pack pins, unrotated and unscaled",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "it measures {0:0.#####} by {1:0.#####} by {2:0.#####} with a base at {3:0.#####}, "
                        + "against the pinned {4:0.#####} by {5:0.#####} by {6:0.#####} and {7:0.#####}",
                        box.size.x,
                        box.size.y,
                        box.size.z,
                        box.min.y,
                        ArtPacks.WidthOf(model),
                        ArtPacks.HeightOf(model),
                        ArtPacks.DepthOf(model),
                        ArtPacks.BaseOf(model)));
            }

            return failures;
        }

        static int EveryAdversaryWearsTheMeshItsNumberNames(
            LevelGraph graph,
            IDictionary<string, Transform> byName,
            IDictionary<PartModel, ISet<Mesh>> packs,
            StringBuilder report)
        {
            var adversaries = 0;
            var right = 0;
            var complaint = new List<string>();

            foreach (var figure in Adversaries(graph, byName))
            {
                adversaries++;
                var wanted = CharacterCast.MeshOf(figure.Style, figure.Value);
                var skins = figure.Instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var primitive = figure.Instance.GetComponent<MeshFilter>();
                var bolted = figure.Instance.GetComponentsInChildren<MeshFilter>(true).Length;
                var stranger = 0;

                foreach (var renderer in figure.Instance.GetComponentsInChildren<Renderer>(true))
                {
                    var mesh = PackMesh.On(renderer);

                    if (mesh == null || !Pack(packs, wanted).Contains(mesh))
                    {
                        stranger++;
                    }
                }

                if (skins.Length > 0 && primitive == null && stranger == 0)
                {
                    right++;
                }
                else if (complaint.Count < 6)
                {
                    complaint.Add(
                        figure.Name + " holding " + figure.Value + " wants " + wanted + " but shows "
                        + skins.Length + " skins, " + (primitive == null ? "no" : "a")
                        + " primitive of its own, " + bolted + " meshes bolted to its bones and "
                        + stranger + " strangers");
                }
            }

            return Assert(
                report,
                adversaries > 0 && right == adversaries,
                "every enemy and the boss wears the skinned mesh its own number names, carries no "
                + "primitive of its own, and shows no mesh from outside that asset",
                right + " of " + adversaries + " do"
                + (complaint.Count == 0 ? "" : "; " + string.Join("; ", complaint.ToArray())));
        }

        static int TheBossCannotBeMistakenForAnEnemy(
            LevelGraph graph,
            IDictionary<string, Transform> byName,
            IDictionary<PartModel, ISet<Mesh>> packs,
            StringBuilder report)
        {
            var bossMeshes = new HashSet<Mesh>();
            var enemyMeshes = new HashSet<Mesh>();
            var bossTop = 0f;
            var tallestEnemy = 0f;
            var bossGround = 0f;
            var widestEnemy = 0f;
            var bosses = 0;

            foreach (var figure in Adversaries(graph, byName))
            {
                var box = PackMesh.Wearing(
                    figure.Instance, Pack(packs, CharacterCast.MeshOf(figure.Style, figure.Value)));
                var ground = box.size.x * box.size.z;

                foreach (var renderer in figure.Instance.GetComponentsInChildren<Renderer>(true))
                {
                    var mesh = PackMesh.On(renderer);
                    if (mesh == null)
                    {
                        continue;
                    }

                    if (figure.Style == PartStyle.Boss)
                    {
                        bossMeshes.Add(mesh);
                    }
                    else
                    {
                        enemyMeshes.Add(mesh);
                    }
                }

                if (figure.Style == PartStyle.Boss)
                {
                    bosses++;
                    bossTop = box.size.y;
                    bossGround = ground;
                }
                else
                {
                    tallestEnemy = Math.Max(tallestEnemy, box.size.y);
                    widestEnemy = Math.Max(widestEnemy, ground);
                }
            }

            var shared = 0;

            foreach (var mesh in bossMeshes)
            {
                if (enemyMeshes.Contains(mesh))
                {
                    shared++;
                }
            }

            var failures = 0;

            failures += Assert(
                report,
                bosses == 1 && bossMeshes.Count > 0 && enemyMeshes.Count > 0 && shared == 0,
                "the boss shares not one mesh with any enemy, so it reads apart by silhouette before "
                + "its badge is read at all",
                bossMeshes.Count + " boss meshes against " + enemyMeshes.Count + " enemy meshes, "
                + shared + " shared, from " + bosses + " boss");

            failures += Assert(
                report,
                bossTop > tallestEnemy && bossGround > widestEnemy,
                "the boss also stands taller and covers more ground than the tallest and widest enemy "
                + "on the level",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "it stands {0:0.#####} against {1:0.#####}, which is {2:0.###} times as tall, and "
                    + "covers {3:0.#####} tiles of footprint against {4:0.#####}, which is {5:0.###} times "
                    + "as much",
                    bossTop,
                    tallestEnemy,
                    tallestEnemy <= 0f ? 0f : bossTop / tallestEnemy,
                    bossGround,
                    widestEnemy,
                    widestEnemy <= 0f ? 0f : bossGround / widestEnemy));

            return failures;
        }

        static int ThreeEnemySilhouettesStandApartOnTheLevel(
            LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            var meshesOfTier = new Dictionary<int, HashSet<Mesh>>();
            var modelOfTier = new Dictionary<int, PartModel>();
            var lowest = new Dictionary<int, int>();
            var highest = new Dictionary<int, int>();

            for (var tier = 0; tier < EnemyTier.Count; tier++)
            {
                meshesOfTier[tier] = new HashSet<Mesh>();
            }

            foreach (var figure in Adversaries(graph, byName))
            {
                if (figure.Style != PartStyle.Enemy)
                {
                    continue;
                }

                var tier = EnemyTier.Of(figure.Value);
                modelOfTier[tier] = CharacterCast.MeshOf(figure.Style, figure.Value);
                lowest[tier] = lowest.ContainsKey(tier)
                    ? Math.Min(lowest[tier], figure.Value)
                    : figure.Value;
                highest[tier] = highest.ContainsKey(tier)
                    ? Math.Max(highest[tier], figure.Value)
                    : figure.Value;

                foreach (var renderer in figure.Instance.GetComponentsInChildren<Renderer>(true))
                {
                    var mesh = PackMesh.On(renderer);

                    if (mesh != null)
                    {
                        meshesOfTier[tier].Add(mesh);
                    }
                }
            }

            var standing = new List<string>();
            var empty = new List<string>();

            for (var tier = 0; tier < EnemyTier.Count; tier++)
            {
                if (meshesOfTier[tier].Count == 0)
                {
                    empty.Add("tier " + tier);
                    continue;
                }

                standing.Add(
                    "tier " + tier + " -> " + modelOfTier[tier] + " on numbers "
                    + lowest[tier] + " to " + highest[tier]);
            }

            report.Append("\n  the enemy silhouettes standing on this level: ")
                .Append(string.Join(", ", standing.ToArray()));

            var shared = new List<string>();

            for (var tier = 0; tier < EnemyTier.Count; tier++)
            {
                for (var other = tier + 1; other < EnemyTier.Count; other++)
                {
                    foreach (var mesh in meshesOfTier[tier])
                    {
                        if (meshesOfTier[other].Contains(mesh))
                        {
                            shared.Add("tiers " + tier + " and " + other + " both show " + mesh.name);
                            break;
                        }
                    }
                }
            }

            var failures = 0;

            failures += Assert(
                report,
                EnemyTier.Count == 3 && CharacterCast.MeshesOf(PartStyle.Enemy).Count == 3,
                "the enemy ramp is three bands wide and the pack dresses each of them on a mesh of "
                + "its own, so the fourth skeleton is left to the boss alone",
                EnemyTier.Count + " bands over "
                + CharacterCast.MeshesOf(PartStyle.Enemy).Count + " enemy meshes, promoting at "
                + string.Join("/", Numbers(EnemyTier.Thresholds)));

            failures += Assert(
                report,
                empty.Count == 0,
                "an enemy of every band stands on this level, so the whole ramp is on screen at once",
                empty.Count == 0
                    ? string.Join("; ", standing.ToArray())
                    : "no enemy stands in " + string.Join(", ", empty.ToArray()));

            failures += Assert(
                report,
                shared.Count == 0 && standing.Count == EnemyTier.Count,
                "no two enemy bands share a single mesh, so a band reads off the silhouette with the "
                + "badge covered",
                shared.Count == 0
                    ? standing.Count + " bands, each on a mesh of its own"
                    : string.Join("; ", shared.ToArray()));

            return failures;
        }

        static string[] Numbers(IReadOnlyList<int> values)
        {
            var named = new string[values.Count];

            for (var slot = 0; slot < values.Count; slot++)
            {
                named[slot] = values[slot].ToString(CultureInfo.InvariantCulture);
            }

            return named;
        }

        static int EveryBandIsPhotographedWithItsBadgeCovered(
            IList<int> shot, StringBuilder report)
        {
            var missing = new List<string>();

            for (var tier = 0; tier < EnemyTier.Count; tier++)
            {
                if (!shot.Contains(tier) || !File.Exists(SilhouettePath + tier + ".png"))
                {
                    missing.Add("tier " + tier);
                }
            }

            return Assert(
                report,
                missing.Count == 0,
                "every band is photographed on the same framing with every badge in the world switched "
                + "off, so the three silhouettes can be compared with no number to read",
                missing.Count == 0
                    ? shot.Count + " shots at " + SilhouettePath + "N.png"
                    : "nothing was written for " + string.Join(", ", missing.ToArray()));
        }

        static List<int> Silhouettes(
            CameraRig rig,
            Camera lens,
            LevelGraph graph,
            IDictionary<string, Transform> byName,
            GameObject root)
        {
            var hidden = new List<GameObject>();

            foreach (var badge in root.GetComponentsInChildren<NumberBadge>(true))
            {
                if (!badge.gameObject.activeSelf)
                {
                    continue;
                }

                badge.gameObject.SetActive(false);
                hidden.Add(badge.gameObject);
            }

            var shot = new List<int>();

            foreach (var figure in Adversaries(graph, byName))
            {
                if (figure.Style != PartStyle.Enemy)
                {
                    continue;
                }

                var tier = EnemyTier.Of(figure.Value);

                if (shot.Contains(tier))
                {
                    continue;
                }

                shot.Add(tier);
                var ground = figure.Figure.Ground;
                var size = IsoProjection.TileEdge * PortraitSize;
                rig.Hold(new CameraFraming(
                    new WorldPoint(ground.X, ground.Y + size * 0.25f, ground.Z), size));
                PreviewFilm.Shoot(lens, SilhouettePath + tier + ".png");
            }

            for (var slot = 0; slot < hidden.Count; slot++)
            {
                hidden[slot].SetActive(true);
            }

            return shot;
        }

        static int EveryFigureIsFittedToItsOwnTile(
            LevelGraph graph,
            IDictionary<string, Transform> byName,
            IDictionary<PartModel, ISet<Mesh>> packs,
            StringBuilder report)
        {
            var figures = 0;
            var standing = 0;
            var footed = 0;
            var inside = 0;
            var complaint = new List<string>();
            var worst = 0f;

            foreach (var figure in Adversaries(graph, byName))
            {
                figures++;
                var model = CharacterCast.MeshOf(figure.Style, figure.Value);

                if (model == PartModel.None)
                {
                    if (complaint.Count < 6)
                    {
                        complaint.Add(figure.Name + " wears no mesh to measure");
                    }

                    continue;
                }

                var box = PackMesh.Wearing(figure.Instance, Pack(packs, model));
                var scale = figure.Figure.transform.localScale.x / FigureFit.ScaleOf(model);
                var ground = figure.Figure.Ground;
                var wanted = FigureFit.StandingHeight(model, scale);
                var half = IsoProjection.TileEdge * 0.5f;

                if (Math.Abs(box.size.y - wanted) <= Epsilon)
                {
                    standing++;
                }
                else if (complaint.Count < 6)
                {
                    complaint.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} stands {1:0.#####} against {2:0.#####}",
                        figure.Name,
                        box.size.y,
                        wanted));
                }

                if (Math.Abs(box.min.y - (ground.Y + ArtPacks.BaseOf(model) * FigureFit.ScaleOf(model) * scale))
                    <= Epsilon)
                {
                    footed++;
                }

                var reach = Math.Max(
                    Math.Max(box.max.x - ground.X, ground.X - box.min.x),
                    Math.Max(box.max.z - ground.Z, ground.Z - box.min.z));
                worst = Math.Max(worst, reach / half);

                if (reach <= half + Epsilon)
                {
                    inside++;
                }
            }

            var failures = 0;

            failures += Assert(
                report,
                figures > 0 && standing == figures,
                "every adversary stands exactly the height the pure fit asks of the mesh it wears",
                standing + " of " + figures + " do"
                + (complaint.Count == 0 ? "" : "; " + string.Join("; ", complaint.ToArray())));

            failures += Assert(
                report,
                figures > 0 && footed == figures,
                "every adversary's measured base sits where its pack base puts it above the tile top",
                footed + " of " + figures + " do");

            failures += Assert(
                report,
                figures > 0 && inside == figures,
                "every adversary's whole footprint lies inside the square of the tile it stands on",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} of {1} do; the greediest reaches {2:0.###} of the half tile it is allowed",
                    inside,
                    figures,
                    worst));

            return failures;
        }

        static int NoFigureHidesMoreGroundThanItsCapsule(
            LevelGraph graph,
            IDictionary<string, Transform> byName,
            IDictionary<PartModel, ISet<Mesh>> packs,
            StringBuilder report)
        {
            var bound = IsoProjection.TileEdge * IsoProjection.OcclusionBound * IsoProjection.TileEdge;
            var figures = 0;
            var kinder = 0;
            var withinBound = 0;
            var enemies = 0;
            var complaint = new List<string>();
            var boss = "no boss measured";

            foreach (var figure in Adversaries(graph, byName))
            {
                figures++;
                var model = CharacterCast.MeshOf(figure.Style, figure.Value);
                var box = PackMesh.Wearing(figure.Instance, Pack(packs, model));
                var scale = figure.Figure.transform.localScale.x / FigureFit.ScaleOf(model);
                var depth = IsoProjection.SightReach(box.size.y);
                var hidden = Math.Max(box.size.x, box.size.z) * depth;
                var capsule = FigureFit.HiddenGroundOf(PartModel.None, scale);
                var capsuleDepth = IsoProjection.SightReach(FigureFit.StandingHeight(PartModel.None, scale));

                if (hidden <= capsule && depth <= capsuleDepth)
                {
                    kinder++;
                }
                else if (complaint.Count < 6)
                {
                    complaint.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} hides {1:0.#####} against the capsule's {2:0.#####}, reaching {3:0.#####} "
                        + "against {4:0.#####}",
                        figure.Name,
                        hidden,
                        capsule,
                        depth,
                        capsuleDepth));
                }

                if (figure.Style == PartStyle.Boss)
                {
                    boss = string.Format(
                        CultureInfo.InvariantCulture,
                        "the boss stands at figure scale {0:0.###} rather than {1:0.###}, so it hides "
                        + "{2:0.#####} tiles where the capsule it replaces already hid {3:0.#####} - the "
                        + "area bound of {4:0.##} was never met by the primitive either, and what the mesh "
                        + "owes is to hide less than it did",
                        scale,
                        LevelBlueprintBuilder.FigureScale,
                        hidden,
                        capsule,
                        bound);
                    continue;
                }

                enemies++;

                if (hidden <= bound)
                {
                    withinBound++;
                }
            }

            var failures = 0;

            failures += Assert(
                report,
                figures > 0 && kinder == figures,
                "every adversary hides no more ground than the capsule it replaces, in depth and in area",
                kinder + " of " + figures + " do"
                + (complaint.Count == 0 ? "" : "; " + string.Join("; ", complaint.ToArray())));

            failures += Assert(
                report,
                enemies > 0 && withinBound == enemies,
                "every enemy hides at most " + IsoProjection.OcclusionBound.ToString(
                    "0.##", CultureInfo.InvariantCulture)
                + " of a tile of ground, which is the wall work's depth bound read as an area because a "
                + "wall spans a whole tile edge and a figure spans a fraction of one",
                withinBound + " of " + enemies + " do");

            report.Append("\n  ").Append(boss);

            return failures;
        }

        static int TheStripTakesTheSparesAndLeavesTheSilhouette(
            LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            var figures = 0;
            var whole = 0;
            var complaint = new List<string>();
            var census = new List<string>();
            var counted = new List<PartModel>();

            foreach (var figure in Adversaries(graph, byName))
            {
                figures++;
                var model = CharacterCast.MeshOf(figure.Style, figure.Value);
                var wanted = Stripped(model);
                var shown = Shown(figure.Instance, false);

                if (string.Join("+", shown.ToArray()) == string.Join("+", wanted.ToArray()))
                {
                    whole++;
                }
                else if (complaint.Count < 6)
                {
                    complaint.Add(
                        figure.Name + " shows " + string.Join("+", shown.ToArray())
                        + " where its bared asset leaves " + string.Join("+", wanted.ToArray()));
                }

                if (!counted.Contains(model))
                {
                    counted.Add(model);
                    var bolted = Shown(figure.Instance, true);
                    census.Add(
                        model + " keeps " + shown.Count + " pieces, "
                        + (bolted.Count == 0
                            ? "none of them bolted"
                            : string.Join("/", bolted.ToArray()) + " bolted to its bones"));
                }
            }

            report.Append("\n  the cast census after the strip: ")
                .Append(string.Join("; ", census.ToArray()));

            return Assert(
                report,
                figures > 0 && whole == figures,
                "the strip takes only what hangs off the pack's slot node, so every piece that gives a "
                + "cast member its silhouette - the boss's hat among them - is still on the figure",
                whole + " of " + figures + " do"
                + (complaint.Count == 0 ? "" : "; " + string.Join("; ", complaint.ToArray())));
        }

        static List<string> Shown(Transform instance, bool boltedOnly)
        {
            var names = new List<string>();

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var mesh = PackMesh.On(renderer);

                if (mesh != null && (!boltedOnly || !(renderer is SkinnedMeshRenderer)))
                {
                    names.Add(mesh.name);
                }
            }

            names.Sort(StringComparer.Ordinal);

            return names;
        }

        static List<string> Stripped(PartModel model)
        {
            var path = WorldModels.AssetPathOf(model);
            var prefab = path == null ? null : Resources.Load<GameObject>(path);
            var names = new List<string>();

            if (prefab == null)
            {
                return names;
            }

            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                var mesh = PackMesh.On(renderer);

                if (mesh != null && !Slotted(renderer.transform))
                {
                    names.Add(mesh.name);
                }
            }

            names.Sort(StringComparer.Ordinal);

            return names;
        }

        static bool Slotted(Transform node)
        {
            for (var walk = node; walk != null; walk = walk.parent)
            {
                if (walk.name.StartsWith(ArtPacks.CastSlotNode, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        static int BadgesStayLegibleOverTheFigures(
            LevelGraph graph,
            IDictionary<string, Transform> byName,
            IDictionary<PartModel, ISet<Mesh>> packs,
            StringBuilder report)
        {
            var badges = 0;
            var clear = 0;
            var sprited = 0;
            var materials = new List<Material>();
            var complaint = new List<string>();

            foreach (var figure in Adversaries(graph, byName))
            {
                Transform carrier;
                if (!byName.TryGetValue(PartNames.Badge(figure.NodeId), out carrier))
                {
                    complaint.Add(PartNames.Badge(figure.NodeId) + " is not in the world");
                    continue;
                }

                badges++;
                var sprite = carrier.GetComponent<SpriteRenderer>();
                var model = CharacterCast.MeshOf(figure.Style, figure.Value);
                var body = PackMesh.Wearing(figure.Instance, Pack(packs, model));

                if (sprite != null && sprite.sprite != null)
                {
                    sprited++;

                    if (!materials.Contains(sprite.sharedMaterial))
                    {
                        materials.Add(sprite.sharedMaterial);
                    }

                    if (sprite.bounds.min.y > body.max.y)
                    {
                        clear++;
                    }
                    else if (complaint.Count < 6)
                    {
                        complaint.Add(string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}'s badge starts at {1:0.#####} over a figure topping out at {2:0.#####}",
                            figure.Name,
                            sprite.bounds.min.y,
                            body.max.y));
                    }
                }
            }

            var failures = 0;

            failures += Assert(
                report,
                badges > 0 && clear == badges,
                "every adversary's badge hangs clear above the figure it labels, so no mesh grew into it",
                clear + " of " + badges + " do"
                + (complaint.Count == 0 ? "" : "; " + string.Join("; ", complaint.ToArray())));

            failures += Assert(
                report,
                badges > 0 && sprited == badges && materials.Count == 1 && materials[0] != null,
                "the badge sprite and material lifecycle is unchanged - every badge still draws one "
                + "sprite through the single shared badge material",
                sprited + " of " + badges + " carry a sprite across " + materials.Count
                + " material" + (materials.Count == 1 ? "" : "s")
                + (materials.Count == 1 && materials[0] != null ? " named " + materials[0].name : ""));

            return failures;
        }

        static int EveryAdversaryReadsAsACreatureAtEveryBand(
            LevelGraph graph,
            IDictionary<string, Transform> byName,
            IDictionary<PartModel, ISet<Mesh>> packs,
            StringBuilder report)
        {
            var swept = 0;
            var reading = 0;
            var measured = 0;
            var agreeing = 0;
            var smallest = float.MaxValue;
            var smallestAt = "nothing";
            var bossFloor = float.MaxValue;
            var enemyCeiling = 0f;
            var tightest = float.MaxValue;
            var complaint = new List<string>();

            foreach (var figure in Adversaries(graph, byName))
            {
                var model = CharacterCast.MeshOf(figure.Style, figure.Value);
                var basis = figure.Figure.transform.localScale.x
                    / FigureFit.ScaleOf(model) / EnemyBands.ScaleOf(figure.Figure.Band);

                measured++;
                var box = PackMesh.Wearing(figure.Instance, Pack(packs, model));
                var showing = LevelFraming.ShareOfScreen(box.size.y, LevelFraming.PlaySize)
                    * ScreenFrame.Height;
                var wanted = FigureReadability.PixelsShowing(
                    model, basis * EnemyBands.ScaleOf(figure.Figure.Band));

                var slack = LevelFraming.ShareOfScreen(Epsilon, LevelFraming.PlaySize)
                    * ScreenFrame.Height;

                if (Math.Abs(showing - wanted) <= slack)
                {
                    agreeing++;
                }
                else if (complaint.Count < 6)
                {
                    complaint.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} measures {1:0.#} pixels where the pure fit reads {2:0.#}",
                        figure.Name,
                        showing,
                        wanted));
                }

                foreach (EnemyBand band in Enum.GetValues(typeof(EnemyBand)))
                {
                    var scale = basis * EnemyBands.ScaleOf(band);
                    var pixels = FigureReadability.PixelsShowing(model, scale);

                    swept++;

                    if (FigureReadability.Reads(model, scale))
                    {
                        reading++;
                    }
                    else if (complaint.Count < 6)
                    {
                        complaint.Add(string.Format(
                            CultureInfo.InvariantCulture,
                            "{0} in band {1} stands {2:0.#} pixels",
                            figure.Name,
                            band,
                            pixels));
                    }

                    if (pixels < smallest)
                    {
                        smallest = pixels;
                        smallestAt = figure.Name + " in band " + band;
                    }

                    var height = FigureFit.StandingHeight(model, scale);

                    if (figure.Style == PartStyle.Boss)
                    {
                        bossFloor = Math.Min(bossFloor, height);
                    }
                    else
                    {
                        enemyCeiling = Math.Max(enemyCeiling, height);
                    }
                }
            }

            if (enemyCeiling > 0f && bossFloor < float.MaxValue)
            {
                tightest = bossFloor / enemyCeiling;
            }

            var failures = 0;

            failures += Assert(
                report,
                swept > 0 && reading == swept,
                "no adversary on the level falls below the readability floor at any band the player's "
                + "own number can put it in, so a trivial enemy still reads as a creature rather than a "
                + "speck",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} of {1} band readings clear the floor of {2:0.#} pixels of the {3} tall frame, "
                    + "which is {4:0.###} of screen height; the smallest is {5} at {6:0.#} pixels"
                    + "{7}",
                    reading,
                    swept,
                    FigureReadability.ReadablePixels,
                    ScreenFrame.Height,
                    FigureReadability.ShareOfScreen,
                    smallestAt,
                    smallest,
                    complaint.Count == 0 ? "" : "; " + string.Join("; ", complaint.ToArray())));

            failures += Assert(
                report,
                measured > 0 && agreeing == measured,
                "the floor is read off the framing rather than off a pinned scale - every adversary's "
                + "measured mesh fills the share of screen height the pure reading of the play framing "
                + "asks of it",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} of {1} agree at a play framing of {2:0.#####}, where a figure of {3:0.#####} "
                    + "metres is the floor",
                    agreeing,
                    measured,
                    LevelFraming.PlaySize,
                    FigureReadability.Height));

            failures += Assert(
                report,
                tightest > 1.2f,
                "the boss stands taller than every enemy on the level at every pairing of bands the two "
                + "can be read in at once, not only when both are read against the same number",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "the boss at its smallest band stands {0:0.#####} against {1:0.#####} for the "
                    + "tallest enemy at its largest, which is {2:0.###} times as tall",
                    bossFloor,
                    enemyCeiling,
                    tightest));

            return failures;
        }

        static int EveryAdversaryStillReadsOnceThePlayerHasOutgrownIt(
            LevelGraph graph,
            IDictionary<string, Transform> byName,
            IDictionary<PartModel, ISet<Mesh>> packs,
            StringBuilder report)
        {
            var read = 0;
            var reading = 0;
            var trivial = 0;
            var smallest = float.MaxValue;
            var smallestAt = "nothing";
            var complaint = new List<string>();

            foreach (var figure in Adversaries(graph, byName))
            {
                read++;

                if (figure.Figure.Band == EnemyBand.Trivial)
                {
                    trivial++;
                }

                var model = CharacterCast.MeshOf(figure.Style, figure.Value);
                var box = PackMesh.Wearing(figure.Instance, Pack(packs, model));
                var pixels = LevelFraming.ShareOfScreen(box.size.y, LevelFraming.PlaySize)
                    * ScreenFrame.Height;

                if (pixels < smallest)
                {
                    smallest = pixels;
                    smallestAt = figure.Name;
                }

                if (pixels >= FigureReadability.ReadablePixels)
                {
                    reading++;
                }
                else if (complaint.Count < 6)
                {
                    complaint.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} measures {1:0.#} pixels",
                        figure.Name,
                        pixels));
                }
            }

            return Assert(
                report,
                read > 0 && trivial > 0 && reading == read,
                "with the run pumped to " + RichPower
                + " and the board reread at its smallest, every adversary's own measured mesh still "
                + "fills the readability floor, so the shrinking end of the band ramp is measured on the "
                + "world and not only on the pure fit",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} of {1} do, {2} of them trivial; the smallest is {3} at {4:0.#} pixels against a "
                    + "floor of {5:0.#}{6}",
                    reading,
                    read,
                    trivial,
                    smallestAt,
                    smallest,
                    FigureReadability.ReadablePixels,
                    complaint.Count == 0 ? "" : "; " + string.Join("; ", complaint.ToArray())));
        }

        static int TheBandMovesTheScaleButNeverTheMeshNorTheColour(
            LevelGraph graph,
            IDictionary<string, Transform> byName,
            IDictionary<PartModel, ISet<Mesh>> packs,
            PowerBadge power,
            StringBuilder report)
        {
            if (power == null)
            {
                return Assert(
                    report, false, "the band moves the scale but never the mesh nor the colour",
                    "the world raised no player badge to move the reading with");
            }

            var before = new Dictionary<string, string>();
            var bandBefore = new Dictionary<string, EnemyBand>();
            var scaleBefore = new Dictionary<string, float>();
            var tintBefore = new Dictionary<string, Color>();

            foreach (var figure in Adversaries(graph, byName))
            {
                before[figure.Name] = MeshNames(figure.Instance);
                bandBefore[figure.Name] = figure.Figure.Band;
                scaleBefore[figure.Name] = figure.Figure.transform.localScale.x;
                tintBefore[figure.Name] = Painted(figure.Figure);
            }

            PowerPump.Settle(power, RichPower);

            var moved = 0;
            var reshaped = 0;
            var repainted = 0;
            var resized = 0;
            var complaint = new List<string>();

            foreach (var figure in Adversaries(graph, byName))
            {
                if (before[figure.Name] != MeshNames(figure.Instance))
                {
                    reshaped++;

                    if (complaint.Count < 6)
                    {
                        complaint.Add(
                            figure.Name + " wore " + before[figure.Name] + " and now wears "
                            + MeshNames(figure.Instance));
                    }
                }

                if (bandBefore[figure.Name] == figure.Figure.Band)
                {
                    continue;
                }

                moved++;

                if (Painted(figure.Figure) != tintBefore[figure.Name])
                {
                    repainted++;
                }

                if (Math.Abs(figure.Figure.transform.localScale.x - scaleBefore[figure.Name]) > 1e-5f)
                {
                    resized++;
                }
            }

            var failures = 0;

            failures += Assert(
                report,
                moved > 0 && reshaped == 0,
                "reading the same enemies against a power of " + RichPower
                + " moves their band without changing one mesh, because the silhouette is a reading of "
                + "the enemy's own number and the band is a reading of the player's",
                moved + " of " + before.Count + " adversaries changed band and " + reshaped
                + " changed mesh" + (complaint.Count == 0 ? "" : "; " + string.Join("; ", complaint.ToArray())));

            failures += Assert(
                report,
                moved > 0 && repainted == 0 && resized == moved,
                "every adversary whose band moved took a new size from it and kept the colour it had, so "
                + "the band owns scale alone and the pack's own texture is left to show through",
                repainted + " repainted and " + resized + " resized of " + moved + " that moved band");

            return failures;
        }

        static int EveryRiggedFigureIsPosedByAnAnimator(
            GameObject root,
            IDictionary<string, Transform> byName,
            CameraRig rig,
            Camera lens,
            LevelGraph graph,
            StringBuilder report)
        {
            var cast = Cast(root, byName);
            var animators = root.GetComponentsInChildren<FigureAnimator>(true);
            var posed = 0;
            var idling = 0;
            var complaint = new List<string>();

            foreach (var member in cast)
            {
                if (member.Driven == null || !member.Driven.IsRigged || !member.Driven.HasClipsToPlay)
                {
                    if (complaint.Count < 6)
                    {
                        complaint.Add(member.Name + (member.Driven == null
                            ? " carries no animator at all, so its rig holds the pose it was bound in"
                            : " carries an animator rigged " + member.Driven.IsRigged + " with a clip loaded "
                                + member.Driven.HasClipsToPlay));
                    }

                    continue;
                }

                posed++;

                if (member.Driven.Act == FigureAct.Idle
                    && member.Driven.Playing != null
                    && member.Driven.Playing.name == CastClips.NameOf(member.Driven.Worn, FigureAct.Idle))
                {
                    idling++;
                }
            }

            var failures = 0;

            failures += Assert(
                report,
                cast.Count > 0 && posed == cast.Count && animators.Length == cast.Count,
                "every figure the world raises - the player, every enemy and the boss alike - carries an "
                + "animator with a clip loaded, so not one rigged mesh is left standing in the pose it was "
                + "bound in",
                posed + " of " + cast.Count + " figures are posed, by the " + animators.Length
                + " animators the whole world holds"
                + (complaint.Count == 0 ? "" : "; " + string.Join("; ", complaint.ToArray())));

            failures += Assert(
                report,
                cast.Count > 0 && idling == cast.Count,
                "every one of them opens on the looping idle clip with nothing having cued it, because the "
                + "still cue is the default cue",
                idling + " of " + cast.Count + " stand on the clip their own pack names for "
                + FigureAct.Idle + ", " + AdventurerClips.Idle + " for the hero and " + SkeletonClips.Idle
                + " for a skeleton");

            if (cast.Count == 0)
            {
                return failures;
            }

            var unsampled = 0;

            foreach (var member in cast)
            {
                if (member.Driven != null && member.Driven.PlayingTime <= 0f)
                {
                    unsampled++;
                }
            }

            failures += Assert(
                report,
                cast.Count > 0 && unsampled == cast.Count,
                "the world hands the whole cast back with its clips loaded but not one of them sampled, so "
                + "every fit and footprint measured above is read off the pose the pack pinned rather than off "
                + "a frame of animation, which is why the epsilon T-33 established is untouched by this ticket",
                unsampled + " of " + cast.Count + " stand at 0s of their clip before a frame is advanced");

            var joints = new Transform[cast.Count][];
            var opening = new Quaternion[cast.Count][];
            var spread = new float[cast.Count];
            var crown = new float[cast.Count];
            var reach = new float[cast.Count];

            for (var slot = 0; slot < cast.Count; slot++)
            {
                joints[slot] = Joints(cast[slot].Body.transform);
                opening[slot] = Pose(joints[slot]);
                crown[slot] = float.MinValue;
            }

            var loop = Math.Max(2, (int)(LongestLoaded(animators) / Frame));
            var subject = Tallest(cast);
            var close = PreviewFilm.Rig(Vector3.zero, CloseRange, CloseFraming);
            var shots = new List<string>();
            var shot = 0;
            var every = Math.Max(1, loop / Sequence);

            Aim(close, cast[subject].Body.transform.position);
            PreviewFilm.Warm(close);

            for (var frame = 0; frame < loop; frame++)
            {
                foreach (var acting in animators)
                {
                    acting.Advance(Frame);
                }

                for (var slot = 0; slot < cast.Count; slot++)
                {
                    var box = Envelope(cast[slot].Body);

                    spread[slot] = Math.Max(spread[slot], Spread(joints[slot], opening[slot]));
                    crown[slot] = Math.Max(crown[slot], box.max.y);
                    reach[slot] = Math.Max(reach[slot], Reach(box, cast[slot].Body.Ground));
                }

                if (shot >= Sequence || frame % every != 0)
                {
                    continue;
                }

                Aim(close, cast[subject].Body.transform.position);
                PreviewFilm.Shoot(
                    close, IdlePath + shot.ToString("00", CultureInfo.InvariantCulture) + ".png");
                shots.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:00} at {1:0.###}s of the idle",
                    shot,
                    cast[subject].Driven == null ? 0f : cast[subject].Driven.PlayingTime));
                shot++;
            }

            WorldObjects.Destroy(close.gameObject);

            var half = IsoProjection.TileEdge * 0.5f;
            var stirred = 0;
            var inside = 0;
            var badges = 0;
            var clear = 0;
            var stillest = float.MaxValue;
            var closest = float.MaxValue;
            var greediest = 0f;
            var stirComplaint = new List<string>();
            var reachComplaint = new List<string>();

            for (var slot = 0; slot < cast.Count; slot++)
            {
                if (spread[slot] > StirFloor)
                {
                    stirred++;
                }
                else if (stirComplaint.Count < 6)
                {
                    stirComplaint.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} turned {1:0.###} degrees over the whole loop",
                        cast[slot].Name,
                        spread[slot]));
                }

                stillest = Math.Min(stillest, spread[slot]);
                greediest = Math.Max(greediest, reach[slot] / half);

                if (reach[slot] <= half + Epsilon)
                {
                    inside++;
                }
                else if (reachComplaint.Count < 6)
                {
                    reachComplaint.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} reached {1:0.#####} of the {2:0.#####} it is allowed",
                        cast[slot].Name,
                        reach[slot],
                        half));
                }

                if (cast[slot].Badge == null)
                {
                    continue;
                }

                var sprite = cast[slot].Badge.GetComponent<SpriteRenderer>();

                if (sprite == null || sprite.sprite == null)
                {
                    continue;
                }

                badges++;
                var gap = sprite.bounds.min.y - crown[slot];
                closest = Math.Min(closest, gap);

                if (gap > 0f)
                {
                    clear++;
                }
            }

            failures += Assert(
                report,
                cast.Count > 0 && stirred == cast.Count,
                "one idle loop advanced on every animator at once carries every figure in the cast off the "
                + "pose it opened on, so nothing in the world renders frozen mid-jumping-jack",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} of {1} stirred over {2} frames, the stillest of them by {3:0.##} degrees{4}",
                    stirred,
                    cast.Count,
                    loop,
                    stillest == float.MaxValue ? 0f : stillest,
                    stirComplaint.Count == 0 ? "" : "; " + string.Join("; ", stirComplaint.ToArray())));

            failures += Assert(
                report,
                cast.Count > 0 && inside == cast.Count,
                "the animated figure keeps the tile grid fit its still pose was measured for - at no frame of "
                + "the loop does any figure's whole footprint leave the square of the tile it stands on, "
                + "within the same epsilon T-33 pinned",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} of {1} do at every frame; the greediest reaches {2:0.###} of the half tile it is "
                    + "allowed, against the {3:0.###} the still pose reaches{4}",
                    inside,
                    cast.Count,
                    greediest,
                    0.92f,
                    reachComplaint.Count == 0 ? "" : "; " + string.Join("; ", reachComplaint.ToArray())));

            failures += Assert(
                report,
                badges > 0 && clear == badges,
                "every badge still hangs clear above the highest its figure's own meshes reached at any frame "
                + "of the loop, so an animated cast stays as legible as a still one and the badge sprite and "
                + "material lifecycle is unchanged",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} of {1} badges do, the tightest by {2:0.####} of a tile",
                    clear,
                    badges,
                    closest == float.MaxValue ? 0f : closest));

            failures += Assert(
                report,
                shot == Sequence,
                "the tallest figure in the cast is photographed close up as a frame sequence a human can read "
                + "the breath off, which is what a whole level shot at once is too far away to show",
                shot + " of " + Sequence + " frames of " + cast[subject].Name + ", " + IdlePath + "NN.png: "
                + string.Join(", ", shots.ToArray()));

            rig.Begin(graph);
            rig.Skip();
            PreviewFilm.Warm(lens);
            PreviewFilm.Shoot(lens, AlivePath);

            report.Append("\n  the animated cast is photographed at ").Append(AlivePath)
                .Append(", against the same level shot before a frame was advanced at ").Append(LevelPath);
            report.Append(Cost(animators, cast));

            return failures;
        }

        struct PosedFigure
        {
            public string Name;

            public Figure Body;

            public FigureAnimator Driven;

            public Transform Badge;
        }

        static List<PosedFigure> Cast(GameObject root, IDictionary<string, Transform> byName)
        {
            var cast = new List<PosedFigure>();

            foreach (var body in root.GetComponentsInChildren<Figure>(true))
            {
                var wanted = BadgeOf(body.name);
                Transform badge = null;

                if (wanted != null)
                {
                    byName.TryGetValue(wanted, out badge);
                }

                cast.Add(new PosedFigure
                {
                    Name = body.name,
                    Body = body,
                    Driven = body.GetComponent<FigureAnimator>(),
                    Badge = badge
                });
            }

            return cast;
        }

        static string BadgeOf(string node)
        {
            var tail = node == null ? -1 : node.LastIndexOf('_');

            int nodeId;
            if (tail < 0 || !int.TryParse(
                    node.Substring(tail + 1),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out nodeId))
            {
                return null;
            }

            return PartNames.Node(nodeId) == node ? PartNames.Badge(nodeId) : null;
        }

        static Bounds Envelope(Component figure)
        {
            var box = new Bounds();
            var first = true;

            foreach (var renderer in figure.GetComponentsInChildren<Renderer>(true))
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

        static Transform[] Joints(Transform figure)
        {
            var joints = new List<Transform>();

            foreach (var node in figure.GetComponentsInChildren<Transform>(true))
            {
                if (!ReferenceEquals(node, figure))
                {
                    joints.Add(node);
                }
            }

            return joints.ToArray();
        }

        static Quaternion[] Pose(Transform[] joints)
        {
            var pose = new Quaternion[joints.Length];

            for (var slot = 0; slot < joints.Length; slot++)
            {
                pose[slot] = joints[slot].localRotation;
            }

            return pose;
        }

        static float Spread(Transform[] joints, Quaternion[] opening)
        {
            var turned = 0f;

            for (var slot = 0; slot < joints.Length; slot++)
            {
                turned += Quaternion.Angle(opening[slot], joints[slot].localRotation);
            }

            return turned;
        }

        static int Tallest(IList<PosedFigure> cast)
        {
            var tallest = 0;
            var top = float.MinValue;

            for (var slot = 0; slot < cast.Count; slot++)
            {
                var height = Envelope(cast[slot].Body).size.y;

                if (height > top)
                {
                    top = height;
                    tallest = slot;
                }
            }

            return tallest;
        }

        static void Aim(Camera lens, Vector3 centre)
        {
            lens.transform.position = centre - lens.transform.forward * CloseRange;
        }

        static float Reach(Bounds box, WorldPoint ground)
        {
            return Math.Max(
                Math.Max(box.max.x - ground.X, ground.X - box.min.x),
                Math.Max(box.max.z - ground.Z, ground.Z - box.min.z));
        }

        static float LongestLoaded(FigureAnimator[] animators)
        {
            var longest = 0f;

            foreach (var acting in animators)
            {
                longest = Math.Max(longest, acting.PlayingSeconds);
            }

            return longest;
        }

        static string Cost(FigureAnimator[] animators, IList<PosedFigure> cast)
        {
            var whole = new double[CostRounds];
            var alone = new double[CostRounds];

            for (var round = 0; round < CostRounds; round++)
            {
                whole[round] = Milliseconds(animators, animators.Length);
                alone[round] = Milliseconds(animators, Math.Min(1, animators.Length));
            }

            var skins = 0;
            var bones = 0;
            var vertices = 0;

            foreach (var member in cast)
            {
                foreach (var skin in member.Body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skins++;
                    bones += skin.bones == null ? 0 : skin.bones.Length;
                    vertices += skin.sharedMesh == null ? 0 : skin.sharedMesh.vertexCount;
                }
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  frame cost of the whole cast, {0} rounds of {1} frames each, alternated so drift is "
                + "shared: advancing and sampling all {2} figures costs {3} against {4} for the single figure "
                + "T-34 left animated, so this ticket adds about {5:0.####} ms of processor work a frame to a "
                + "16.667 ms budget"
                + "\n  what the cast skins: {6} skinned meshes over {7} bone bindings and {8} vertices"
                + "\n  what this does not prove: no Android device is attached to this machine, so these are "
                + "editor numbers on a desktop processor and not a device frame rate. They bound the processor "
                + "work the animation adds and say nothing about the device's own skinning path or its GPU cost",
                CostRounds,
                CostFrames,
                animators.Length,
                Spans(whole),
                Spans(alone),
                Mean(whole) - Mean(alone),
                skins,
                bones,
                vertices);
        }

        static double Milliseconds(FigureAnimator[] animators, int count)
        {
            var clock = Stopwatch.StartNew();

            for (var frame = 0; frame < CostFrames; frame++)
            {
                for (var slot = 0; slot < count; slot++)
                {
                    animators[slot].Advance(Frame);
                }
            }

            clock.Stop();

            return clock.Elapsed.TotalMilliseconds / CostFrames;
        }

        static string Spans(double[] rounds)
        {
            var lowest = double.MaxValue;
            var highest = double.MinValue;

            foreach (var round in rounds)
            {
                lowest = Math.Min(lowest, round);
                highest = Math.Max(highest, round);
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:0.####} ms a frame ({1:0.####} to {2:0.####})",
                Mean(rounds),
                lowest,
                highest);
        }

        static double Mean(double[] rounds)
        {
            var total = 0d;

            foreach (var round in rounds)
            {
                total += round;
            }

            return rounds.Length == 0 ? 0d : total / rounds.Length;
        }

        static void Portraits(
            CameraRig rig,
            Camera lens,
            LevelGraph graph,
            IDictionary<string, Transform> byName,
            IDictionary<PartModel, ISet<Mesh>> packs)
        {
            var shot = new List<PartModel>();

            foreach (var figure in Adversaries(graph, byName))
            {
                var model = CharacterCast.MeshOf(figure.Style, figure.Value);

                if (shot.Contains(model))
                {
                    continue;
                }

                shot.Add(model);
                var box = PackMesh.Wearing(figure.Instance, Pack(packs, model));
                var ground = figure.Figure.Ground;
                var size = (box.size.y > 0f ? box.size.y : IsoProjection.TileEdge) * PortraitSize;
                rig.Hold(new CameraFraming(
                    new WorldPoint(ground.X, ground.Y + size * 0.25f, ground.Z), size));
                PreviewFilm.Shoot(lens, PortraitPath + model + ".png");
            }
        }

        struct CastFigure
        {
            public string Name;

            public int NodeId;

            public int Value;

            public PartStyle Style;

            public Transform Instance;

            public EnemyFigure Figure;
        }

        static IEnumerable<CastFigure> Adversaries(
            LevelGraph graph, IDictionary<string, Transform> byName)
        {
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
                    continue;
                }

                var figure = instance.GetComponent<EnemyFigure>();
                if (figure == null)
                {
                    continue;
                }

                yield return new CastFigure
                {
                    Name = prop.Name,
                    NodeId = node.Id,
                    Value = node.Value,
                    Style = prop.Style,
                    Instance = instance,
                    Figure = figure
                };
            }
        }

        static string MeshNames(Transform instance)
        {
            var names = new List<string>();

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var mesh = PackMesh.On(renderer);

                if (mesh != null)
                {
                    names.Add(mesh.name);
                }
            }

            names.Sort(StringComparer.Ordinal);

            return string.Join("+", names.ToArray());
        }

        static Material Skin(EnemyFigure figure)
        {
            foreach (var renderer in figure.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                return renderer.sharedMaterial;
            }

            return null;
        }

        static Color Painted(EnemyFigure figure)
        {
            var skin = Skin(figure);

            if (skin == null || !skin.HasProperty(BaseColour))
            {
                return Color.black;
            }

            return skin.GetColor(BaseColour);
        }

        static int Overrides(EnemyFigure figure)
        {
            var found = 0;

            foreach (var renderer in figure.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.HasPropertyBlock())
                {
                    found++;
                }
            }

            return found;
        }

        static int EveryAdversaryShowsThePacksOwnTexture(
            LevelGraph graph, IDictionary<string, Transform> byName, StringBuilder report)
        {
            var read = 0;
            var atlassed = 0;
            var white = 0;
            var overridden = 0;
            var complaint = new List<string>();

            foreach (var figure in Adversaries(graph, byName))
            {
                read++;

                var skin = Skin(figure.Figure);
                var atlas = skin != null && skin.HasProperty(BaseMap) ? skin.GetTexture(BaseMap) : null;

                if (atlas != null)
                {
                    atlassed++;
                }

                if (Painted(figure.Figure) == Color.white)
                {
                    white++;
                }
                else if (complaint.Count < 6)
                {
                    complaint.Add(figure.Name + " reads " + Painted(figure.Figure));
                }

                overridden += Overrides(figure.Figure);
            }

            var failures = 0;

            failures += Assert(
                report,
                read > 0 && atlassed == read,
                "every adversary wears a material bound to the skeleton pack's atlas, so what shows is the "
                + "pack's own texture",
                atlassed + " of " + read + " do");

            failures += Assert(
                report,
                read > 0 && white == read,
                "that material multiplies the atlas by white rather than by a palette colour, so no flat "
                + "tint sits over the mesh",
                white + " of " + read + " do"
                + (complaint.Count == 0 ? "" : "; " + string.Join("; ", complaint.ToArray())));

            failures += Assert(
                report,
                overridden == 0,
                "no renderer under any adversary carries a property block, so no second colour is laid "
                + "over the material either",
                overridden + " renderers across " + read + " adversaries do");

            return failures;
        }

        static ISet<Mesh> Pack(IDictionary<PartModel, ISet<Mesh>> packs, PartModel model)
        {
            ISet<Mesh> pack;

            return packs.TryGetValue(model, out pack) ? pack : new HashSet<Mesh>();
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
