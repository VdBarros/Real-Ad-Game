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

        static readonly int[] Ladder = { 9, 47, 615, 4200 };

        public static void Check()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var builder = new WorldBuilder();

            var first = builder.Build(LevelGenerator.Generate(FirstSeed, MazePreset.Ship).Graph);
            var firstSprites = SpritesOn(first);
            var firstMaterials = MaterialsOn(first);
            ReportBadgeWidths(FirstSeed, first);
            WorldObjects.Destroy(first);

            var second = builder.Build(LevelGenerator.Generate(SecondSeed, MazePreset.Ship).Graph);
            var secondSprites = SpritesOn(second);
            var secondMaterials = MaterialsOn(second);
            ReportBadgeWidths(SecondSeed, second);
            ReportPlayerGrowth(builder.PlayerBadge);

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
