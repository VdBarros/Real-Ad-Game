using System;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class OpeningPlanFreezeTests
    {
        const int FrozenSeeds = 200;

        const long FrozenFingerprint = 7573283008921576682L;

        [Test]
        public void TheOpeningPlanMintsTheLevelsItAlwaysMinted()
        {
            var fingerprint = Fingerprint(LevelPlan.For(1), FrozenSeeds);

            Console.WriteLine("opening plan fingerprint over " + FrozenSeeds + " seeds: " + fingerprint);

            Assert.That(fingerprint, Is.EqualTo(FrozenFingerprint));
        }

        static long Fingerprint(LevelPlan plan, int seeds)
        {
            unchecked
            {
                var hash = 1469598103934665603L;

                for (var seed = 1; seed <= seeds; seed++)
                {
                    LevelGenerationReport report;
                    var level = LevelGenerator.Generate(seed, plan.Preset, plan.Recipe, plan.Tuning, out report);

                    hash = Folded(hash, report.Attempts);
                    foreach (var node in level.Graph.Decisions.Nodes)
                    {
                        hash = Folded(hash, node.Id);
                        hash = Folded(hash, (int)node.Type);
                        hash = Folded(hash, node.Value);
                        hash = Folded(hash, node.Position.Elevation);
                        hash = Folded(hash, node.Position.X);
                        hash = Folded(hash, node.Position.Y);
                    }
                }

                return hash;
            }
        }

        static long Folded(long hash, int value)
        {
            unchecked
            {
                return (hash ^ value) * 1099511628211L;
            }
        }
    }
}
