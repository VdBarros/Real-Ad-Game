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
