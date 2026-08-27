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
    public static class EnemyCastCheckCommand
    {
        const long Seed = 20250824L;

        const float Epsilon = DungeonPack.BoundsEpsilon;

        const float PortraitSize = 2.4f;

        const int RichPower = 20000;

        const string LevelPath = "dev/scratch/t-33-enemy-cast-level.png";

        const string PortraitPath = "dev/scratch/t-33-cast-";

        static readonly PartStyle[] StillPrimitive =
        {
            PartStyle.Pillar, PartStyle.Trail, PartStyle.Spark
        };

        public static void Check()
        {
            Wipe(LevelPath);

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                Wipe(PortraitPath + model + ".png");
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
            var byName = new Dictionary<string, Transform>();

            foreach (var node in root.GetComponentsInChildren<Transform>(true))
            {
                byName[node.name] = node;
            }

            var packs = new Dictionary<PartModel, ISet<Mesh>>();

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (!ArtPacks.IsRigged(model))
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
            failures += EveryFigureIsFittedToItsOwnTile(graph, byName, packs, report);
            failures += NoFigureHidesMoreGroundThanItsCapsule(graph, byName, packs, report);
            failures += TheStripTakesTheSparesAndLeavesTheSilhouette(graph, byName, report);
            failures += BadgesStayLegibleOverTheFigures(graph, byName, packs, report);

            Portraits(rig, lens, graph, byName, packs);

            failures += TheBandMovesTheTintAndScaleButNeverTheMesh(graph, byName, packs, power, report);

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
                "exactly the cutscene pillar and the two effects still fall back to a primitive, "
                + "and nothing else does",
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

            for (var tier = 0; tier < VisualTier.Count; tier++)
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

        static int TheBandMovesTheTintAndScaleButNeverTheMesh(
            LevelGraph graph,
            IDictionary<string, Transform> byName,
            IDictionary<PartModel, ISet<Mesh>> packs,
            PowerBadge power,
            StringBuilder report)
        {
            if (power == null)
            {
                return Assert(
                    report, false, "the band moves the tint and the scale but never the mesh",
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
                moved > 0 && repainted == moved && resized == moved,
                "every adversary whose band moved took a new tint and a new size from it, so the band "
                + "owns material and scale exactly as the number owns the mesh",
                repainted + " repainted and " + resized + " resized of " + moved + " that moved band");

            return failures;
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

        static Color Painted(EnemyFigure figure)
        {
            var block = new MaterialPropertyBlock();

            foreach (var renderer in figure.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.GetPropertyBlock(block);

                return block.GetColor("_BaseColor");
            }

            return Color.black;
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
