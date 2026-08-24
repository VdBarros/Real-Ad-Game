using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class SmokeTest
    {
        [Test]
        public void DomainTestLoopIsWired()
        {
            Assert.That(1 + 1, Is.EqualTo(2));
        }
    }
}
