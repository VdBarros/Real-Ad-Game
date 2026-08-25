using System;
using System.Collections.Generic;
using Game.Domain;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class WalkTests
    {
        const float Step = 1f / Walk.StepsPerSecond;

        [Test]
        public void NobodyIsWalkingUntilARouteIsGiven()
        {
            Assert.That(Walk.Nowhere.IsSettled, Is.True);
            Assert.That(Walk.Nowhere.IsWaiting, Is.False);
            Assert.That(Walk.Nowhere.ArrivedNodeId, Is.EqualTo(TapAim.Nothing));
            Assert.That(default(Walk).IsSettled, Is.True);
            Assert.That(default(Walk).Advanced(1f), Is.EqualTo(default(Walk)));
        }

        [Test]
        public void ARouteThatGoesNowhereIsOverBeforeItStarts()
        {
            var walk = Walk.Along(TileRoute.Of(RunFixture.Level(), new[] { RunFixture.Start }));

            Assert.That(walk.IsSettled, Is.True);
            Assert.That(walk.Position, Is.EqualTo(IsoProjection.Of(new TilePosition(0, 3, 2))));
        }

        [Test]
        public void AWalkStartsOnTheTileItsFirstNodeStandsOn()
        {
            var walk = Walk.Along(RunFixture.PastTheMultiplier());

            Assert.That(walk.IsSettled, Is.False);
            Assert.That(walk.IsWaiting, Is.False);
            Assert.That(walk.Travelled, Is.EqualTo(0f));
            Assert.That(walk.Position, Is.EqualTo(IsoProjection.Of(new TilePosition(0, 3, 2))));
        }

        [Test]
        public void MidStepTheWalkerStandsBetweenTwoTiles()
        {
            var walk = Walk.Along(RunFixture.PastTheMultiplier()).Advanced(Step * 0.5f);

            var from = IsoProjection.Of(new TilePosition(0, 3, 2));
            var to = IsoProjection.Of(new TilePosition(0, 2, 2));

            Assert.That(walk.Position.X, Is.EqualTo((from.X + to.X) * 0.5f).Within(0.0001f));
            Assert.That(walk.Position.Z, Is.EqualTo((from.Z + to.Z) * 0.5f).Within(0.0001f));
        }

        [Test]
        public void TheWalkStopsAtEveryNodeOnTheRouteAndNamesIt()
        {
            var walk = Walk.Along(RunFixture.PastTheMultiplier()).Advanced(Step * 10f);

            Assert.That(walk.IsWaiting, Is.True);
            Assert.That(walk.ArrivedNodeId, Is.EqualTo(RunFixture.Multiplier));
            Assert.That(walk.Position, Is.EqualTo(IsoProjection.Of(new TilePosition(0, 1, 2))));
            Assert.That(walk.Travelled, Is.EqualTo(2f));
        }

        [Test]
        public void AWaitingWalkGoesNoFurtherUntilItIsResumed()
        {
            var waiting = Walk.Along(RunFixture.PastTheMultiplier()).Advanced(Step * 10f);

            Assert.That(waiting.Advanced(Step * 10f), Is.EqualTo(waiting));

            var walked = waiting.Resumed().Advanced(Step * 10f);

            Assert.That(walked.ArrivedNodeId, Is.EqualTo(RunFixture.AdditiveBeyondTheMultiplier));
            Assert.That(walked.Position, Is.EqualTo(IsoProjection.Of(new TilePosition(0, 1, 4))));
        }

        [Test]
        public void EveryNodeOnTheRouteIsNamedOnceAndInOrder()
        {
            var walk = Walk.Along(RunFixture.PastTheMultiplier());
            var arrivals = new List<int>();

            for (var frame = 0; frame < 200 && !walk.IsSettled; frame++)
            {
                walk = walk.Advanced(1f / 60f);

                if (!walk.IsWaiting)
                {
                    continue;
                }

                arrivals.Add(walk.ArrivedNodeId);
                walk = walk.Resumed();
            }

            Assert.That(walk.IsSettled, Is.True);
            Assert.That(
                arrivals,
                Is.EqualTo(new[] { RunFixture.Multiplier, RunFixture.AdditiveBeyondTheMultiplier }));
        }

        [Test]
        public void AWalkCutShortStillFinishesTheLegItIsOn()
        {
            var walk = Walk.Along(RunFixture.PastTheMultiplier()).Advanced(Step * 0.5f).Stopped();

            Assert.That(walk.IsSettled, Is.False);

            walk = walk.Advanced(Step * 10f);

            Assert.That(walk.ArrivedNodeId, Is.EqualTo(RunFixture.Multiplier));
            Assert.That(walk.Resumed().IsSettled, Is.True);
        }

        [Test]
        public void AWalkCutShortWhileWaitingEndsAtTheNodeItWaitsOn()
        {
            var walk = Walk.Along(RunFixture.PastTheMultiplier()).Advanced(Step * 10f).Stopped();

            Assert.That(walk.ArrivedNodeId, Is.EqualTo(RunFixture.Multiplier));
            Assert.That(walk.Resumed().IsSettled, Is.True);
        }

        [Test]
        public void AWalkCutShortWalksOnlyAsFarAsTheTilesItKeeps()
        {
            var walk = Walk.Along(RunFixture.PastTheMultiplier()).Stopped();

            Assert.That(walk.Route.Nodes, Is.EqualTo(new[] { RunFixture.Start, RunFixture.Multiplier }));
        }

        [Test]
        public void ASettledWalkIsPastCuttingShort()
        {
            var walk = Walk.Along(TileRoute.Of(RunFixture.Level(), new[] { RunFixture.Start }));

            Assert.That(walk.Stopped(), Is.EqualTo(walk));
            Assert.That(walk.Resumed(), Is.EqualTo(walk));
        }

        [Test]
        public void AWalkThatFallsBackReturnsToTheNodeItLastStoodOn()
        {
            var walk = Walk.Along(RunFixture.PastTheMultiplier()).Advanced(Step * 1.5f).Backtracked();

            Assert.That(walk.IsRetreating, Is.True);
            Assert.That(walk.IsWaiting, Is.False);
            Assert.That(walk.IsSettled, Is.False);

            walk = walk.Advanced(Step * 10f);

            Assert.That(walk.IsSettled, Is.True);
            Assert.That(walk.Position, Is.EqualTo(IsoProjection.Of(new TilePosition(0, 3, 2))));
        }

        [Test]
        public void AWalkFallingBackNeverNamesAnotherNodeToResolve()
        {
            var walk = Walk.Along(RunFixture.PastTheMultiplier()).Advanced(Step * 1.9f).Backtracked();

            for (var frame = 0; frame < 200 && !walk.IsSettled; frame++)
            {
                Assert.That(walk.ArrivedNodeId, Is.EqualTo(TapAim.Nothing));
                walk = walk.Advanced(1f / 60f);
            }

            Assert.That(walk.IsSettled, Is.True);
        }

        [Test]
        public void AWalkFallingBackFromTheSecondLegReturnsToTheNodeBetweenThem()
        {
            var walk = Walk.Along(RunFixture.PastTheMultiplier())
                .Advanced(Step * 10f)
                .Resumed()
                .Advanced(Step * 0.5f)
                .Backtracked()
                .Advanced(Step * 10f);

            Assert.That(walk.IsSettled, Is.True);
            Assert.That(walk.Position, Is.EqualTo(IsoProjection.Of(new TilePosition(0, 1, 2))));
        }

        [Test]
        public void AWalkAlreadyFallingBackDoesNotTurnRoundTwice()
        {
            var walk = Walk.Along(RunFixture.PastTheMultiplier()).Advanced(Step * 0.5f).Backtracked();

            Assert.That(walk.Backtracked(), Is.EqualTo(walk));
            Assert.That(walk.Resumed(), Is.EqualTo(walk));
        }

        [Test]
        public void AWalkOnlyEverRunsForwards()
        {
            var walk = Walk.Along(RunFixture.PastTheMultiplier());

            Assert.That(() => walk.Advanced(-0.1f), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void AWalkNeedsARouteToFollow()
        {
            Assert.That(() => Walk.Along(null), Throws.ArgumentNullException);
        }
    }
}
