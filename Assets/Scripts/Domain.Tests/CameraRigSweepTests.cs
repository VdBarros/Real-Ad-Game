using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class CameraRigSweepTests
    {
        const int Seeds = 56;

        const float Frame = 1f / 60f;

        const float PartExtent = 1.5f;

        const int FrameCap = 1200;

        const float Horizon = 1000f;

        static List<LevelGraph> sweep;

        static IReadOnlyList<LevelGraph> Sweep()
        {
            if (sweep != null)
            {
                return sweep;
            }

            sweep = new List<LevelGraph>(Seeds);
            for (var seed = 1; seed <= Seeds; seed++)
            {
                sweep.Add(LevelGenerator.Generate(seed, MazePreset.Ship).Graph);
            }

            return sweep;
        }

        [Test]
        public void ThePeakOnScreenPanStaysUnderOneScreenWidthPerSecond()
        {
            var openings = new List<float>(Seeds);
            var follows = new List<float>(Seeds);
            var returns = new List<float>(Seeds);
            var worst = 0f;
            long worstSeed = 0;
            var worstLeg = "opening";

            foreach (var graph in Sweep())
            {
                var staging = CameraStaging.Over(graph);
                var previous = staging.Framing;
                var peak = 0f;

                while (staging.IsBusy || !staging.IsSettled)
                {
                    staging = staging.Advanced(Frame);
                    peak = Math.Max(peak, ScreenFrame.PanPixels(previous, staging.Framing) / Frame);
                    previous = staging.Framing;
                }

                openings.Add(peak);
                if (peak > worst)
                {
                    worst = peak;
                    worstSeed = graph.Seed;
                    worstLeg = "opening";
                }

                var chased = 0f;
                foreach (var node in graph.Decisions.Nodes)
                {
                    staging = staging.Follows(IsoProjection.Of(node.Position));

                    for (var frame = 0; frame < FrameCap && !staging.IsSettled; frame++)
                    {
                        staging = staging.Advanced(Frame);
                        chased = Math.Max(chased, ScreenFrame.PanPixels(previous, staging.Framing) / Frame);
                        previous = staging.Framing;
                    }
                }

                follows.Add(chased);
                if (chased > worst)
                {
                    worst = chased;
                    worstSeed = graph.Seed;
                    worstLeg = "follow";
                }

                var came = 0f;
                foreach (var pull in Compass())
                {
                    staging = staging.Looks(pull).LooksBack();
                    previous = staging.Framing;

                    for (var frame = 0; frame < FrameCap && !staging.IsSettled; frame++)
                    {
                        staging = staging.Advanced(Frame);
                        came = Math.Max(came, ScreenFrame.PanPixels(previous, staging.Framing) / Frame);
                        previous = staging.Framing;
                    }

                    Assert.That(
                        staging.Framing,
                        Is.EqualTo(LevelFraming.Play(staging.Subject)),
                        "Seed " + graph.Seed + " let go of a drag and never came back to the player.");
                }

                returns.Add(came);
                if (came > worst)
                {
                    worst = came;
                    worstSeed = graph.Seed;
                    worstLeg = "return";
                }
            }

            openings.Sort();
            follows.Sort();
            returns.Sort();

            Console.WriteLine(
                "ship, " + Seeds + " seeds, opening over " + CameraFlight.Duration
                + " s (" + CameraFlight.Seconds + " reveal, " + CameraFlight.HoldSeconds
                + " hold), a follow settling onto the player, and a drag to each of eight horizons let go of");
            Console.WriteLine("  opening peak pan median  " + openings[openings.Count / 2].ToString("0") + " px/s");
            Console.WriteLine("  follow peak pan median   " + follows[follows.Count / 2].ToString("0") + " px/s");
            Console.WriteLine("  return peak pan median   " + returns[returns.Count / 2].ToString("0") + " px/s");
            Console.WriteLine(
                "  worst anywhere           " + worst.ToString("0") + " px/s on the " + worstLeg
                + " of seed " + worstSeed);
            Console.WriteLine("  ceiling                  " + ScreenFrame.PanCeiling.ToString("0") + " px/s");

            Assert.That(
                worst,
                Is.LessThan(ScreenFrame.PanCeiling),
                "Seed " + worstSeed + " pans at " + worst + " px/s on its " + worstLeg + ", over the "
                + ScreenFrame.PanCeiling + " px/s ceiling for a " + ScreenFrame.Width + "-wide portrait frame.");
        }

        [Test]
        public void NoCutClipsGeometryAtEitherClippingPlane()
        {
            var nearest = float.MaxValue;
            var furthest = float.MinValue;

            foreach (var graph in Sweep())
            {
                var blueprint = LevelBlueprintBuilder.Build(graph);
                var framings = new List<CameraFraming> { LevelFraming.Whole(graph), LevelFraming.Opening(graph) };

                foreach (var node in graph.Decisions.Nodes)
                {
                    framings.Add(LevelFraming.CloseUp(node.Position));
                    framings.Add(LevelFraming.Play(IsoProjection.Of(node.Position)));
                }

                foreach (var framing in framings)
                {
                    foreach (var part in blueprint.AllParts)
                    {
                        var depth = framing.DepthOf(part.Position);
                        if (depth < nearest)
                        {
                            nearest = depth;
                        }

                        if (depth > furthest)
                        {
                            furthest = depth;
                        }
                    }
                }
            }

            Console.WriteLine("  nearest geometry " + nearest.ToString("0.00") + " m ahead of the camera");
            Console.WriteLine("  furthest         " + furthest.ToString("0.00") + " m");
            Console.WriteLine("  clip planes      "
                + IsoProjection.NearPlane + " / " + IsoProjection.FarPlane
                + " at a back-offset of " + IsoProjection.CameraBack + " m");

            Assert.That(
                nearest - PartExtent,
                Is.GreaterThan(IsoProjection.NearPlane),
                "Geometry crosses the near plane at " + nearest + " m ahead of the camera.");
            Assert.That(
                furthest + PartExtent,
                Is.LessThan(IsoProjection.FarPlane),
                "Geometry falls past the far plane at " + furthest + " m ahead of the camera.");
        }

        [Test]
        public void TheSameSeedProducesTheSameFlight()
        {
            for (var seed = 1; seed <= 8; seed++)
            {
                var first = CameraFlight.Over(LevelGenerator.Generate(seed, MazePreset.Ship).Graph);
                var second = CameraFlight.Over(LevelGenerator.Generate(seed, MazePreset.Ship).Graph);

                while (!first.IsSettled)
                {
                    Assert.That(second.Framing, Is.EqualTo(first.Framing), "Seed " + seed + " flew differently.");
                    first = first.Advanced(Frame);
                    second = second.Advanced(Frame);
                }

                Assert.That(second.Framing, Is.EqualTo(first.Framing));
            }
        }

        [Test]
        public void TheRevealFitsTheLevelItRevealsAndReadsTheSameEveryTime()
        {
            var widest = 0f;
            var tightest = float.MaxValue;

            foreach (var graph in Sweep())
            {
                var whole = LevelFraming.Whole(graph);

                Assert.That(
                    whole,
                    Is.EqualTo(LevelFraming.Whole(graph)),
                    "Seed " + graph.Seed + " reveals itself differently on a second reading.");

                widest = Math.Max(widest, whole.OrthographicSize);
                tightest = Math.Min(tightest, whole.OrthographicSize);
            }

            Console.WriteLine(
                "  reveal size      " + tightest.ToString("0.##") + " to " + widest.ToString("0.##")
                + " over " + Seeds + " seeds, a tile "
                + (ScreenFrame.PixelsPerMetre(widest) * IsoProjection.TileEdge).ToString("0")
                + " px across at its widest against "
                + (ScreenFrame.PixelsPerMetre(LevelFraming.PlaySize) * IsoProjection.TileEdge).ToString("0")
                + " px in play");
        }

        [Test]
        public void TheWholeLevelIsOnScreenOnlyAtTheReveal()
        {
            var offAtPlay = 0;

            foreach (var graph in Sweep())
            {
                Assert.That(Spills(graph, LevelFraming.Whole(graph)), Is.False,
                    "Seed " + graph.Seed + " does not fit the frame the opening reveals it on.");

                if (Spills(graph, LevelFraming.Play(LevelFraming.Centre(graph))))
                {
                    offAtPlay++;
                }
            }

            Assert.That(
                offAtPlay,
                Is.EqualTo(Seeds),
                "A level fitted the play frame whole, so there was nothing left to discover.");

            Console.WriteLine(
                "  " + offAtPlay + " of " + Seeds
                + " seeds run off a play-sized frame, which is why the camera follows rather than holds");
        }

        [Test]
        public void ThePlayFramingShowsAWindowOnTheLevelAndNeverTheWholeOfIt()
        {
            var tightest = 1f;
            var widest = 0f;
            var nodesSeen = 0;
            var nodesAll = 0;

            foreach (var graph in Sweep())
            {
                var play = LevelFraming.Play(LevelFraming.StartPoint(graph));
                var showing = 0;

                foreach (var tile in graph.Tiles.Tiles)
                {
                    if (Inside(play, IsoProjection.Of(tile.Position)))
                    {
                        showing++;
                    }
                }

                foreach (var node in graph.Decisions.Nodes)
                {
                    nodesAll++;
                    if (Inside(play, IsoProjection.Of(node.Position)))
                    {
                        nodesSeen++;
                    }
                }

                var share = showing / (float)graph.Tiles.Tiles.Count;

                tightest = Math.Min(tightest, share);
                widest = Math.Max(widest, share);

                Assert.That(
                    showing,
                    Is.GreaterThan(0),
                    "Seed " + graph.Seed + " opens play with the player on no tile the camera can see.");
                Assert.That(
                    share,
                    Is.LessThan(0.55f),
                    "Seed " + graph.Seed + " puts " + showing + " of its " + graph.Tiles.Tiles.Count
                    + " tiles on one frame, which leaves nothing to discover.");
            }

            Console.WriteLine(
                "  play framing at size " + LevelFraming.PlaySize.ToString("0.###")
                + " puts the figure on " + (LevelFraming.FigureHeightFraction * 100f).ToString("0.#")
                + "% of screen height and shows " + (tightest * 100f).ToString("0")
                + "% to " + (widest * 100f).ToString("0") + "% of a level's tiles, "
                + (nodesSeen / (float)nodesAll * 100f).ToString("0") + "% of its rooms");
        }

        [Test]
        public void NoDragCanLoseTheLevelOffScreenOnAnySeed()
        {
            foreach (var graph in Sweep())
            {
                var staging = CameraStaging.Over(graph).Skipped();

                for (var frame = 0; frame < FrameCap && !staging.IsSettled; frame++)
                {
                    staging = staging.Advanced(Frame);
                }

                foreach (var pull in Compass())
                {
                    var looking = staging.Looks(pull);
                    var shows = false;

                    foreach (var tile in graph.Tiles.Tiles)
                    {
                        shows |= Inside(looking.Framing, IsoProjection.Of(tile.Position));
                    }

                    Assert.That(
                        shows,
                        Is.True,
                        "Seed " + graph.Seed + " dragged to " + pull + " left no tile on screen at "
                        + looking.Framing + ".");
                }
            }
        }

        static bool Inside(CameraFraming framing, WorldPoint point)
        {
            var acrossHalf = framing.OrthographicSize * ScreenFrame.Width / ScreenFrame.Height;
            var apart = new WorldPoint(
                point.X - framing.Target.X, point.Y - framing.Target.Y, point.Z - framing.Target.Z);

            return Math.Abs(WorldPoint.Dot(apart, IsoProjection.CameraRight)) <= acrossHalf
                && Math.Abs(WorldPoint.Dot(apart, IsoProjection.CameraUp)) <= framing.OrthographicSize;
        }

        static IEnumerable<WorldPoint> Compass()
        {
            var right = IsoProjection.CameraRight;
            var up = IsoProjection.CameraUp;

            for (var step = 0; step < 8; step++)
            {
                var angle = step * Math.PI * 0.25;
                var across = (float)Math.Cos(angle) * Horizon;
                var along = (float)Math.Sin(angle) * Horizon;

                yield return new WorldPoint(
                    right.X * across + up.X * along,
                    right.Y * across + up.Y * along,
                    right.Z * across + up.Z * along);
            }
        }

        static bool Spills(LevelGraph graph, CameraFraming framing)
        {
            var acrossHalf = framing.OrthographicSize * ScreenFrame.Width / ScreenFrame.Height;

            foreach (var tile in graph.Tiles.Tiles)
            {
                var point = IsoProjection.Of(tile.Position);
                var apart = new WorldPoint(
                    point.X - framing.Target.X, point.Y - framing.Target.Y, point.Z - framing.Target.Z);

                if (Math.Abs(WorldPoint.Dot(apart, IsoProjection.CameraRight)) > acrossHalf
                    || Math.Abs(WorldPoint.Dot(apart, IsoProjection.CameraUp)) > framing.OrthographicSize)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
