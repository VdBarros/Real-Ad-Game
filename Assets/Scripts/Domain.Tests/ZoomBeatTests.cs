using System;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class ZoomBeatTests
    {
        static readonly CameraFraming Subject = LevelFraming.CloseUp(new TilePosition(2, 3, 4));

        [Test]
        public void NoBeatIsRunningUntilOneIsCutTo()
        {
            Assert.That(ZoomBeat.None.IsSettled, Is.True);
            Assert.That(default(ZoomBeat).IsSettled, Is.True);
            Assert.That(ZoomBeat.On(Subject).IsSettled, Is.False);
        }

        [Test]
        public void ABeatCutsStraightToItsSubjectAndHoldsThereWithoutMoving()
        {
            var beat = ZoomBeat.On(Subject);

            Assert.That(beat.Framing, Is.EqualTo(Subject));

            beat = beat.Advanced(0.2f);

            Assert.That(beat.Framing, Is.EqualTo(Subject));
            Assert.That(beat.Framing.OrthographicSize, Is.EqualTo(LevelFraming.CloseUpSize));
        }

        [Test]
        public void AReleaseBeforeTheFloorStillHoldsUntilTheFloor()
        {
            var beat = ZoomBeat.On(Subject).Advanced(0.1f).Released();

            Assert.That(beat.IsSettled, Is.False);

            beat = beat.Advanced(ZoomBeat.FloorSeconds - 0.1f);

            Assert.That(beat.IsSettled, Is.True);
        }

        [Test]
        public void AnEnclosedAnimationThatNeverSettlesIsCutOffAtTheCap()
        {
            var beat = ZoomBeat.On(Subject).Advanced(ZoomBeat.CapSeconds);

            Assert.That(beat.IsSettled, Is.True);
        }

        [Test]
        public void ABeatHoldsForAsLongAsTheAnimationItEnclosesTakes()
        {
            var beat = ZoomBeat.On(Subject).Advanced(ZoomBeat.FloorSeconds + 0.2f);

            Assert.That(beat.IsSettled, Is.False);

            Assert.That(beat.Released().IsSettled, Is.True);
        }

        [Test]
        public void ABeatOnlyEverRunsForwards()
        {
            Assert.That(
                () => ZoomBeat.On(Subject).Advanced(-0.1f),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }
    }
}
