using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class LevelGraphSerializationTests
    {
        [Test]
        public void AGraphSurvivesBeingWrittenAndReadBack()
        {
            var graph = LevelGraphFixture.TwoFloors();

            var restored = LevelGraphReader.Read(LevelGraphWriter.Write(graph));

            Assert.That(restored, Is.EqualTo(graph));
        }

        [Test]
        public void WritingTheSameGraphTwiceProducesIdenticalBytes()
        {
            var graph = LevelGraphFixture.TwoFloors();

            Assert.That(LevelGraphWriter.WriteBytes(graph), Is.EqualTo(LevelGraphWriter.WriteBytes(graph)));
        }

        [Test]
        public void RebuildingTheGraphProducesIdenticalBytes()
        {
            Assert.That(
                LevelGraphWriter.WriteBytes(LevelGraphFixture.TwoFloors()),
                Is.EqualTo(LevelGraphWriter.WriteBytes(LevelGraphFixture.TwoFloors())));
        }

        [Test]
        public void AssemblingTheGraphInTheOppositeOrderProducesIdenticalBytes()
        {
            Assert.That(
                LevelGraphWriter.WriteBytes(LevelGraphFixture.TwoFloorsAssembledBackwards()),
                Is.EqualTo(LevelGraphWriter.WriteBytes(LevelGraphFixture.TwoFloors())));
        }

        [Test]
        public void WritingWhatWasReadProducesIdenticalBytes()
        {
            var written = LevelGraphWriter.Write(LevelGraphFixture.TwoFloors());

            var rewritten = LevelGraphWriter.Write(LevelGraphReader.Read(written));

            Assert.That(rewritten, Is.EqualTo(written));
        }

        [Test]
        public void TheWrittenDocumentUsesLineFeedsAndNoByteOrderMark()
        {
            var bytes = LevelGraphWriter.WriteBytes(LevelGraphFixture.TwoFloors());
            var text = LevelGraphWriter.Write(LevelGraphFixture.TwoFloors());

            Assert.That(bytes[0], Is.EqualTo((byte)'{'));
            Assert.That(text, Does.Not.Contain("\r"));
        }
    }
}
