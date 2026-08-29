using System;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class FigureTurnTests
    {
        const float Tolerance = 1e-3f;

        const float Frame = 1f / 60f;

        static void Aimed(float yaw, float wanted, string note)
        {
            Assert.That(
                Math.Abs(FigureFacing.Shortest(yaw, wanted)),
                Is.LessThanOrEqualTo(Tolerance),
                note + ": " + yaw + " against " + wanted);
        }

        static FigureTurn Run(FigureTurn turn, float seconds)
        {
            var frames = (int)Math.Ceiling(seconds / Frame);

            for (var frame = 0; frame < frames; frame++)
            {
                turn = turn.Advanced(Frame);
            }

            return turn;
        }

        [Test]
        public void AFigureNobodyHasTurnedStandsWhereItWasPlaced()
        {
            var turn = FigureTurn.Facing(225f);

            Assert.That(turn.Yaw, Is.EqualTo(225f).Within(Tolerance));
            Assert.That(turn.Wanted, Is.EqualTo(225f).Within(Tolerance));
            Assert.That(turn.IsSettled, Is.True);
            Assert.That(turn.Seconds, Is.EqualTo(0f));
        }

        [Test]
        public void AFigurePlacedOutsideTheCircleIsCarriedIntoIt()
        {
            Assert.That(FigureTurn.Facing(-45f).Yaw, Is.EqualTo(315f).Within(Tolerance));
            Assert.That(FigureTurn.Facing(585f).Yaw, Is.EqualTo(225f).Within(Tolerance));
        }

        [Test]
        public void NobodyTurnsThroughMoreThanHalfACircleToFaceAnything()
        {
            Assert.That(
                Math.Abs(FigureTurn.Facing(350f).Toward(10f).Swing),
                Is.EqualTo(20f).Within(Tolerance));
            Assert.That(
                Math.Abs(FigureTurn.Facing(10f).Toward(350f).Swing),
                Is.EqualTo(20f).Within(Tolerance));
            Assert.That(
                Math.Abs(FigureTurn.Facing(0f).Toward(270f).Swing),
                Is.EqualTo(90f).Within(Tolerance));
        }

        [Test]
        public void ATurnTakesTheShortWayRoundRatherThanUnwinding()
        {
            var turn = FigureTurn.Facing(350f).Toward(10f);

            for (var frame = 0; frame < 60 && !turn.IsSettled; frame++)
            {
                turn = turn.Advanced(Frame);

                Assert.That(
                    Math.Abs(FigureFacing.Shortest(350f, turn.Yaw)),
                    Is.LessThanOrEqualTo(20f + Tolerance),
                    turn.ToString());
            }

            Aimed(turn.Yaw, 10f, "the short way round");
        }

        [Test]
        public void TheWidestTurnSettlesWellInsideTheTimeToCrossOneTile()
        {
            var widest = FigureTurn.SecondsToTurn(FigureFacing.HalfTurn);

            Assert.That(widest, Is.LessThan(FigureTurn.TileSeconds));
            Assert.That(widest, Is.LessThanOrEqualTo(FigureTurn.TileSeconds * 0.7f));
            Assert.That(
                FigureTurn.SecondsToTurn(90f),
                Is.LessThanOrEqualTo(FigureTurn.TileSeconds * 0.4f));
            Assert.That(
                FigureTurn.DegreesPerSecond,
                Is.EqualTo(FigureFacing.HalfTurn * FigureTurn.HalfTurnsPerTile / FigureTurn.TileSeconds)
                    .Within(Tolerance));
        }

        [Test]
        public void AQuarterTurnIsOverBeforeTheNextTileIsReached()
        {
            var turn = Run(FigureTurn.Facing(0f).Toward(90f), FigureTurn.TileSeconds);

            Assert.That(turn.IsSettled, Is.True);
            Aimed(turn.Yaw, 90f, "a quarter turn");
        }

        [Test]
        public void AHalfTurnIsOverBeforeTheNextTileIsReached()
        {
            var turn = Run(FigureTurn.Facing(0f).Toward(180f), FigureTurn.TileSeconds);

            Assert.That(turn.IsSettled, Is.True);
            Aimed(turn.Yaw, 180f, "a half turn");
        }

        [Test]
        public void ATurnEasesInAndOutRatherThanSnapping()
        {
            var turn = FigureTurn.Facing(0f).Toward(180f);
            var span = turn.Seconds;
            var quarter = turn.Advanced(span * 0.25f).Yaw;
            var half = turn.Advanced(span * 0.5f).Yaw;
            var third = turn.Advanced(span * 0.75f).Yaw;

            Assert.That(turn.Yaw, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(half, Is.EqualTo(90f).Within(Tolerance));
            Assert.That(quarter, Is.LessThan(45f));
            Assert.That(third, Is.GreaterThan(135f));
            Assert.That(quarter, Is.EqualTo(180f - third).Within(Tolerance));
            Assert.That(half - quarter, Is.GreaterThan(quarter));
            Assert.That(third - half, Is.GreaterThan(180f - third));
        }

        [Test]
        public void ATurnNeverMovesFasterThanItsRateAllowsInOneFrame()
        {
            var turn = FigureTurn.Facing(0f).Toward(180f);
            var cap = FigureTurn.DegreesPerSecond * Frame * 1.55f;
            var was = turn.Yaw;

            for (var frame = 0; frame < 60 && !turn.IsSettled; frame++)
            {
                turn = turn.Advanced(Frame);

                Assert.That(
                    Math.Abs(FigureFacing.Shortest(was, turn.Yaw)),
                    Is.LessThanOrEqualTo(cap),
                    turn.ToString());

                was = turn.Yaw;
            }
        }

        [Test]
        public void ATurnSettlesExactlyOnWhatItWasAimedAt()
        {
            foreach (var wanted in new[] { 0f, 37f, 90f, 180f, 271.5f, 359f })
            {
                var turn = Run(FigureTurn.Facing(225f).Toward(wanted), FigureTurn.TileSeconds);

                Assert.That(turn.IsSettled, Is.True, wanted.ToString());
                Aimed(turn.Yaw, wanted, wanted.ToString());
                Aimed(turn.Wanted, wanted, wanted.ToString());
            }
        }

        [Test]
        public void BeingToldAgainWhereItIsAlreadyHeadedNeverStartsATurnOver()
        {
            var turn = FigureTurn.Facing(0f).Toward(180f);
            var frames = 0;

            for (; frames < 60 && !turn.IsSettled; frames++)
            {
                turn = turn.Toward(180f).Advanced(Frame);
            }

            Assert.That(turn.IsSettled, Is.True);
            Assert.That(frames * Frame, Is.LessThanOrEqualTo(FigureTurn.TileSeconds));
            Aimed(turn.Yaw, 180f, "told again every frame");
        }

        [Test]
        public void AFigureNobodyRetargetsHoldsTheFacingItSettledOn()
        {
            var turn = Run(FigureTurn.Facing(225f).Toward(90f), FigureTurn.TileSeconds);
            var held = turn.Yaw;

            for (var frame = 0; frame < 600; frame++)
            {
                turn = turn.Advanced(Frame);
            }

            Assert.That(turn.Yaw, Is.EqualTo(held));
            Assert.That(turn.IsSettled, Is.True);
            Aimed(turn.Yaw, 90f, "held after settling");
        }

        [Test]
        public void ATurnRetargetedPartwayCarriesOnFromWhereItHadGot()
        {
            var turn = FigureTurn.Facing(0f).Toward(180f).Advanced(Frame * 3f);
            var partway = turn.Yaw;

            Assert.That(partway, Is.GreaterThan(0f));
            Assert.That(partway, Is.LessThan(180f));

            var again = turn.Toward(270f);

            Assert.That(again.Yaw, Is.EqualTo(partway).Within(Tolerance));
            Aimed(Run(again, FigureTurn.TileSeconds).Yaw, 270f, "retargeted partway");
        }

        [Test]
        public void ATurnOnlyEverRunsForwards()
        {
            Assert.That(
                () => FigureTurn.Facing(0f).Advanced(-Frame),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void OneTurnEqualsAnotherWhenItHasTheSameStoryBehindIt()
        {
            var turn = FigureTurn.Facing(0f).Toward(90f).Advanced(Frame);
            var same = FigureTurn.Facing(0f).Toward(90f).Advanced(Frame);

            Assert.That(turn, Is.EqualTo(same));
            Assert.That(turn.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(turn, Is.Not.EqualTo(FigureTurn.Facing(0f)));
            Assert.That(FigureTurn.Facing(0f).Toward(0f), Is.EqualTo(FigureTurn.Facing(0f)));
        }
    }
}
