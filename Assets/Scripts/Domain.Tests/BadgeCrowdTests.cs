using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class BadgeCrowdTests
    {
        const float Tolerance = 1e-4f;

        const float Roomy = 4f;

        const int StartingPower = 2;

        const long FirstSeed = 20250824L;

        const int Sweep = 25;

        const int Grown = 4200;

        const float Dimmest = 0.4f;

        const float Readable = 0.2f;

        [Test]
        public void BadgesThatNeverTouchKeepTheirPlaces()
        {
            var spots = new[]
            {
                Spot(0, 0f, 0f, 0f, 0.5f),
                Spot(1, 2f, 0f, 1f, 0.5f),
                Spot(2, -2f, 1.5f, 2f, 0.5f)
            };

            var stack = BadgeCrowd.Resolve(spots);

            Assert.That(stack.Stacked, Is.Zero);
            Assert.That(stack.Faded, Is.Zero);

            foreach (var seat in stack.Seats)
            {
                Assert.That(seat.Lift, Is.EqualTo(0f).Within(Tolerance), seat.ToString());
                Assert.That(seat.Opacity, Is.EqualTo(1f).Within(Tolerance), seat.ToString());
            }
        }

        [Test]
        public void TwoBadgesOnTopOfOneAnotherComeApartOnScreen()
        {
            var spots = new[]
            {
                Spot(0, 0f, 0f, 0f, 0.6f),
                Spot(1, 0.05f, 0.04f, 1f, 0.6f)
            };

            Assert.That(Overlapping(Play(), spots[0], spots[1]), Is.True, "the pair has to collide to prove anything");

            var seated = BadgeCrowd.Seated(spots, BadgeCrowd.Resolve(spots));

            Assert.That(Overlapping(Play(), seated[0], seated[1]), Is.False);
        }

        [Test]
        public void TheNearerBadgeHoldsItsGroundAndTheFartherOneRises()
        {
            var spots = new[]
            {
                Spot(7, 0f, 0f, 3f, 0.6f),
                Spot(3, 0f, 0f, 0f, 0.6f)
            };

            var stack = BadgeCrowd.Resolve(spots);

            Assert.That(stack.Of(3).Lift, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(stack.Of(7).Lift, Is.GreaterThan(0f));
            Assert.That(stack.Of(7).Order, Is.LessThan(stack.Of(3).Order));
        }

        [Test]
        public void ALiftMovesABadgeStraightUpTheScreenAndNowhereElse()
        {
            var spot = Spot(0, 0.3f, -0.2f, 1.7f, 0.6f);
            var lifted = spot.Lifted(0.75f);

            Assert.That(lifted.Across, Is.EqualTo(spot.Across).Within(Tolerance));
            Assert.That(lifted.Depth, Is.EqualTo(spot.Depth).Within(Tolerance));
            Assert.That(lifted.Up, Is.EqualTo(spot.Up + 0.75f).Within(Tolerance));
        }

        [Test]
        public void SeatingACrowdAndResolvingItAgainMovesNothing()
        {
            var spots = Huddle(4, 0.3f, 0.2f);
            var stack = BadgeCrowd.Resolve(spots);

            foreach (var seat in stack.Seats)
            {
                Assert.That(seat.Opacity, Is.EqualTo(1f).Within(Tolerance), "the huddle must fit under the ceiling");
            }

            var again = BadgeCrowd.Resolve(BadgeCrowd.Seated(spots, stack));

            foreach (var seat in again.Seats)
            {
                Assert.That(seat.Lift, Is.EqualTo(0f).Within(Tolerance), seat.ToString());
                Assert.That(seat.Opacity, Is.EqualTo(1f).Within(Tolerance), seat.ToString());
                Assert.That(seat.Order, Is.EqualTo(stack.Of(seat.NodeId).Order), seat.ToString());
            }
        }

        [Test]
        public void ResolvingTheSameCrowdTwiceSeatsItTheSameWay()
        {
            var spots = Huddle(8, 0.2f, 0.12f);
            var first = BadgeCrowd.Resolve(spots);
            var second = BadgeCrowd.Resolve(spots);

            for (var slot = 0; slot < first.Seats.Count; slot++)
            {
                Assert.That(second.Seats[slot], Is.EqualTo(first.Seats[slot]));
            }
        }

        [Test]
        public void TheOrderABadgeIsDrawnInDependsOnNothingButWhereItStands()
        {
            var spots = new List<BadgeSpot>(Huddle(7, 0.18f, 0.11f));
            var straight = BadgeCrowd.Resolve(spots);

            var shuffled = new List<BadgeSpot>(spots);
            shuffled.Reverse();
            var reversed = BadgeCrowd.Resolve(shuffled);

            foreach (var seat in straight.Seats)
            {
                Assert.That(reversed.Of(seat.NodeId).Order, Is.EqualTo(seat.Order), seat.ToString());
                Assert.That(reversed.Of(seat.NodeId).Lift, Is.EqualTo(seat.Lift).Within(Tolerance), seat.ToString());
            }
        }

        [Test]
        public void DrawOrderRunsFromTheFarthestBadgeBackToTheNearestInFront()
        {
            var spots = Huddle(9, 0.15f, 0.09f);
            var stack = BadgeCrowd.Resolve(spots);
            var taken = new List<int>();

            foreach (var seat in stack.Seats)
            {
                Assert.That(taken, Has.No.Member(seat.Order));
                taken.Add(seat.Order);
                Assert.That(seat.Order, Is.InRange(0, spots.Count - 1));
            }

            for (var slot = 0; slot < spots.Count; slot++)
            {
                for (var other = 0; other < spots.Count; other++)
                {
                    if (spots[slot].Depth >= spots[other].Depth)
                    {
                        continue;
                    }

                    Assert.That(
                        stack.Of(spots[slot].NodeId).Order,
                        Is.GreaterThan(stack.Of(spots[other].NodeId).Order),
                        spots[slot].ToString());
                }
            }
        }

        [Test]
        public void WideningOneBadgeNeverBringsAnotherBackDown()
        {
            var lower = 0f;

            for (var step = 0; step <= 120; step++)
            {
                var width = 0.3f + step * 0.01f;
                var spots = Terrace(5, width);
                var stack = BadgeCrowd.Resolve(spots);
                var total = 0f;

                foreach (var seat in stack.Seats)
                {
                    total += seat.Lift;
                }

                Assert.That(total, Is.GreaterThanOrEqualTo(lower - Tolerance), width.ToString("0.###"));
                lower = total;
            }
        }

        [Test]
        public void CountingUpThroughFourDigitsNeverReordersTheStackNorLetsAPairCollide()
        {
            var narrow = BadgeFit.Of(1f, Roomy).Width;
            var wide = BadgeFit.Of(4f, Roomy).Width;
            var opening = BadgeCrowd.Resolve(Counting(narrow));
            var risen = new Dictionary<int, float>();
            var jump = 0f;

            foreach (var seat in opening.Seats)
            {
                risen.Add(seat.NodeId, seat.Lift);
            }

            for (var step = 0; step <= 400; step++)
            {
                var width = narrow + (wide - narrow) * step / 400f;
                var spots = Counting(width);
                var stack = BadgeCrowd.Resolve(spots);
                var seated = BadgeCrowd.Seated(spots, stack);

                foreach (var seat in stack.Seats)
                {
                    Assert.That(
                        seat.Order,
                        Is.EqualTo(opening.Of(seat.NodeId).Order),
                        "node " + seat.NodeId + " changed places at width " + width.ToString("0.####"));

                    Assert.That(
                        seat.Lift,
                        Is.GreaterThanOrEqualTo(risen[seat.NodeId] - Tolerance),
                        "node " + seat.NodeId + " sank at width " + width.ToString("0.####"));

                    var travelled = Math.Abs(seat.Lift - risen[seat.NodeId]);
                    jump = travelled > jump ? travelled : jump;
                    risen[seat.NodeId] = seat.Lift;
                }

                NoPairCollides(Play(), seated, stack);
            }

            Assert.That(jump, Is.LessThan(0.02f), "a digit gain must not snap a badge into place");
        }

        [Test]
        public void PanningAndZoomingTheCameraLeavesEveryBadgeWhereItSat()
        {
            var spots = Counting(BadgeFit.Of(3f, Roomy).Width);
            var stack = BadgeCrowd.Resolve(spots);
            var seated = BadgeCrowd.Seated(spots, stack);

            for (var step = 0; step <= 60; step++)
            {
                var slide = -3f + step * 0.1f;
                var framing = new CameraFraming(
                    Anchor(slide, slide * 0.4f, 0f),
                    LevelFraming.PlaySize * (1f + 0.4f * (float)Math.Sin(step * 0.3f)));

                NoPairCollides(framing, seated, stack);
            }
        }

        [Test]
        public void ACrowdTooDeepToStackFadesTheFartherBadgeAndNeverPutsItOut()
        {
            var spots = Huddle(14, 0f, 0f);
            var stack = BadgeCrowd.Resolve(spots);

            Assert.That(stack.Faded, Is.GreaterThan(0), "fourteen badges on one spot cannot all be stacked");

            foreach (var seat in stack.Seats)
            {
                Assert.That(seat.Lift, Is.LessThanOrEqualTo(BadgeCrowd.LiftCeiling + Tolerance), seat.ToString());
                Assert.That(
                    seat.Opacity,
                    Is.GreaterThanOrEqualTo(BadgeCrowd.FaintestOpacity - Tolerance),
                    seat.ToString());
            }

            for (var slot = 0; slot < spots.Count; slot++)
            {
                for (var other = 0; other < spots.Count; other++)
                {
                    if (spots[slot].Depth <= spots[other].Depth)
                    {
                        continue;
                    }

                    Assert.That(
                        stack.Of(spots[slot].NodeId).Opacity,
                        Is.LessThanOrEqualTo(stack.Of(spots[other].NodeId).Opacity + Tolerance),
                        "the farther badge of a jammed pair is the one that gives way");
                }
            }
        }

        [Test]
        public void TheCrowdFadeMultipliesTheTopologyWashRatherThanReplacingIt()
        {
            var topology = new[] { 1f, 0.7f, Dimmest };

            foreach (var wash in topology)
            {
                for (var step = 0; step <= 20; step++)
                {
                    var shown = BadgeCrowd.OpacityFor(step * 0.05f * BadgeCrowd.FadeSpan) * wash;

                    Assert.That(shown, Is.GreaterThan(0f));
                    Assert.That(shown, Is.LessThanOrEqualTo(wash + Tolerance));
                    Assert.That(
                        shown,
                        Is.GreaterThanOrEqualTo(BadgeCrowd.FaintestOpacity * wash - Tolerance));
                }
            }

            Assert.That(BadgeCrowd.FaintestOpacity * Dimmest, Is.GreaterThanOrEqualTo(Readable));
        }

        [Test]
        public void ClearanceRisesSmoothlyOutOfNothingAsTwoBadgesDriftTogether()
        {
            var previous = 0f;

            for (var step = 60; step >= 0; step--)
            {
                var apart = step * 0.02f;
                var clearance = BadgeCrowd.ClearanceBetween(
                    Spot(0, 0f, 0f, 0f, 0.5f), Spot(1, apart, 0f, 1f, 0.5f));

                Assert.That(clearance, Is.GreaterThanOrEqualTo(previous - Tolerance), apart.ToString("0.###"));
                Assert.That(clearance - previous, Is.LessThan(0.08f), apart.ToString("0.###"));
                previous = clearance;
            }

            Assert.That(previous, Is.GreaterThan(BadgeMetrics.Height));
        }

        [Test]
        public void NoTwoBadgesOnAGeneratedLevelOverlapAtThePlayFraming()
        {
            var presets = new[] { MazePreset.Tiny, MazePreset.Ship, MazePreset.Stress };
            var crowded = 0;
            var stacked = 0;

            foreach (var preset in presets)
            {
                for (var step = 0; step < Sweep; step++)
                {
                    var graph = LevelGenerator.Generate(FirstSeed + step, preset).Graph;
                    var spots = Dressed(graph);
                    var framing = LevelFraming.Play(LevelFraming.Centre(graph));
                    var stack = BadgeCrowd.Resolve(spots);

                    for (var slot = 0; slot < spots.Count; slot++)
                    {
                        for (var other = slot + 1; other < spots.Count; other++)
                        {
                            if (Overlapping(framing, spots[slot], spots[other]))
                            {
                                crowded++;
                            }
                        }
                    }

                    stacked += stack.Stacked;
                    NoPairCollides(framing, BadgeCrowd.Seated(spots, stack), stack);
                }
            }

            Assert.That(crowded, Is.GreaterThan(0), "no generated level crowds its badges, so this proves nothing");
            Assert.That(stacked, Is.GreaterThan(0));
        }

        static IReadOnlyList<BadgeSpot> Dressed(LevelGraph graph)
        {
            var badges = BadgeBlueprintBuilder.Build(graph, StartingPower);
            var spots = new List<BadgeSpot>();
            var loudest = TargetMarks.Look(TargetMark.Win).Scale;

            foreach (var part in badges.Badges)
            {
                var size = part.Style == BadgeStyle.Player
                    ? BadgeFit.Of(BadgeText.Cells(part.Style, Grown), part.SubjectWidth)
                    : part.Size;

                spots.Add(new BadgeSpot(
                    part.NodeId,
                    part.Elevation,
                    part.Position,
                    size.Width * loudest,
                    size.Height * loudest));
            }

            return spots;
        }

        static void NoPairCollides(CameraFraming framing, IReadOnlyList<BadgeSpot> seated, BadgeStack stack)
        {
            for (var slot = 0; slot < seated.Count; slot++)
            {
                for (var other = slot + 1; other < seated.Count; other++)
                {
                    if (!Overlapping(framing, seated[slot], seated[other]))
                    {
                        continue;
                    }

                    var behind = seated[slot].Depth > seated[other].Depth ? seated[slot] : seated[other];

                    Assert.That(
                        stack.Of(behind.NodeId).Opacity,
                        Is.LessThan(1f),
                        seated[slot] + " still covers " + seated[other] + " at full strength");
                }
            }
        }

        static bool Overlapping(CameraFraming framing, BadgeSpot one, BadgeSpot other)
        {
            var pixels = ScreenProjection.PixelsPerMetre(framing.OrthographicSize, ScreenFrame.Height);
            var here = ScreenProjection.Of(framing, one.Anchor, ScreenFrame.Width, ScreenFrame.Height);
            var there = ScreenProjection.Of(framing, other.Anchor, ScreenFrame.Width, ScreenFrame.Height);

            return Math.Abs(here.X - there.X) < (one.Width + other.Width) * 0.5f * pixels
                && Math.Abs(here.Y - there.Y) < (one.Height + other.Height) * 0.5f * pixels;
        }

        static CameraFraming Play()
        {
            return new CameraFraming(Anchor(0f, 0f, 0f), LevelFraming.PlaySize);
        }

        static IReadOnlyList<BadgeSpot> Huddle(int count, float acrossStep, float upStep)
        {
            var spots = new BadgeSpot[count];

            for (var slot = 0; slot < count; slot++)
            {
                spots[slot] = Spot(slot, slot * acrossStep, slot * upStep, slot, 0.6f);
            }

            return spots;
        }

        static IReadOnlyList<BadgeSpot> Terrace(int count, float width)
        {
            var spots = new BadgeSpot[count];

            for (var slot = 0; slot < count; slot++)
            {
                spots[slot] = Spot(slot, slot * 0.34f, slot * 0.08f, slot, width);
            }

            return spots;
        }

        static IReadOnlyList<BadgeSpot> Counting(float width)
        {
            return new[]
            {
                Spot(0, 0f, 0f, 0f, width),
                Spot(1, 0.36f, 0.1f, 1f, 0.52f),
                Spot(2, -0.34f, 0.06f, 2f, 0.48f),
                Spot(3, 0.05f, 0.42f, 3f, 0.6f),
                Spot(4, 1.4f, -0.2f, 4f, 0.5f)
            };
        }

        static BadgeSpot Spot(int nodeId, float across, float up, float depth, float width)
        {
            return new BadgeSpot(nodeId, 0, Anchor(across, up, depth), width, BadgeMetrics.Height);
        }

        static WorldPoint Anchor(float across, float up, float depth)
        {
            var right = IsoProjection.CameraRight;
            var rising = IsoProjection.CameraUp;
            var forward = IsoProjection.CameraForward;

            return new WorldPoint(
                right.X * across + rising.X * up + forward.X * depth,
                right.Y * across + rising.Y * up + forward.Y * depth,
                right.Z * across + rising.Z * up + forward.Z * depth);
        }
    }
}
