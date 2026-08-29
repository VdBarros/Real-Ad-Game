using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Game.Domain;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTooling
{
    public static class BadgeAssetCheckCommand
    {
        const long FirstSeed = 20250824L;

        const long SecondSeed = 20250825L;

        const float Frame = 1f / 60f;

        const float Tolerance = 1e-4f;

        const long CrowdedSeed = 20250850L;

        const float SameDepth = 1e-3f;

        const string CrowdedPath = "dev/scratch/t-140-badges-crowded.png";

        const string ResolvedPath = "dev/scratch/t-140-badges-resolved.png";

        const string PiledPath = "dev/scratch/t-140-badges-piled.png";

        const int Pile = 5;

        static readonly int[] Ladder = { 9, 47, 615, 4200 };

        static bool lit;

        public static void Check()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var builder = new WorldBuilder();

            var firstGraph = LevelGenerator.Generate(FirstSeed, MazePreset.Ship).Graph;
            var first = builder.Build(firstGraph);
            var firstSprites = SpritesOn(first);
            var firstMaterials = MaterialsOn(first);
            ReportBadgeWidths(FirstSeed, first);
            NoMultiplierGateWearsABadge(FirstSeed, firstGraph, first);
            NoBadgeBuriesAnother(FirstSeed, firstGraph, builder, "as the level opens");
            WorldObjects.Destroy(first);

            var secondGraph = LevelGenerator.Generate(SecondSeed, MazePreset.Ship).Graph;
            var second = builder.Build(secondGraph);
            var secondSprites = SpritesOn(second);
            var secondMaterials = MaterialsOn(second);
            ReportBadgeWidths(SecondSeed, second);
            NoMultiplierGateWearsABadge(SecondSeed, secondGraph, second);
            ReportBadgeShapes(second);
            ReportPlayerGrowth(builder.PlayerBadge);
            NoBadgeBuriesAnother(SecondSeed, secondGraph, builder, "with the player counted up to 4200");

            firstSprites.AddRange(secondSprites);
            firstMaterials.AddRange(secondMaterials);

            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "badge assets: {0} badges over two levels share {1} sprites and {2} materials",
                secondSprites.Count,
                Distinct(firstSprites),
                Distinct(firstMaterials)));

            var sprite = secondSprites[0];
            var material = secondMaterials[0];

            WorldObjects.Destroy(second);
            Debug.Log("with the level destroyed the sprite is " + Fate(sprite)
                + " and the material is " + Fate(material));

            builder.Dispose();
            Debug.Log("with the builder disposed the sprite is " + Fate(sprite)
                + " and the material is " + Fate(material));

            ACrowdedLevelComesApart();
        }


        static void NoBadgeBuriesAnother(long seed, LevelGraph graph, WorldBuilder builder, string moment)
        {
            var crowd = builder.Crowd;
            crowd.Settle();

            var framing = LevelFraming.Play(TightestPlace(crowd, LevelFraming.Centre(graph)));
            var badges = Readable(crowd, framing);
            var buried = 0;
            var touching = 0;
            var tightest = float.MaxValue;

            for (var slot = 0; slot < badges.Count; slot++)
            {
                for (var other = slot + 1; other < badges.Count; other++)
                {
                    var gap = GapBetween(badges[slot], badges[other]);
                    tightest = gap < tightest ? gap : tightest;

                    if (gap >= 0f)
                    {
                        continue;
                    }

                    touching++;
                    var behind = badges[slot].Depth > badges[other].Depth ? badges[slot] : badges[other];

                    if (behind.Opacity < 1f - Tolerance)
                    {
                        continue;
                    }

                    buried++;
                    Debug.LogError(
                        "FAIL: seed " + seed + " " + moment + " draws " + badges[slot].Name + " over "
                        + badges[other].Name + " at the play framing, overlapping by "
                        + (-gap).ToString("0.#") + "px, and the farther of the two is at full strength.");
                }
            }

            NoBadgeSharesADrawOrder(seed, badges);

            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "seed {0} {1}: {2} badges, {3} stacked and {4} faded to clear one another, {5} pairs still "
                + "touching ({6} of them buried), tightest gap {7:0.#}px at the play framing",
                seed,
                moment,
                badges.Count,
                crowd.Stack.Stacked,
                crowd.Stack.Faded,
                touching,
                buried,
                tightest));
        }

        static void NoBadgeSharesADrawOrder(long seed, IReadOnlyList<BadgeRect> badges)
        {
            for (var slot = 0; slot < badges.Count; slot++)
            {
                for (var other = slot + 1; other < badges.Count; other++)
                {
                    if (badges[slot].Order == badges[other].Order)
                    {
                        Debug.LogError(
                            "FAIL: seed " + seed + " draws " + badges[slot].Name + " and "
                            + badges[other].Name + " both at order " + badges[slot].Order
                            + ", so which one covers the other is left to chance.");
                        continue;
                    }

                    if (Mathf.Abs(badges[slot].Depth - badges[other].Depth) < SameDepth)
                    {
                        continue;
                    }

                    var nearer = badges[slot].Depth < badges[other].Depth ? badges[slot] : badges[other];
                    var farther = badges[slot].Depth < badges[other].Depth ? badges[other] : badges[slot];

                    if (nearer.Order <= farther.Order)
                    {
                        Debug.LogError(
                            "FAIL: seed " + seed + " draws the nearer " + nearer.Name + " at order "
                            + nearer.Order + " behind the farther " + farther.Name + " at order "
                            + farther.Order + ".");
                    }
                }
            }
        }

        static void ACrowdedLevelComesApart()
        {
            var graph = LevelGenerator.Generate(CrowdedSeed, MazePreset.Ship).Graph;
            var builder = new WorldBuilder();
            var root = builder.Build(graph);
            var crowd = builder.Crowd;

            var shoved = ShoveTogether(crowd);
            crowd.Flatten();
            var crowded = Touching(crowd, LevelFraming.Play(shoved));
            Film(shoved, CrowdedPath);

            crowd.Settle();
            Film(shoved, ResolvedPath);

            if (crowded == 0)
            {
                Debug.LogError(
                    "FAIL: shoving two neighbouring badges onto one another left them not overlapping at all, "
                    + "so nothing here proves a crowd is ever untangled.");
            }

            NoBadgeBuriesAnother(
                CrowdedSeed, graph, builder, "with two neighbouring badges shoved onto one another");

            Debug.Log(
                "seed " + CrowdedSeed + ": left unstacked, that pair overlaps in " + crowded
                + " place at the play framing; the two shots are " + CrowdedPath + " and " + ResolvedPath);

            var heaped = Heap(crowd);
            Film(heaped, PiledPath);

            if (crowd.Stack.Faded == 0)
            {
                Debug.LogError(
                    "FAIL: " + Pile + " badges heaped on one spot were all stacked with room to spare, so the "
                    + "fade that catches what stacking cannot is never exercised.");
            }

            NoBadgeBuriesAnother(CrowdedSeed, graph, builder, "with " + Pile + " badges heaped on one spot");

            Debug.Log(
                "seed " + CrowdedSeed + ": heaped " + Pile + " badges on one spot, " + crowd.Stack.Stacked
                + " stacked and " + crowd.Stack.Faded + " of those faded as well; the shot is " + PiledPath);

            WorldObjects.Destroy(root);
            builder.Dispose();
        }

        static WorldPoint ShoveTogether(CrowdBoard crowd)
        {
            crowd.Settle();

            var badges = crowd.Badges;
            var closest = float.MaxValue;
            var here = 0;
            var there = 1;

            for (var slot = 0; slot < badges.Count; slot++)
            {
                for (var other = slot + 1; other < badges.Count; other++)
                {
                    var apart = Flat(Home(badges[slot]), Home(badges[other]));

                    if (apart >= closest)
                    {
                        continue;
                    }

                    closest = apart;
                    here = slot;
                    there = other;
                }
            }

            var anchor = Home(badges[here]);
            var right = IsoProjection.CameraRight;
            var up = IsoProjection.CameraUp;

            badges[there].transform.localPosition = new Vector3(
                anchor.X + right.X * 0.14f + up.X * 0.1f,
                anchor.Y + right.Y * 0.14f + up.Y * 0.1f,
                anchor.Z + right.Z * 0.14f + up.Z * 0.1f);

            crowd.Settle();

            return anchor;
        }

        static WorldPoint Heap(CrowdBoard crowd)
        {
            crowd.Settle();

            var badges = crowd.Badges;
            var anchor = Home(badges[0]);
            var forward = IsoProjection.CameraForward;
            var up = IsoProjection.CameraUp;

            for (var slot = 1; slot < Pile && slot < badges.Count; slot++)
            {
                badges[slot].transform.localPosition = new Vector3(
                    anchor.X + forward.X * slot * 0.05f + up.X * slot * 0.03f,
                    anchor.Y + forward.Y * slot * 0.05f + up.Y * slot * 0.03f,
                    anchor.Z + forward.Z * slot * 0.05f + up.Z * slot * 0.03f);
            }

            crowd.Settle();

            return anchor;
        }

        static WorldPoint Home(NumberBadge badge)
        {
            var seat = badge.Home;

            return new WorldPoint(seat.x, seat.y, seat.z);
        }

        static float Flat(WorldPoint one, WorldPoint other)
        {
            var across = WorldPoint.Dot(one, IsoProjection.CameraRight)
                - WorldPoint.Dot(other, IsoProjection.CameraRight);
            var up = WorldPoint.Dot(one, IsoProjection.CameraUp) - WorldPoint.Dot(other, IsoProjection.CameraUp);

            return across * across + up * up;
        }

        static int Touching(CrowdBoard crowd, CameraFraming framing)
        {
            var badges = Readable(crowd, framing);
            var pairs = 0;

            for (var slot = 0; slot < badges.Count; slot++)
            {
                for (var other = slot + 1; other < badges.Count; other++)
                {
                    if (GapBetween(badges[slot], badges[other]) < 0f)
                    {
                        pairs++;
                    }
                }
            }

            return pairs;
        }

        static void Film(WorldPoint centre, string path)
        {
            if (!lit)
            {
                PreviewFilm.Sun();
                lit = true;
            }

            var camera = PreviewFilm.Rig(
                new Vector3(centre.X, centre.Y, centre.Z), IsoProjection.CameraBack, LevelFraming.PlaySize);

            PreviewFilm.Warm(camera);
            PreviewFilm.Shoot(camera, path);
            WorldObjects.Destroy(camera.gameObject);
        }

        static WorldPoint TightestPlace(CrowdBoard crowd, WorldPoint fallback)
        {
            var spots = crowd.Spots;
            var closest = float.MaxValue;
            var place = fallback;

            for (var slot = 0; slot < spots.Count; slot++)
            {
                for (var other = slot + 1; other < spots.Count; other++)
                {
                    var across = spots[slot].Across - spots[other].Across;
                    var up = spots[slot].Up - spots[other].Up;
                    var apart = across * across + up * up;

                    if (apart >= closest)
                    {
                        continue;
                    }

                    closest = apart;
                    place = WorldPoint.Between(spots[slot].Anchor, spots[other].Anchor, 0.5f);
                }
            }

            return place;
        }

        static IReadOnlyList<BadgeRect> Readable(CrowdBoard crowd, CameraFraming framing)
        {
            var pixels = ScreenProjection.PixelsPerMetre(framing.OrthographicSize, ScreenFrame.Height);
            var rects = new List<BadgeRect>();

            foreach (var badge in crowd.Badges)
            {
                if (badge == null || !badge.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var plate = badge.GetComponent<SpriteRenderer>();
                var standing = badge.transform.position;
                var worn = badge.transform.lossyScale;
                var here = new WorldPoint(standing.x, standing.y, standing.z);

                rects.Add(new BadgeRect
                {
                    Name = badge.name,
                    Centre = ScreenProjection.Of(framing, here, ScreenFrame.Width, ScreenFrame.Height),
                    HalfWidth = plate.size.x * Mathf.Abs(worn.x) * 0.5f * pixels,
                    HalfHeight = plate.size.y * Mathf.Abs(worn.y) * 0.5f * pixels,
                    Depth = framing.DepthOf(here),
                    Opacity = badge.Opacity,
                    Order = badge.Order
                });
            }

            return rects;
        }

        static float GapBetween(BadgeRect one, BadgeRect other)
        {
            var across = Mathf.Abs(one.Centre.X - other.Centre.X) - (one.HalfWidth + other.HalfWidth);
            var up = Mathf.Abs(one.Centre.Y - other.Centre.Y) - (one.HalfHeight + other.HalfHeight);

            return across > up ? across : up;
        }

        struct BadgeRect
        {
            public string Name;

            public ScreenPoint Centre;

            public float HalfWidth;

            public float HalfHeight;

            public float Depth;

            public float Opacity;

            public int Order;
        }

        static void ReportBadgeWidths(long seed, GameObject root)
        {
            var badges = root.GetComponentsInChildren<NumberBadge>(true);
            var overhanging = 0;
            var cramped = 0;
            var widest = 0f;
            var narrowest = float.MaxValue;

            foreach (var badge in badges)
            {
                var drawn = badge.GetComponent<SpriteRenderer>().size;

                if (drawn.x > badge.SubjectWidth + Tolerance)
                {
                    overhanging++;
                }

                if (badge.Cells < BadgeMetrics.MinimumCells - Tolerance || drawn.x <= 0f || drawn.y <= 0f)
                {
                    cramped++;
                }

                widest = drawn.x > widest ? drawn.x : widest;
                narrowest = drawn.x < narrowest ? drawn.x : narrowest;
            }

            if (overhanging > 0)
            {
                Debug.LogError(
                    "FAIL: " + overhanging + " of " + badges.Length + " badges on seed " + seed
                    + " are wider than the character they label, so the clamp did not hold.");
            }

            if (cramped > 0)
            {
                Debug.LogError(
                    "FAIL: " + cramped + " of " + badges.Length + " badges on seed " + seed
                    + " fell under one legible glyph, so the minimum clamp did not hold.");
            }

            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "seed {0}: {1} badges span {2:0.###} to {3:0.###} units, none wider than the figure under it "
                + "(a badge sized to the whole level would be {4:0.###})",
                seed,
                badges.Length,
                narrowest,
                widest,
                BadgeMetrics.WidthFor(5)));
        }

        static void NoMultiplierGateWearsABadge(long seed, LevelGraph graph, GameObject root)
        {
            var gates = 0;
            var wearing = 0;

            foreach (var node in graph.Decisions.Nodes)
            {
                if (node.Type != NodeType.Multiplier)
                {
                    continue;
                }

                gates++;
                var named = PartNames.Badge(node.Id);

                foreach (var badge in root.GetComponentsInChildren<NumberBadge>(true))
                {
                    if (badge.name == named)
                    {
                        wearing++;
                    }
                }

                var prop = Named(root, PartNames.Node(node.Id));

                if (prop == null)
                {
                    Debug.LogError(
                        "FAIL: seed " + seed + " raised no arch over multiplier node " + node.Id + ".");
                    continue;
                }

                if (prop.GetComponent<GateProp>() == null)
                {
                    Debug.LogError(
                        "FAIL: seed " + seed + " node " + node.Id
                        + " is not a gate arch, so a multiplier is not a world object.");
                }

                foreach (var badge in prop.GetComponentsInChildren<NumberBadge>(true))
                {
                    Debug.LogError(
                        "FAIL: seed " + seed + " hangs " + badge.name + " on multiplier node "
                        + node.Id + ", which is meant to carry no badge of any kind.");
                }

                foreach (var plate in prop.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    Debug.LogError(
                        "FAIL: seed " + seed + " hangs the sprite " + plate.name
                        + " on multiplier node " + node.Id + ".");
                }
            }

            if (gates == 0)
            {
                Debug.LogError(
                    "FAIL: seed " + seed + " placed no multiplier at all, so it proves nothing "
                    + "about a gate wearing no badge.");
            }

            if (wearing > 0)
            {
                Debug.LogError(
                    "FAIL: seed " + seed + " built " + wearing + " badges named for a multiplier node.");
            }

            Debug.Log(
                "seed " + seed + ": " + gates + " multiplier gates, each an arch, none of them badged");
        }

        static void ReportBadgeShapes(GameObject root)
        {
            var byStyle = new Dictionary<BadgeStyle, string>();
            var sprites = new Dictionary<BadgeShape, Sprite>();

            foreach (var badge in root.GetComponentsInChildren<NumberBadge>(true))
            {
                var plate = badge.GetComponent<SpriteRenderer>();
                var shape = BadgeStyles.ShapeOf(badge.Style);

                if (plate == null)
                {
                    Debug.LogError("FAIL: badge " + badge.name + " draws no plate behind its number.");
                    continue;
                }

                byStyle[badge.Style] = shape + " in #" + ColorUtility.ToHtmlStringRGB(BadgePalette.Of(badge.Style));

                Sprite cut;
                if (sprites.TryGetValue(shape, out cut))
                {
                    if (cut != plate.sprite)
                    {
                        Debug.LogError(
                            "FAIL: two " + shape + " badges were cut from different sprites.");
                    }
                }
                else
                {
                    sprites[shape] = plate.sprite;
                }
            }

            foreach (var style in byStyle)
            {
                foreach (var other in byStyle)
                {
                    if (!SameFamily(style.Key, other.Key) && style.Value == other.Value)
                    {
                        Debug.LogError(
                            "FAIL: a " + style.Key + " badge and a " + other.Key
                            + " badge are both " + style.Value
                            + ", so only their numbers tell the two meanings apart.");
                    }
                }
            }

            var report = new StringBuilder("badge meanings, one look each:");

            foreach (var style in byStyle)
            {
                report.Append("\n  ").Append(style.Key).Append(": ").Append(style.Value);
            }

            report.Append("\n  Multiplier: no badge at all, a lit arch on the ground");
            Debug.Log(report.ToString());
        }

        static bool SameFamily(BadgeStyle left, BadgeStyle right)
        {
            if (left == right)
            {
                return true;
            }

            return (left == BadgeStyle.Enemy || left == BadgeStyle.Boss)
                && (right == BadgeStyle.Enemy || right == BadgeStyle.Boss);
        }

        static Transform Named(GameObject root, string name)
        {
            foreach (var part in root.GetComponentsInChildren<Transform>(true))
            {
                if (part.name == name)
                {
                    return part;
                }
            }

            return null;
        }

        static void ReportPlayerGrowth(PowerBadge power)
        {
            if (power == null)
            {
                Debug.LogError("FAIL: the level raised no player badge to grow.");
                return;
            }

            var report = new StringBuilder("player badge growth");
            var previous = power.Width;
            var opening = previous;

            Row(report, power);

            foreach (var target in Ladder)
            {
                power.Show(target);

                if (Mathf.Abs(power.Width - previous) > Tolerance)
                {
                    Debug.LogError(
                        "FAIL: the badge snapped from " + previous.ToString("0.####")
                        + " to " + power.Width.ToString("0.####") + " the instant it was told to count to " + target);
                }

                var settledBefore = previous;

                for (var frame = 0; frame < PowerPump.Ceiling && !power.IsSettled; frame++)
                {
                    var counting = !power.HasLanded;
                    power.Advance(Frame);

                    if (counting && power.Width < previous - Tolerance)
                    {
                        Debug.LogError(
                            "FAIL: the badge width jittered from " + previous.ToString("0.####")
                            + " to " + power.Width.ToString("0.####") + " while counting to " + target);
                    }

                    if (power.Width < settledBefore - Tolerance)
                    {
                        Debug.LogError(
                            "FAIL: the badge narrowed below the " + settledBefore.ToString("0.####")
                            + " it held before counting to " + target);
                    }

                    if (power.Width > power.CharacterWidth + Tolerance)
                    {
                        Debug.LogError(
                            "FAIL: mid-count the badge reached " + power.Width.ToString("0.####")
                            + " over a character only " + power.CharacterWidth.ToString("0.####") + " wide");
                    }

                    previous = power.Width;
                }

                if (!power.IsSettled)
                {
                    Debug.LogError("FAIL: the badge never settled counting to " + target);
                }

                Row(report, power);
            }

            if (power.Width <= opening + Tolerance)
            {
                Debug.LogError(
                    "FAIL: four digits left the badge no wider than the one digit it opened on ("
                    + opening.ToString("0.####") + ").");
            }

            Debug.Log(report.ToString());
        }

        static void Row(StringBuilder report, PowerBadge power)
        {
            report.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  showing {0,5} ({1} digits): badge {2:0.###} wide over a {3:0.###} character, {4:0.##}x",
                power.Shown,
                BadgeText.Digits(power.Shown),
                power.Width,
                power.CharacterWidth,
                power.Width / power.CharacterWidth);
        }

        static string Fate(Object asset)
        {
            return asset == null ? "gone" : "still alive";
        }

        static List<Sprite> SpritesOn(GameObject root)
        {
            var sprites = new List<Sprite>();
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sprites.Add(renderer.sprite);
            }

            return sprites;
        }

        static List<Material> MaterialsOn(GameObject root)
        {
            var materials = new List<Material>();
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                materials.Add(renderer.sharedMaterial);
            }

            return materials;
        }

        static int Distinct<T>(List<T> assets) where T : Object
        {
            var seen = new List<T>();
            foreach (var asset in assets)
            {
                if (!seen.Contains(asset))
                {
                    seen.Add(asset);
                }
            }

            return seen.Count;
        }
    }
}
