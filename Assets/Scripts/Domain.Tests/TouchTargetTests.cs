using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class TouchTargetTests
    {
        const int Seeds = 60;

        const float Tolerance = 0.01f;

        [Test]
        public void NineMillimetresIsTheSameNineMillimetresGoingBackTheOtherWay()
        {
            var dpi = TouchTargets.ReferenceDotsPerInch;
            var pixels = TouchTargets.Pixels(TouchTargets.MinimumMillimetres, dpi);

            Assert.That(
                TouchTargets.Millimetres(pixels, dpi),
                Is.EqualTo(TouchTargets.MinimumMillimetres).Within(Tolerance));
        }

        [Test]
        public void TheReachIsHalfTheSmallestTargetBecauseATapLandsInTheMiddleOfOne()
        {
            Assert.That(
                TouchTargets.Millimetres(TouchTargets.Reach * 2f, TouchTargets.ReferenceDotsPerInch),
                Is.EqualTo(TouchTargets.MinimumMillimetres).Within(Tolerance));
        }

        [Test]
        public void TheReferenceDeviceIsTheFrameTheGameIsAuthoredFor()
        {
            var diagonal = Math.Sqrt(
                (double)ScreenFrame.Width * ScreenFrame.Width
                + (double)ScreenFrame.Height * ScreenFrame.Height);

            Assert.That(
                TouchTargets.ReferenceDotsPerInch,
                Is.EqualTo(diagonal / TouchTargets.ReferenceDiagonalInches).Within(Tolerance));
        }

        [Test]
        public void ADeviceThatDoesNotKnowItsOwnDensityFallsBackToTheReferenceOne()
        {
            Assert.That(TouchTargets.DotsPerInchOr(0f), Is.EqualTo(TouchTargets.ReferenceDotsPerInch));
            Assert.That(TouchTargets.DotsPerInchOr(float.NaN), Is.EqualTo(TouchTargets.ReferenceDotsPerInch));
            Assert.That(TouchTargets.DotsPerInchOr(320f), Is.EqualTo(320f));
        }

        [Test]
        public void EveryNodeWithRoomForANineMillimetreTargetGetsOne()
        {
            var reach = TouchTargets.Reach;
            var full = 0;
            var crowded = 0;
            var narrowest = float.MaxValue;
            var narrowestWhere = string.Empty;
            var byPreset = new List<string>();

            foreach (var preset in new[] { MazePreset.Tiny, MazePreset.Ship, MazePreset.Stress })
            {
                var presetNarrowest = float.MaxValue;
                var presetFull = 0;
                var presetCrowded = 0;

                for (var seed = 1L; seed <= Seeds; seed++)
                {
                    var graph = LevelGenerator.Generate(seed, preset).Graph;
                    var points = Drawn(graph);

                    for (var index = 0; index < points.Count; index++)
                    {
                        var separation = NearestOther(points, index);
                        var target = TouchTargets.Millimetres(
                            2f * Math.Min(reach, separation * 0.5f), TouchTargets.ReferenceDotsPerInch);

                        if (separation * 0.5f >= reach)
                        {
                            Assert.That(
                                target,
                                Is.EqualTo(TouchTargets.MinimumMillimetres).Within(Tolerance),
                                "Node " + index + " of " + preset + " seed " + seed
                                + " has room for a full target and did not get one.");
                            presetFull++;
                        }
                        else
                        {
                            presetCrowded++;
                        }

                        if (target < presetNarrowest)
                        {
                            presetNarrowest = target;
                        }

                        if (target < narrowest)
                        {
                            narrowest = target;
                            narrowestWhere = preset + " seed " + seed + " node " + index;
                        }
                    }
                }

                full += presetFull;
                crowded += presetCrowded;
                byPreset.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: {1} of {2} nodes get the full {3:0.#} mm, narrowest {4:0.##} mm",
                    preset,
                    presetFull,
                    presetFull + presetCrowded,
                    TouchTargets.MinimumMillimetres,
                    presetNarrowest));
            }

            Console.WriteLine(
                "touch targets over " + Seeds + " seeds a preset, at the play framing on a "
                + TouchTargets.ReferenceDotsPerInch.ToString("0", CultureInfo.InvariantCulture)
                + " dpi reference device:\n  " + string.Join("\n  ", byPreset.ToArray())
                + "\n  narrowest overall " + narrowest.ToString("0.##", CultureInfo.InvariantCulture)
                + " mm at " + narrowestWhere
                + "\n  " + full + " full, " + crowded + " crowded by a nearer node");

            Assert.That(full, Is.GreaterThan(0), "No node anywhere had room for a full target.");
        }

        [Test]
        public void TheFramingAndNotTheAimIsWhatHoldsATargetUnderNineMillimetres()
        {
            const float Close = 4.5f;

            for (var seed = 1L; seed <= Seeds; seed++)
            {
                var graph = LevelGenerator.Generate(seed, MazePreset.Tiny).Graph;
                var points = Drawn(graph, new CameraFraming(LevelFraming.Centre(graph), Close));

                for (var index = 0; index < points.Count; index++)
                {
                    Assert.That(
                        TouchTargets.Millimetres(NearestOther(points, index), TouchTargets.ReferenceDotsPerInch),
                        Is.GreaterThanOrEqualTo(TouchTargets.MinimumMillimetres),
                        "Node " + index + " of tiny seed " + seed + " is crowded even at a size " + Close
                        + " framing, so no amount of zoom would give it a full target.");
                }
            }
        }

        [Test]
        public void AFingerAnywhereInsideANodeTargetAimsAtThatNode()
        {
            const int Bearings = 24;
            var reach = TouchTargets.Reach;

            foreach (var preset in new[] { MazePreset.Tiny, MazePreset.Ship, MazePreset.Stress })
            {
                for (var seed = 1L; seed <= 10L; seed++)
                {
                    var graph = LevelGenerator.Generate(seed, preset).Graph;
                    var state = RunState.Begin(graph, PowerTuning.For(preset).StartingPower);
                    var candidates = Candidates(state);

                    for (var index = 0; index < candidates.Count; index++)
                    {
                        var candidate = candidates[index];
                        var radius = Math.Min(reach, NearestOtherCandidate(candidates, index) * 0.5f) - Tolerance;

                        for (var bearing = 0; bearing < Bearings; bearing++)
                        {
                            var angle = bearing * 2.0 * Math.PI / Bearings;
                            var finger = new ScreenPoint(
                                candidate.Point.X + (float)(Math.Cos(angle) * radius),
                                candidate.Point.Y + (float)(Math.Sin(angle) * radius));

                            Assert.That(
                                TapAim.Of(candidates, finger, reach),
                                Is.EqualTo(candidate.NodeId),
                                "A finger inside the target of node " + candidate.NodeId + " of " + preset
                                + " seed " + seed + " aimed elsewhere.");
                        }
                    }
                }
            }
        }

        static IReadOnlyList<TapCandidate> Candidates(RunState state)
        {
            return TapAim.Candidates(
                state, LevelFraming.Play(state.Level), ScreenFrame.Width, ScreenFrame.Height);
        }

        static float NearestOtherCandidate(IReadOnlyList<TapCandidate> candidates, int index)
        {
            var points = new List<ScreenPoint>(candidates.Count);
            foreach (var candidate in candidates)
            {
                points.Add(candidate.Point);
            }

            return NearestOther(points, index);
        }

        static List<ScreenPoint> Drawn(LevelGraph graph)
        {
            return Drawn(graph, LevelFraming.Play(graph));
        }

        static List<ScreenPoint> Drawn(LevelGraph graph, CameraFraming framing)
        {
            var points = new List<ScreenPoint>();

            foreach (var node in graph.Decisions.Nodes)
            {
                WorldPart prop;
                if (!LevelBlueprintBuilder.TryProp(node, out prop))
                {
                    continue;
                }

                points.Add(ScreenProjection.Of(
                    framing, TapAim.AnchorOf(node), ScreenFrame.Width, ScreenFrame.Height));
            }

            return points;
        }

        static float NearestOther(IReadOnlyList<ScreenPoint> points, int index)
        {
            var nearest = float.MaxValue;

            for (var other = 0; other < points.Count; other++)
            {
                if (other == index)
                {
                    continue;
                }

                var distance = ScreenPoint.Distance(points[index], points[other]);
                if (distance < nearest)
                {
                    nearest = distance;
                }
            }

            return nearest;
        }
    }
}
