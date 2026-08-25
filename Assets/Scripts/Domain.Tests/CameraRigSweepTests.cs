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
            var peaks = new List<float>(Seeds);
            var worst = 0f;
            long worstSeed = 0;

            foreach (var graph in Sweep())
            {
                var flight = CameraFlight.Over(graph);
                var previous = flight.Framing;
                var peak = 0f;

                while (!flight.IsSettled)
                {
                    flight = flight.Advanced(Frame);
                    var speed = ScreenFrame.PanPixels(previous, flight.Framing) / Frame;
                    if (speed > peak)
                    {
                        peak = speed;
                    }

                    previous = flight.Framing;
                }

                peaks.Add(peak);
                if (peak > worst)
                {
                    worst = peak;
                    worstSeed = graph.Seed;
                }
            }

            peaks.Sort();

            Console.WriteLine("ship, " + Seeds + " seeds, flight over " + CameraFlight.Seconds + " s");
            Console.WriteLine("  peak pan median  " + peaks[peaks.Count / 2].ToString("0") + " px/s");
            Console.WriteLine("  peak pan worst   " + worst.ToString("0") + " px/s on seed " + worstSeed);
            Console.WriteLine("  ceiling          " + ScreenFrame.PanCeiling.ToString("0") + " px/s");

            Assert.That(
                worst,
                Is.LessThan(ScreenFrame.PanCeiling),
                "Seed " + worstSeed + " pans at " + worst + " px/s, over the "
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
                var framings = new List<CameraFraming> { LevelFraming.Play(graph), LevelFraming.Opening(graph) };

                foreach (var node in graph.Decisions.Nodes)
                {
                    framings.Add(LevelFraming.CloseUp(node.Position));
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
        public void TheConstantIsAPerPresetConstantRatherThanAPerSeedFit()
        {
            CameraFraming? constant = null;

            foreach (var graph in Sweep())
            {
                var play = LevelFraming.Play(graph);
                if (constant == null)
                {
                    constant = play;
                    continue;
                }

                Assert.That(play, Is.EqualTo(constant.Value), "Seed " + graph.Seed + " reframed the level.");
            }
        }
    }
}
