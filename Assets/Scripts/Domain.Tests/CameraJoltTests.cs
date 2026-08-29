using Game.Domain;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class CameraJoltTests
    {
        const float Frame = 1f / 60f;

        const float Tolerance = 1e-4f;

        static readonly WorldPoint Framing = new WorldPoint(3f, 9f, -4f);

        [Test]
        public void ACameraNothingHasStruckSitsExactlyWhereItsFramingPutsIt()
        {
            Assert.That(CameraJolt.Offset(0f), Is.EqualTo(default(WorldPoint)));
            Assert.That(CameraJolt.Jolted(Framing, 0f), Is.EqualTo(Framing));
        }

        [Test]
        public void AnImpulseOutsideItsRangeIsHeldInsideIt()
        {
            Assert.That(CameraJolt.Clamped(-3f), Is.EqualTo(0f));
            Assert.That(CameraJolt.Clamped(0.4f), Is.EqualTo(0.4f));
            Assert.That(CameraJolt.Clamped(9f), Is.EqualTo(1f));
            Assert.That(CameraJolt.Offset(9f), Is.EqualTo(CameraJolt.Offset(1f)));
            Assert.That(CameraJolt.Offset(-9f), Is.EqualTo(CameraJolt.Offset(0f)));
        }

        [Test]
        public void AFullImpulseDropsTheCameraAndTheHalfOneDropsItHalfAsFar()
        {
            var full = CameraJolt.Offset(1f);
            var half = CameraJolt.Offset(0.5f);

            Assert.That(full.Y, Is.EqualTo(-CameraJolt.Drop).Within(Tolerance));
            Assert.That(half.Y, Is.EqualTo(-CameraJolt.Drop * 0.5f).Within(Tolerance));
            Assert.That(full.X, Is.EqualTo(CameraJolt.Yield).Within(Tolerance));
            Assert.That(full.Z, Is.EqualTo(CameraJolt.Yield).Within(Tolerance));
            Assert.That(CameraJolt.Drop, Is.GreaterThan(CameraJolt.Yield));
        }

        [Test]
        public void TheJoltRidesOnTopOfTheFramingRatherThanReplacingIt()
        {
            var jolted = CameraJolt.Jolted(Framing, 1f);
            var offset = CameraJolt.Offset(1f);

            Assert.That(jolted.X, Is.EqualTo(Framing.X + offset.X).Within(Tolerance));
            Assert.That(jolted.Y, Is.EqualTo(Framing.Y + offset.Y).Within(Tolerance));
            Assert.That(jolted.Z, Is.EqualTo(Framing.Z + offset.Z).Within(Tolerance));
        }

        [Test]
        public void EveryFightKicksTheCameraAtContactAndHandsItBackBeforeItSettles()
        {
            var outcomes = new[] { ActionOutcome.Win, ActionOutcome.Tie, ActionOutcome.Loss };

            foreach (var outcome in outcomes)
            {
                var fight = Fight.Of(outcome);
                var deepest = 0f;
                var kicked = 0;

                while (!fight.IsSettled)
                {
                    var dropped = -CameraJolt.Jolted(Framing, fight.Impact).Y + Framing.Y;

                    if (dropped > 0f)
                    {
                        kicked++;
                    }

                    deepest = dropped > deepest ? dropped : deepest;
                    fight = fight.Advanced(Frame);
                }

                Assert.That(kicked, Is.GreaterThan(0), outcome.ToString());
                Assert.That(deepest, Is.GreaterThan(0f), outcome.ToString());
                Assert.That(deepest, Is.LessThanOrEqualTo(CameraJolt.Drop), outcome.ToString());
                Assert.That(
                    CameraJolt.Jolted(Framing, fight.Impact), Is.EqualTo(Framing), outcome.ToString());
            }
        }
    }
}
