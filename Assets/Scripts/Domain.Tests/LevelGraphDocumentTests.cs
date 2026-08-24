using System;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class LevelGraphDocumentTests
    {
        static readonly string[] Golden =
        {
            "{",
            "  \"version\": 1,",
            "  \"seed\": 20250824,",
            "  \"preset\": \"tiny\",",
            "  \"tiles\": [",
            "    { \"floor\": 0, \"x\": 1, \"y\": 0, \"region\": 0 },",
            "    { \"floor\": 0, \"x\": 2, \"y\": 0, \"region\": 0 },",
            "    { \"floor\": 0, \"x\": 3, \"y\": 0, \"region\": 0 },",
            "    { \"floor\": 0, \"x\": 4, \"y\": 0, \"region\": 0 },",
            "    { \"floor\": 0, \"x\": 5, \"y\": 0, \"region\": 0 },",
            "    { \"floor\": 0, \"x\": 1, \"y\": 1, \"region\": 0 },",
            "    { \"floor\": 0, \"x\": 5, \"y\": 1, \"region\": 0 },",
            "    { \"floor\": 0, \"x\": 1, \"y\": 2, \"region\": 1 },",
            "    { \"floor\": 0, \"x\": 2, \"y\": 2, \"region\": 1 },",
            "    { \"floor\": 0, \"x\": 3, \"y\": 2, \"region\": 1 },",
            "    { \"floor\": 0, \"x\": 4, \"y\": 2, \"region\": 1 },",
            "    { \"floor\": 0, \"x\": 5, \"y\": 2, \"region\": 1 },",
            "    { \"floor\": 1, \"x\": 5, \"y\": 0, \"region\": 2 },",
            "    { \"floor\": 1, \"x\": 6, \"y\": 0, \"region\": 2 },",
            "    { \"floor\": 1, \"x\": 6, \"y\": 1, \"region\": 2 }",
            "  ],",
            "  \"stairs\": [",
            "    { \"floor\": 0, \"x\": 5, \"y\": 0 }",
            "  ],",
            "  \"nodes\": [",
            "    { \"id\": 0, \"floor\": 0, \"x\": 1, \"y\": 0, \"type\": \"Start\", \"value\": 1 },",
            "    { \"id\": 1, \"floor\": 0, \"x\": 5, \"y\": 0, \"type\": \"Empty\", \"value\": 0 },",
            "    { \"id\": 2, \"floor\": 0, \"x\": 1, \"y\": 2, \"type\": \"Enemy\", \"value\": 4 },",
            "    { \"id\": 3, \"floor\": 0, \"x\": 5, \"y\": 2, \"type\": \"Additive\", \"value\": 12 },",
            "    { \"id\": 4, \"floor\": 1, \"x\": 5, \"y\": 0, \"type\": \"Empty\", \"value\": 0 },",
            "    { \"id\": 5, \"floor\": 1, \"x\": 6, \"y\": 0, \"type\": \"Multiplier\", \"value\": 3 },",
            "    { \"id\": 6, \"floor\": 1, \"x\": 6, \"y\": 1, \"type\": \"Boss\", \"value\": 30 }",
            "  ],",
            "  \"corridors\": [",
            "    { \"low\": 0, \"high\": 1, \"tiles\": [ { \"floor\": 0, \"x\": 2, \"y\": 0 }, { \"floor\": 0, \"x\": 3, \"y\": 0 }, { \"floor\": 0, \"x\": 4, \"y\": 0 } ] },",
            "    { \"low\": 0, \"high\": 2, \"tiles\": [ { \"floor\": 0, \"x\": 1, \"y\": 1 } ] },",
            "    { \"low\": 1, \"high\": 3, \"tiles\": [ { \"floor\": 0, \"x\": 5, \"y\": 1 } ] },",
            "    { \"low\": 1, \"high\": 4, \"tiles\": [] },",
            "    { \"low\": 2, \"high\": 3, \"tiles\": [ { \"floor\": 0, \"x\": 2, \"y\": 2 }, { \"floor\": 0, \"x\": 3, \"y\": 2 }, { \"floor\": 0, \"x\": 4, \"y\": 2 } ] },",
            "    { \"low\": 4, \"high\": 5, \"tiles\": [] },",
            "    { \"low\": 5, \"high\": 6, \"tiles\": [] }",
            "  ]",
            "}",
            ""
        };

        [Test]
        public void TheWrittenDocumentMatchesTheAgreedFormat()
        {
            Assert.That(
                LevelGraphWriter.Write(LevelGraphFixture.TwoFloors()),
                Is.EqualTo(string.Join("\n", Golden)));
        }

        [Test]
        public void TheAgreedFormatReadsBackIntoTheSameGraph()
        {
            Assert.That(
                LevelGraphReader.Read(string.Join("\n", Golden)),
                Is.EqualTo(LevelGraphFixture.TwoFloors()));
        }

        [Test]
        public void AFormatVersionThisReaderDoesNotSpeakIsRefused()
        {
            Assert.That(
                () => LevelGraphReader.Read(GoldenWith("  \"version\": 1,", "  \"version\": 2,")),
                Throws.InstanceOf<FormatException>());
        }

        [Test]
        public void AMemberOutOfOrderIsRefused()
        {
            Assert.That(
                () => LevelGraphReader.Read(GoldenWith("  \"seed\": 20250824,", "  \"preset\": 20250824,")),
                Throws.InstanceOf<FormatException>());
        }

        [Test]
        public void ANodeTypeThisReaderDoesNotKnowIsRefused()
        {
            Assert.That(
                () => LevelGraphReader.Read(GoldenWith("\"type\": \"Enemy\"", "\"type\": \"Elite\"")),
                Throws.InstanceOf<FormatException>());
        }

        [Test]
        public void NodesListedOutOfSweepOrderAreRefused()
        {
            var swapped = GoldenWith(
                "    { \"id\": 2, \"floor\": 0, \"x\": 1, \"y\": 2, \"type\": \"Enemy\", \"value\": 4 },",
                "    { \"id\": 2, \"floor\": 0, \"x\": 5, \"y\": 2, \"type\": \"Enemy\", \"value\": 4 },");
            swapped = Replace(
                swapped,
                "    { \"id\": 3, \"floor\": 0, \"x\": 5, \"y\": 2, \"type\": \"Additive\", \"value\": 12 },",
                "    { \"id\": 3, \"floor\": 0, \"x\": 1, \"y\": 2, \"type\": \"Additive\", \"value\": 12 },");

            Assert.That(
                () => LevelGraphReader.Read(swapped),
                Throws.InstanceOf<FormatException>().With.Message.Contains("sweep"));
        }

        [Test]
        public void ANodeIdThatSkipsAheadIsRefused()
        {
            Assert.That(
                () => LevelGraphReader.Read(GoldenWith("{ \"id\": 2, \"floor\": 0", "{ \"id\": 5, \"floor\": 0")),
                Throws.InstanceOf<FormatException>());
        }

        [Test]
        public void AnythingAfterTheDocumentIsRefused()
        {
            Assert.That(
                () => LevelGraphReader.Read(string.Join("\n", Golden) + "{}"),
                Throws.InstanceOf<FormatException>());
        }

        static string GoldenWith(string original, string replacement)
        {
            return Replace(string.Join("\n", Golden), original, replacement);
        }

        static string Replace(string document, string original, string replacement)
        {
            Assert.That(document, Does.Contain(original), "The golden document no longer holds the line under test.");
            return document.Replace(original, replacement);
        }
    }
}
