using System;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class GameCycleTests
    {
        const int Cycles = 20;

        [Test]
        public void AGameOpensBootingBeforeAnyLevelExists()
        {
            var cycle = GameCycle.Booting;

            Assert.That(cycle.Phase, Is.EqualTo(GamePhase.Boot));
            Assert.That(cycle.LevelNumber, Is.Zero);
            Assert.That(cycle.FinalPower, Is.Zero);
        }

        [Test]
        public void TheCutsceneOpensTheGameAndTheFirstLevelFollowsIt()
        {
            var cycle = GameCycle.Booting.Watching();

            Assert.That(cycle.Phase, Is.EqualTo(GamePhase.Cutscene));
            Assert.That(cycle.LevelNumber, Is.Zero);

            cycle = cycle.Generating();

            Assert.That(cycle.Phase, Is.EqualTo(GamePhase.Generating));
            Assert.That(cycle.LevelNumber, Is.EqualTo(1));
        }

        [Test]
        public void TheFlyThroughRunsOverTheLevelThatWasJustGenerated()
        {
            var cycle = Generated().Previewing();

            Assert.That(cycle.Phase, Is.EqualTo(GamePhase.Preview));
            Assert.That(cycle.LevelNumber, Is.EqualTo(1));
        }

        [Test]
        public void PlayBeginsWhereTheFlyThroughLands()
        {
            var cycle = Generated().Previewing().Playing();

            Assert.That(cycle.Phase, Is.EqualTo(GamePhase.Play));
            Assert.That(cycle.LevelNumber, Is.EqualTo(1));
        }

        [Test]
        public void BeatingTheBossLeavesAResultHoldingTheFinalPower()
        {
            var cycle = Playing().Finished(97);

            Assert.That(cycle.Phase, Is.EqualTo(GamePhase.Result));
            Assert.That(cycle.FinalPower, Is.EqualTo(97));
            Assert.That(cycle.LevelNumber, Is.EqualTo(1));
        }

        [Test]
        public void NextGeneratesAFreshLevelAndForgetsTheLastResult()
        {
            var cycle = Playing().Finished(97).Generating();

            Assert.That(cycle.Phase, Is.EqualTo(GamePhase.Generating));
            Assert.That(cycle.LevelNumber, Is.EqualTo(2));
            Assert.That(cycle.FinalPower, Is.Zero);
        }

        [Test]
        public void TwentyTurnsOfTheLoopCountTwentyLevels()
        {
            var cycle = GameCycle.Booting.Watching();

            for (var turn = 1; turn <= Cycles; turn++)
            {
                cycle = cycle.Generating();
                Assert.That(cycle.LevelNumber, Is.EqualTo(turn));

                cycle = cycle.Previewing().Playing().Finished(turn);
                Assert.That(cycle.Phase, Is.EqualTo(GamePhase.Result));
                Assert.That(cycle.FinalPower, Is.EqualTo(turn));
            }

            Assert.That(cycle.LevelNumber, Is.EqualTo(Cycles));
        }

        [Test]
        public void TheCutsceneOnlyEverOpensTheGame()
        {
            Assert.That(
                () => GameCycle.Booting.Watching().Watching(),
                Throws.InvalidOperationException.With.Message.Contains("Cutscene"));
        }

        [Test]
        public void ALevelIsNotGeneratedFromTheBootPhase()
        {
            Assert.That(
                () => GameCycle.Booting.Generating(),
                Throws.InvalidOperationException.With.Message.Contains("after the cutscene"));
        }

        [Test]
        public void ALevelIsNotPreviewedBeforeItIsGenerated()
        {
            Assert.That(
                () => GameCycle.Booting.Watching().Previewing(),
                Throws.InvalidOperationException.With.Message.Contains("just been generated"));
        }

        [Test]
        public void PlayDoesNotBeginWhileTheLevelIsStillBeingGenerated()
        {
            Assert.That(
                () => Generated().Playing(),
                Throws.InvalidOperationException.With.Message.Contains("fly-through lands"));
        }

        [Test]
        public void AResultIsNotShownWithoutPlayingForIt()
        {
            Assert.That(
                () => Generated().Previewing().Finished(12),
                Throws.InvalidOperationException.With.Message.Contains("beating the boss"));
        }

        [Test]
        public void AResultCannotBeShownWithoutPower()
        {
            Assert.That(
                () => Playing().Finished(0),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TheSamePhaseOfTheSameLevelAtTheSamePowerIsTheSameCycle()
        {
            var one = Playing().Finished(97);
            var other = Playing().Finished(97);

            Assert.That(one, Is.EqualTo(other));
            Assert.That(one.GetHashCode(), Is.EqualTo(other.GetHashCode()));
            Assert.That(one, Is.Not.EqualTo(Playing().Finished(98)));
        }

        [Test]
        public void ACycleReadsAsItsPhaseAndWhatItIsHolding()
        {
            Assert.That(GameCycle.Booting.ToString(), Is.EqualTo("Boot"));
            Assert.That(Playing().ToString(), Is.EqualTo("Play of level 1"));
            Assert.That(Playing().Finished(97).ToString(), Is.EqualTo("Result of level 1 at power 97"));
        }

        static GameCycle Generated()
        {
            return GameCycle.Booting.Watching().Generating();
        }

        static GameCycle Playing()
        {
            return Generated().Previewing().Playing();
        }
    }
}
