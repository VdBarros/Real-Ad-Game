using System;
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

        const float LeastChroma = 0.2f;

        const float CloseUpRange = 12f;

        const float CloseUpSize = 1.1f;

        const string ShotPath = "dev/scratch/t-137-";

        static readonly int[] Ladder = { 9, 47, 615, 4200 };

        public static void Check()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PreviewFilm.Sun();

            var builder = new WorldBuilder();

            var firstGraph = LevelGenerator.Generate(FirstSeed, MazePreset.Ship).Graph;
            var first = builder.Build(firstGraph);
            var firstSprites = SpritesOn(first);
            var firstMaterials = MaterialsOn(first);
            ReportBadgeWidths(FirstSeed, first);
            NoMultiplierGateWearsABadge(FirstSeed, firstGraph, first);
            WorldObjects.Destroy(first);

            var secondGraph = LevelGenerator.Generate(SecondSeed, MazePreset.Ship).Graph;
            var second = builder.Build(secondGraph);
            var secondSprites = SpritesOn(second);
            var secondMaterials = MaterialsOn(second);
            ReportBadgeWidths(SecondSeed, second);
            NoMultiplierGateWearsABadge(SecondSeed, secondGraph, second);
            ReportBadgeShapes(second);
            ReportPlayerGrowth(builder.PlayerBadge);
            EveryMarkHoldsItsHueAndSpendsOpacityInstead(SecondSeed, builder.Targets);
            PhotographTheFade(builder.Targets);

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

        static void EveryMarkHoldsItsHueAndSpendsOpacityInstead(long seed, TargetBoard board)
        {
            var report = new StringBuilder("badge marks, seed ")
                .Append(seed.ToString(CultureInfo.InvariantCulture))
                .Append(": hue held, opacity spent");

            foreach (TargetMark mark in Enum.GetValues(typeof(TargetMark)))
            {
                var look = TargetMarks.Look(mark);
                var aimed = TargetMarks.IsAimed(mark);
                var worn = 0;
                var greyed = 0;
                var shifted = 0;
                var misfaded = 0;
                var floating = 0;
                var sample = string.Empty;

                foreach (var target in board.Targets)
                {
                    var badge = target.Badge;

                    if (badge == null)
                    {
                        continue;
                    }

                    target.Wear(mark, badge.Value);
                    worn++;

                    var plain = BadgePalette.Of(badge.Style);
                    var painted = badge.Colour;
                    var label = badge.LabelColour;

                    if (Chroma(painted) <= LeastChroma)
                    {
                        greyed++;
                        sample = badge.name + " in " + Describe(painted);
                    }

                    if (!aimed && !SameHue(painted, plain))
                    {
                        shifted++;
                        sample = badge.name + " painted " + Describe(painted)
                            + " over a plain " + Describe(plain);
                    }

                    if (Math.Abs(painted.a - look.Opacity) > Tolerance)
                    {
                        misfaded++;
                        sample = badge.name + " sits at " + painted.a.ToString("0.###")
                            + " opaque where " + mark + " asks for " + look.Opacity.ToString("0.###");
                    }

                    if (Math.Abs(label.a - painted.a) > Tolerance)
                    {
                        floating++;
                        sample = badge.name + " reads its number at " + label.a.ToString("0.###")
                            + " over a plate at " + painted.a.ToString("0.###");
                    }
                }

                if (worn == 0)
                {
                    Debug.LogError("FAIL: seed " + seed + " raised no badge to wear " + mark + " at all.");
                    continue;
                }

                if (greyed > 0)
                {
                    Debug.LogError(
                        "FAIL: " + greyed + " of " + worn + " badges wearing " + mark
                        + " drained below " + LeastChroma.ToString("0.##", CultureInfo.InvariantCulture)
                        + " chroma, so a colour that should say what the thing is reads as a grey ("
                        + sample + ").");
                }

                if (shifted > 0)
                {
                    Debug.LogError(
                        "FAIL: " + shifted + " of " + worn + " badges wearing " + mark
                        + " were repainted rather than faded, so the hue no longer says what the thing is ("
                        + sample + ").");
                }

                if (misfaded > 0)
                {
                    Debug.LogError(
                        "FAIL: " + misfaded + " of " + worn + " badges wearing " + mark
                        + " hold the wrong opacity (" + sample + ").");
                }

                if (floating > 0)
                {
                    Debug.LogError(
                        "FAIL: " + floating + " of " + worn + " badges wearing " + mark
                        + " float their number at full opacity over a faded plate (" + sample + ").");
                }

                report.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "\n  {0,-12} {1,3} badges at {2:0.##} opaque, washed {3:0.##}, dimmest chroma {4:0.###}",
                    mark,
                    worn,
                    look.Opacity,
                    look.Weight,
                    DimmestChroma(board, mark));
            }

            Rest(board);
            Debug.Log(report.ToString());
        }

        static float DimmestChroma(TargetBoard board, TargetMark mark)
        {
            var dimmest = float.MaxValue;

            foreach (var target in board.Targets)
            {
                var badge = target.Badge;

                if (badge == null || target.Mark != mark)
                {
                    continue;
                }

                var chroma = Chroma(badge.Colour);
                dimmest = chroma < dimmest ? chroma : dimmest;
            }

            return dimmest == float.MaxValue ? 0f : dimmest;
        }

        static void PhotographTheFade(TargetBoard board)
        {
            NodeTarget subject = null;
            NodeTarget arch = null;

            foreach (var target in board.Targets)
            {
                if (subject == null && target.Badge != null && target.Badge.Style != BadgeStyle.Player)
                {
                    subject = target;
                }

                if (arch == null && target.Gate != null)
                {
                    arch = target;
                }
            }

            if (subject == null)
            {
                Debug.LogError("FAIL: no badge stands anywhere the fade could be photographed on.");
            }
            else
            {
                Frames(subject, "badge");
            }

            if (arch == null)
            {
                Debug.LogError("FAIL: no gate arch stands anywhere the fade could be photographed on.");
            }
            else
            {
                Frames(arch, "gate");
            }

            Rest(board);
        }

        static void Frames(NodeTarget subject, string name)
        {
            var eye = PreviewFilm.Rig(subject.transform.position, CloseUpRange, CloseUpSize);
            var report = new StringBuilder(name + " photographed wearing each resting mark:");

            PreviewFilm.Warm(eye);

            foreach (var mark in new[] { TargetMark.Idle, TargetMark.Aside, TargetMark.Unreachable })
            {
                subject.Wear(mark, 0);
                PreviewFilm.Shoot(eye, ShotPath + name + "-" + mark.ToString().ToLowerInvariant() + ".png");

                report.Append("\n  ").Append(mark).Append(": ");
                report.Append(
                    subject.Badge != null
                        ? Describe(subject.Badge.Colour) + ", number at "
                            + subject.Badge.LabelColour.a.ToString("0.##", CultureInfo.InvariantCulture)
                        : Describe(subject.Gate.Colour));
            }

            subject.Wear(TargetMark.Idle, 0);
            WorldObjects.Destroy(eye.gameObject);
            Debug.Log(report.ToString());
        }

        static void Rest(TargetBoard board)
        {
            foreach (var target in board.Targets)
            {
                target.Wear(TargetMark.Idle, target.Badge == null ? 0 : target.Badge.Value);
            }
        }

        static bool SameHue(Color painted, Color plain)
        {
            return Math.Abs(painted.r - plain.r) <= Tolerance
                && Math.Abs(painted.g - plain.g) <= Tolerance
                && Math.Abs(painted.b - plain.b) <= Tolerance;
        }

        static float Chroma(Color colour)
        {
            return BadgeTints.Chroma(new Tint(colour.r, colour.g, colour.b));
        }

        static string Describe(Color colour)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(colour)
                + " at " + colour.a.ToString("0.##", CultureInfo.InvariantCulture) + " opaque";
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

        static string Fate(UnityEngine.Object asset)
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

        static int Distinct<T>(List<T> assets) where T : UnityEngine.Object
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
