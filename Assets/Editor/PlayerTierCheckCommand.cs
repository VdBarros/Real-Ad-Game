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
    public static class PlayerTierCheckCommand
    {
        const long Seed = 20250824L;

        static readonly int[] Climb = { 9, 40, 140, 420 };

        public static void Check()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var graph = LevelGenerator.Generate(Seed, MazePreset.Ship).Graph;
            var builder = new WorldBuilder();
            var root = builder.Build(graph);
            var power = builder.PlayerBadge;
            var player = root.GetComponentInChildren<PlayerFigure>(true);
            var enemies = root.GetComponentsInChildren<EnemyFigure>(true);
            var site = DeathSite(graph);

            var report = new StringBuilder("tiers on ship seed ")
                .Append(Seed.ToString(CultureInfo.InvariantCulture))
                .Append(", ")
                .Append(enemies.Length.ToString(CultureInfo.InvariantCulture))
                .Append(" enemies:")
                .Append(Row(power, player, enemies));

            foreach (var target in Climb)
            {
                power.DropWeaponFrom(site);
                report.Append(Walk(power, player, target)).Append(Row(power, player, enemies));
            }

            Debug.Log(report.ToString());

            WorldObjects.Destroy(root);
            builder.Dispose();
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

        static string Row(PowerBadge power, PlayerFigure player, IReadOnlyList<EnemyFigure> enemies)
        {
            var counts = new int[4];
            foreach (var enemy in enemies)
            {
                counts[(int)enemy.Band]++;
            }

            var row = new StringBuilder();
            row.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  power {0} is tier {1} at scale {2:0.###} carrying {3} ->",
                power.Power,
                power.Look.Tier,
                player.transform.localScale.x,
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
    }
}
