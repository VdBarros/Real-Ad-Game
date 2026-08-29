using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class TapAimTests
    {
        const float Reach = 70f;

        static TapCandidate At(int nodeId, float x, float y, float depth = 10f)
        {
            return new TapCandidate(nodeId, new ScreenPoint(x, y), depth);
        }

        [Test]
        public void NothingIsAimedAtWhenNothingIsOffered()
        {
            Assert.That(
                TapAim.Of(new TapCandidate[0], new ScreenPoint(100f, 100f), Reach),
                Is.EqualTo(TapAim.Nothing));
        }

        [Test]
        public void TheNearestCandidateWithinReachIsAimedAt()
        {
            var candidates = new List<TapCandidate> { At(4, 100f, 100f), At(7, 130f, 100f) };

            Assert.That(TapAim.Of(candidates, new ScreenPoint(120f, 100f), Reach), Is.EqualTo(7));
        }

        [Test]
        public void ACandidateBeyondReachIsNotAimedAtNoMatterHowAloneItIs()
        {
            var candidates = new List<TapCandidate> { At(4, 100f, 100f) };

            Assert.That(
                TapAim.Of(candidates, new ScreenPoint(100f + Reach + 1f, 100f), Reach),
                Is.EqualTo(TapAim.Nothing));
        }

        [Test]
        public void ACandidateExactlyAtReachIsStillAimedAt()
        {
            var candidates = new List<TapCandidate> { At(4, 100f, 100f) };

            Assert.That(TapAim.Of(candidates, new ScreenPoint(100f + Reach, 100f), Reach), Is.EqualTo(4));
        }

        [Test]
        public void TwoCandidatesTheSameDistanceAwayGoToTheOneNearerTheCamera()
        {
            var candidates = new List<TapCandidate>
            {
                At(4, 90f, 100f, depth: 12f),
                At(7, 110f, 100f, depth: 8f)
            };

            Assert.That(TapAim.Of(candidates, new ScreenPoint(100f, 100f), Reach), Is.EqualTo(7));
        }

        [Test]
        public void TwoCandidatesAtTheSameDistanceAndDepthGoToTheLowerNodeId()
        {
            var candidates = new List<TapCandidate>
            {
                At(7, 110f, 100f),
                At(4, 90f, 100f)
            };

            Assert.That(TapAim.Of(candidates, new ScreenPoint(100f, 100f), Reach), Is.EqualTo(4));
        }

        [Test]
        public void TheAimDoesNotDependOnTheOrderTheCandidatesArrivedIn()
        {
            var candidates = new List<TapCandidate>
            {
                At(1, 40f, 40f),
                At(2, 100f, 100f),
                At(3, 160f, 160f)
            };
            var reversed = new List<TapCandidate>(candidates);
            reversed.Reverse();

            var pointer = new ScreenPoint(110f, 105f);

            Assert.That(TapAim.Of(reversed, pointer, Reach), Is.EqualTo(TapAim.Of(candidates, pointer, Reach)));
        }

        [Test]
        public void AnAimSweptAcrossARowOfTargetsNeverBlinksThroughNothing()
        {
            var swept = Swept(Row(), holding: true);

            Assert.That(swept[0], Is.EqualTo(4));
            Assert.That(swept[swept.Count - 1], Is.EqualTo(9));
            Assert.That(swept, Has.No.Member(TapAim.Nothing));
        }

        [Test]
        public void AnAimSweptAcrossARowOfTargetsSettlesOnEachOfThemExactlyOnce()
        {
            var swept = Swept(Row(), holding: true);
            var visited = new List<int>();

            for (var sample = 0; sample < swept.Count; sample++)
            {
                if (sample != 0 && swept[sample] == swept[sample - 1])
                {
                    continue;
                }

                Assert.That(
                    visited,
                    Has.No.Member(swept[sample]),
                    "Node " + swept[sample] + " was aimed at again after the sweep had left it.");
                visited.Add(swept[sample]);
            }

            Assert.That(visited, Is.EqualTo(new[] { 4, 7, 9 }));
        }

        [Test]
        public void WithoutTheHoldTheSameSweepFallsThroughTheGapsBetweenTheTargets()
        {
            var swept = Swept(Row(), holding: false);

            Assert.That(
                swept,
                Has.Member(TapAim.Nothing),
                "The row has to be spread far enough apart for the hold to be doing the work.");
        }

        [Test]
        public void AGapWiderThanTheHoldPromisesIsWhereThePreviewFinallyLetsGo()
        {
            var spacing = (1f + TapAim.Hold) * Reach + 2f;
            var candidates = new List<TapCandidate> { At(4, 100f, 100f), At(7, 100f + spacing, 100f) };

            Assert.That(Swept(candidates, holding: true), Has.Member(TapAim.Nothing));
        }

        [Test]
        public void TheHoldKeepsATargetOnlyWhileTheFingerLingersNearIt()
        {
            var candidates = new List<TapCandidate> { At(4, 100f, 100f) };
            var lingering = new ScreenPoint(100f + Reach * TapAim.Hold, 100f);
            var gone = new ScreenPoint(100f + Reach * TapAim.Hold + 1f, 100f);

            Assert.That(TapAim.Of(candidates, lingering, Reach, held: 4), Is.EqualTo(4));
            Assert.That(TapAim.Of(candidates, gone, Reach, held: 4), Is.EqualTo(TapAim.Nothing));
        }

        [Test]
        public void TheHoldNeverInventsAnAimTheStrictReachHadNotAlreadyWon()
        {
            var candidates = new List<TapCandidate> { At(4, 100f, 100f), At(7, 260f, 100f) };
            var between = new ScreenPoint(180f, 100f);

            Assert.That(TapAim.Of(candidates, between, Reach), Is.EqualTo(TapAim.Nothing));
            Assert.That(TapAim.Of(candidates, between, Reach, held: TapAim.Nothing), Is.EqualTo(TapAim.Nothing));
        }

        [Test]
        public void AFreshTargetWithinReachAlwaysBeatsTheOneBeingHeld()
        {
            var candidates = new List<TapCandidate> { At(4, 100f, 100f), At(7, 200f, 100f) };

            Assert.That(TapAim.Of(candidates, new ScreenPoint(190f, 100f), Reach, held: 4), Is.EqualTo(7));
        }

        static List<TapCandidate> Row()
        {
            var spacing = (1f + TapAim.Hold) * Reach;

            return new List<TapCandidate>
            {
                At(4, 100f, 100f),
                At(7, 100f + spacing, 100f),
                At(9, 100f + spacing * 2f, 100f)
            };
        }

        static List<int> Swept(IReadOnlyList<TapCandidate> candidates, bool holding)
        {
            var swept = new List<int>();
            var held = TapAim.Nothing;
            var last = candidates[candidates.Count - 1].Point.X;

            for (var x = 100f; x <= last; x += 1f)
            {
                var finger = new ScreenPoint(x, 100f);
                held = holding
                    ? TapAim.Of(candidates, finger, Reach, held)
                    : TapAim.Of(candidates, finger, Reach);
                swept.Add(held);
            }

            return swept;
        }

        [Test]
        public void AReachThatIsNotPositiveIsNotAReach()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => TapAim.Of(new TapCandidate[0], new ScreenPoint(0f, 0f), 0f));
        }

        [Test]
        public void CandidatesAreRequired()
        {
            Assert.Throws<ArgumentNullException>(() => TapAim.Of(null, new ScreenPoint(0f, 0f), Reach));
        }
    }
}
