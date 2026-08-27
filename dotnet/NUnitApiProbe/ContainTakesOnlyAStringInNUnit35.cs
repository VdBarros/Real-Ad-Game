using NUnit.Framework;

namespace Game.NUnitApiProbe
{
    public static class ContainTakesOnlyAStringInNUnit35
    {
        public static void AssertOnANonStringMember()
        {
            Assert.That(new[] { 1, 2 }, Does.Not.Contain(3));
        }
    }
}
