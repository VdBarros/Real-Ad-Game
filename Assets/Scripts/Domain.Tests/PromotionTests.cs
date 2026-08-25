using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class PromotionTests
    {
        const float Frame = 1f / 60f;

        [Test]
        public void ASettledPromotionAlreadyWearsItsTargetAndStaysThere()
        {
            var look = PlayerLook.Of(50);
            var promotion = Promotion.Settled(look).Advanced(Frame);

            Assert.That(promotion.IsSettled, Is.True);
            Assert.That(promotion.Scale, Is.EqualTo(look.Scale).Within(1e-6f));
            Assert.That(promotion.Tint, Is.EqualTo(look.Tint));
        }

        [Test]
        public void APromotionOvershootsPastTheNewScaleBeforeSettlingOnIt()
        {
            var from = PlayerLook.Of(2);
            var to = PlayerLook.Of(9);
            var promotion = Promotion.Settled(from).Toward(to);

            var peak = promotion.Scale;
            while (!promotion.IsSettled)
            {
                promotion = promotion.Advanced(Frame);
                if (promotion.Scale > peak)
                {
                    peak = promotion.Scale;
                }
            }

            Assert.That(peak, Is.GreaterThan(to.Scale));
            Assert.That(promotion.Scale, Is.EqualTo(to.Scale).Within(1e-6f));
        }

        [Test]
        public void TheOvershootIsBigEnoughToReadAsAnEventRatherThanADrift()
        {
            var from = PlayerLook.Of(2);
            var to = PlayerLook.Of(9);
            var promotion = Promotion.Settled(from).Toward(to);

            var peak = from.Scale;
            while (!promotion.IsSettled)
            {
                promotion = promotion.Advanced(Frame);
                if (promotion.Scale > peak)
                {
                    peak = promotion.Scale;
                }
            }

            Assert.That((peak - from.Scale) / (to.Scale - from.Scale), Is.GreaterThan(1.1f));
        }

        [Test]
        public void APromotionStartsWhereTheBodyAlreadyIsRatherThanSnappingBack()
        {
            var from = PlayerLook.Of(2);
            var promotion = Promotion.Settled(from).Toward(PlayerLook.Of(9));

            Assert.That(promotion.Scale, Is.EqualTo(from.Scale).Within(1e-6f));
            Assert.That(promotion.Tint, Is.EqualTo(from.Tint));
        }

        [Test]
        public void ASecondPromotionCollapsesTheRunningOneRatherThanQueueingBehindIt()
        {
            var promotion = Promotion.Settled(PlayerLook.Of(2))
                .Toward(PlayerLook.Of(9))
                .Advanced(Promotion.Seconds * 0.4f);

            var midway = promotion.Scale;
            var collapsed = promotion.Toward(PlayerLook.Of(400));

            Assert.That(collapsed.Scale, Is.EqualTo(midway).Within(1e-6f));
            Assert.That(Run(collapsed).Scale, Is.EqualTo(PlayerLook.Of(400).Scale).Within(1e-6f));
        }

        [Test]
        public void RetargetingToTheLookAlreadyBeingWornChangesNothing()
        {
            var running = Promotion.Settled(PlayerLook.Of(2)).Toward(PlayerLook.Of(9)).Advanced(Frame);

            Assert.That(running.Toward(PlayerLook.Of(9)), Is.EqualTo(running));
        }

        [Test]
        public void TheColourNeverLeavesTheRampEvenWhileTheScaleOvershoots()
        {
            var promotion = Promotion.Settled(PlayerLook.Of(2)).Toward(PlayerLook.Of(400));

            while (!promotion.IsSettled)
            {
                promotion = promotion.Advanced(Frame);
                Assert.That(promotion.Tint.Red, Is.InRange(0f, 1f));
                Assert.That(promotion.Tint.Green, Is.InRange(0f, 1f));
                Assert.That(promotion.Tint.Blue, Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void APromotionSettlesInsideItsOwnDuration()
        {
            var promotion = Promotion.Settled(PlayerLook.Of(2))
                .Toward(PlayerLook.Of(400))
                .Advanced(Promotion.Seconds);

            Assert.That(promotion.IsSettled, Is.True);
            Assert.That(promotion.Scale, Is.EqualTo(PlayerLook.Of(400).Scale).Within(1e-6f));
        }

        static Promotion Run(Promotion promotion)
        {
            for (var step = 0; step < 1000 && !promotion.IsSettled; step++)
            {
                promotion = promotion.Advanced(Frame);
            }

            return promotion;
        }
    }
}
