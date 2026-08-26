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
        public void NoNodeIsEverCrowdedByANodeOnAnotherTerrace()
        {
            var reach = TouchTargets.Reach;
            var full = 0;
            var crowded = 0;
            var tightestAcross = float.MaxValue;
            var tightestAcrossWhere = string.Empty;
            var byPreset = new List<string>();

            foreach (var preset in new[] { MazePreset.Tiny, MazePreset.Ship, MazePreset.Stress })
            {
                var presetFull = 0;
                var presetCrowded = 0;
                var presetTightestAcross = float.MaxValue;

                for (var seed = 1L; seed <= Seeds; seed++)
                {
                    var graph = LevelGenerator.Generate(seed, preset).Graph;
                    var drawn = Drawn(graph);

                    for (var index = 0; index < drawn.Count; index++)
                    {
                        if (NearestOther(drawn, index) * 0.5f >= reach)
                        {
                            presetFull++;
                        }
                        else
                        {
                            presetCrowded++;
                        }

                        var across = NearestOnAnotherTerrace(drawn, index);
                        if (across == float.MaxValue)
                        {
                            continue;
                        }

                        Assert.That(
                            across * 0.5f,
                            Is.GreaterThanOrEqualTo(reach),
                            "Node " + index + " of " + preset + " seed " + seed + " at " + drawn[index].Where
                            + " is crowded by a node on another terrace " + across.ToString("0.#", CultureInfo.InvariantCulture)
                            + " px away, so a finger meant for one of them can land on the other.");

                        if (across < presetTightestAcross)
                        {
                            presetTightestAcross = across;
                        }

                        if (across < tightestAcross)
                        {
                            tightestAcross = across;
                            tightestAcrossWhere = preset + " seed " + seed + " node " + index;
                        }
                    }
                }

                full += presetFull;
                crowded += presetCrowded;
                byPreset.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: {1} of {2} nodes have a full {3:0.#} mm target, tightest pair across terraces {4}",
                    preset,
                    presetFull,
                    presetFull + presetCrowded,
                    TouchTargets.MinimumMillimetres,
                    Pixels(presetTightestAcross)));
            }

            Console.WriteLine(
                "touch targets over " + Seeds + " seeds a preset, at the play framing on a "
                + TouchTargets.ReferenceDotsPerInch.ToString("0", CultureInfo.InvariantCulture)
                + " dpi reference device, reach " + reach.ToString("0.#", CultureInfo.InvariantCulture)
                + " px:\n  " + string.Join("\n  ", byPreset.ToArray())
                + "\n  " + full + " full, " + crowded + " crowded by a nearer node on their own terrace"
                + "\n  tightest pair across terraces " + tightestAcross.ToString("0.#", CultureInfo.InvariantCulture)
                + " px at " + tightestAcrossWhere);

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
            var nearest = float.MaxValue;

            for (var other = 0; other < candidates.Count; other++)
            {
                if (other == index)
                {
                    continue;
                }

                var distance = ScreenPoint.Distance(candidates[index].Point, candidates[other].Point);
                if (distance < nearest)
                {
                    nearest = distance;
                }
            }

            return nearest;
        }

        static string Pixels(float separation)
        {
            return separation == float.MaxValue
                ? "none, the preset has one terrace"
                : separation.ToString("0.#", CultureInfo.InvariantCulture) + " px";
        }

        static List<DrawnNode> Drawn(LevelGraph graph)
        {
            return Drawn(graph, LevelFraming.Play(graph));
        }

        static List<DrawnNode> Drawn(LevelGraph graph, CameraFraming framing)
        {
            var drawn = new List<DrawnNode>();

            foreach (var node in graph.Decisions.Nodes)
            {
                WorldPart prop;
                if (!LevelBlueprintBuilder.TryProp(node, out prop))
                {
                    continue;
                }

                drawn.Add(new DrawnNode(
                    ScreenProjection.Of(framing, TapAim.AnchorOf(node), ScreenFrame.Width, ScreenFrame.Height),
                    node.Position));
            }

            return drawn;
        }

        static float NearestOther(IReadOnlyList<DrawnNode> drawn, int index)
        {
            return Nearest(drawn, index, acrossTerracesOnly: false);
        }

        static float NearestOnAnotherTerrace(IReadOnlyList<DrawnNode> drawn, int index)
        {
            return Nearest(drawn, index, acrossTerracesOnly: true);
        }

        static float Nearest(IReadOnlyList<DrawnNode> drawn, int index, bool acrossTerracesOnly)
        {
            var nearest = float.MaxValue;

            for (var other = 0; other < drawn.Count; other++)
            {
                if (other == index)
                {
                    continue;
                }

                if (acrossTerracesOnly && drawn[other].Elevation == drawn[index].Elevation)
                {
                    continue;
                }

                var distance = ScreenPoint.Distance(drawn[index].Point, drawn[other].Point);
                if (distance < nearest)
                {
                    nearest = distance;
                }
            }

            return nearest;
        }

        readonly struct DrawnNode
        {
            public DrawnNode(ScreenPoint point, TilePosition position)
            {
                Point = point;
                Position = position;
            }

            public ScreenPoint Point { get; }

            public TilePosition Position { get; }

            public int Elevation
            {
                get { return Position.Elevation; }
            }

            public string Where
            {
                get { return Position.ToString(); }
            }
        }
    }
}
