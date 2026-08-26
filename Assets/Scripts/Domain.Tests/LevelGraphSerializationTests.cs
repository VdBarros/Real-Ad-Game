using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class LevelGraphSerializationTests
    {
        [Test]
        public void AGraphSurvivesBeingWrittenAndReadBack()
        {
            var graph = LevelGraphFixture.TwoTerraces();

            var restored = LevelGraphReader.Read(LevelGraphWriter.Write(graph));

            Assert.That(restored, Is.EqualTo(graph));
        }

        [Test]
        public void WritingTheSameGraphTwiceProducesIdenticalBytes()
        {
            var graph = LevelGraphFixture.TwoTerraces();

            Assert.That(LevelGraphWriter.WriteBytes(graph), Is.EqualTo(LevelGraphWriter.WriteBytes(graph)));
        }

        [Test]
        public void RebuildingTheGraphProducesIdenticalBytes()
        {
            Assert.That(
                LevelGraphWriter.WriteBytes(LevelGraphFixture.TwoTerraces()),
                Is.EqualTo(LevelGraphWriter.WriteBytes(LevelGraphFixture.TwoTerraces())));
        }

        [Test]
        public void AssemblingTheGraphInTheOppositeOrderProducesIdenticalBytes()
        {
            Assert.That(
                LevelGraphWriter.WriteBytes(LevelGraphFixture.TwoTerracesAssembledBackwards()),
                Is.EqualTo(LevelGraphWriter.WriteBytes(LevelGraphFixture.TwoTerraces())));
        }

        [Test]
        public void WritingWhatWasReadProducesIdenticalBytes()
        {
            var written = LevelGraphWriter.Write(LevelGraphFixture.TwoTerraces());

            var rewritten = LevelGraphWriter.Write(LevelGraphReader.Read(written));

            Assert.That(rewritten, Is.EqualTo(written));
        }

        [Test]
        public void TheWrittenDocumentUsesLineFeedsAndNoByteOrderMark()
        {
            var bytes = LevelGraphWriter.WriteBytes(LevelGraphFixture.TwoTerraces());
            var text = LevelGraphWriter.Write(LevelGraphFixture.TwoTerraces());

            Assert.That(bytes[0], Is.EqualTo((byte)'{'));
            Assert.That(text, Does.Not.Contain("\r"));
        }
    }
}
