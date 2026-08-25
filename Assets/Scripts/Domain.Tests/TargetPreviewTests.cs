using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class TargetPreviewTests
    {
        [Test]
        public void NothingAimedAtPreviewsNothing()
        {
            Assert.That(TargetPreview.Of(RunFixture.Begin(2), TapAim.Nothing), Is.SameAs(TargetPreview.None));
            Assert.That(TargetPreview.None.IsAimed, Is.False);
            Assert.That(TargetPreview.None.IsLegal, Is.False);
        }

        [Test]
        public void APickupPreviewsTheWalkAndThePowerItLeavesBehind()
        {
            var preview = TargetPreview.Of(RunFixture.Begin(2), RunFixture.Additive);

            Assert.That(preview.Outcome, Is.EqualTo(ActionOutcome.Walked));
            Assert.That(preview.Power, Is.EqualTo(2 + RunFixture.AdditiveValue));
            Assert.That(preview.IsLegal, Is.True);
        }

        [Test]
        public void AnEnemyWithinReachPreviewsTheWinAndTheSpoils()
        {
            var preview = TargetPreview.Of(RunFixture.Begin(3), RunFixture.DoorstepEnemy);

            Assert.That(preview.Outcome, Is.EqualTo(ActionOutcome.Win));
            Assert.That(preview.Power, Is.EqualTo(3 + RunFixture.DoorstepEnemyValue));
        }

        [Test]
        public void AnEnemyMatchedExactlyPreviewsATieAndNotAWin()
        {
            var preview = TargetPreview.Of(
                RunFixture.Begin(RunFixture.DoorstepEnemyValue), RunFixture.DoorstepEnemy);

            Assert.That(preview.Outcome, Is.EqualTo(ActionOutcome.Tie));
        }

        [Test]
        public void AnEnemyOutOfReachPreviewsALoss()
        {
            var preview = TargetPreview.Of(
                RunFixture.Begin(RunFixture.DoorstepEnemyValue - 1), RunFixture.DoorstepEnemy);

            Assert.That(preview.Outcome, Is.EqualTo(ActionOutcome.Loss));
        }

        [Test]
        public void AMultiHopTargetPreviewsThePowerItArrivesWithHavingEatenEverythingOnTheWay()
        {
            var preview = TargetPreview.Of(RunFixture.Begin(2), RunFixture.GateEnemy);

            Assert.That(
                preview.Route,
                Is.EqualTo(new[] { RunFixture.Start, RunFixture.Additive, RunFixture.GateEnemy }));
            Assert.That(preview.Outcome, Is.EqualTo(ActionOutcome.Win));
            Assert.That(
                preview.Power,
                Is.EqualTo(2 + RunFixture.AdditiveValue + RunFixture.GateEnemyValue));
        }

        [Test]
        public void ANodeBehindAnEnemyTooStrongToPassIsNotEvenAimedAt()
        {
            var state = RunFixture.Begin(1);

            Assert.That(state.IsReachable(RunFixture.Boss), Is.False);
            Assert.That(TapAim.Aimable(state), Has.No.Member(RunFixture.Boss));
            Assert.That(TargetPreview.Of(state, RunFixture.Boss).IsLegal, Is.False);
            Assert.That(TargetPreview.Of(state, RunFixture.Boss).Outcome, Is.EqualTo(ActionOutcome.Rejected));
        }

        [Test]
        public void ThePlayerIsNotATargetToWalkTo()
        {
            Assert.That(TapAim.Aimable(RunFixture.Begin(2)), Has.No.Member(RunFixture.Start));
        }

        [Test]
        public void ASpentPickupIsNotATargetBecauseThereIsNothingLeftToTake()
        {
            var state = RunFixture.Begin(2);

            Assert.That(TapAim.Aimable(state), Has.Member(RunFixture.Multiplier));

            var taken = ActionResolver.Resolve(state, RunFixture.Multiplier).State;
            taken = ActionResolver.Resolve(taken, RunFixture.Start).State;

            Assert.That(taken.IsConsumed(RunFixture.Multiplier), Is.True);
            Assert.That(taken.IsReachable(RunFixture.Multiplier), Is.True);
            Assert.That(TapAim.Aimable(taken), Has.No.Member(RunFixture.Multiplier));
            Assert.That(
                TapAim.Aimable(taken),
                Has.Member(RunFixture.AdditiveBeyondTheMultiplier),
                "The walk still routes over the spent pedestal to reach what lies past it.");
        }

        [Test]
        public void ADefeatedEnemyIsNotATargetEither()
        {
            var state = ActionResolver
                .Resolve(RunFixture.Begin(RunFixture.DoorstepEnemyValue + 1), RunFixture.DoorstepEnemy)
                .State;

            Assert.That(state.IsConsumed(RunFixture.DoorstepEnemy), Is.True);
            Assert.That(TapAim.Aimable(state), Has.No.Member(RunFixture.DoorstepEnemy));
        }

        [Test]
        public void OnlyDrawnNodesAreAimedAtBecauseATapLandsOnWhatItCanSee()
        {
            var state = RunFixture.Begin(2);

            foreach (var nodeId in TapAim.Aimable(state))
            {
                WorldPart prop;
                Assert.That(
                    LevelBlueprintBuilder.TryProp(state.Level.Decisions.Node(nodeId), out prop),
                    Is.True,
                    "Node " + nodeId + " is aimable but the world builder raises nothing to aim at.");
            }
        }

        [Test]
        public void ThePreviewMatchesAnIndependentWalkOfTheRulesOnEveryReachableTargetOfEveryGeneratedLevel()
        {
            var multiHop = 0;
            var previewed = 0;

            foreach (var preset in new[] { MazePreset.Tiny, MazePreset.Ship, MazePreset.Stress })
            {
                for (var seed = 1L; seed <= 40L; seed++)
                {
                    var graph = LevelGenerator.Generate(seed, preset).Graph;
                    var state = RunState.Begin(graph, PowerTuning.For(preset).StartingPower);

                    for (var move = 0; move < 6 && !state.IsLevelComplete; move++)
                    {
                        var stepped = false;

                        foreach (var nodeId in TapAim.Aimable(state))
                        {
                            var preview = TargetPreview.Of(state, nodeId);
                            int power;
                            var outcome = WalkedByTheRules(state, preview.Route, out power);

                            Assert.That(
                                preview.Outcome,
                                Is.EqualTo(outcome),
                                "Seed " + seed + " of " + preset + " previews " + preview.Outcome
                                + " on node " + nodeId + " where the rules give " + outcome + ".");
                            Assert.That(
                                preview.Power,
                                Is.EqualTo(power),
                                "Seed " + seed + " of " + preset + " previews power " + preview.Power
                                + " on node " + nodeId + " where the rules give " + power + ".");

                            previewed++;
                            if (preview.Route.Count > 2)
                            {
                                multiHop++;
                            }

                            if (!stepped && preview.Outcome == ActionOutcome.Win)
                            {
                                state = ActionResolver.Resolve(state, nodeId).State;
                                stepped = true;
                            }
                        }

                        if (!stepped)
                        {
                            break;
                        }
                    }
                }
            }

            Console.WriteLine(
                "previews checked against the rules: " + previewed + ", of which " + multiHop + " multi-hop");

            Assert.That(multiHop, Is.GreaterThan(1000), "The sweep never previewed enough multi-hop targets.");
        }

        [Test]
        public void ThePreviewAsksTheResolverRatherThanRestatingTheRules()
        {
            var source = SourceTree.Read("Presentation.Pure", "TargetPreview.cs");

            Assert.That(
                source,
                Does.Contain("ActionResolver.Resolve"),
                "The preview has to be the resolver, or it is a second reading of the rules waiting to drift.");

            foreach (var restatement in new[]
            {
                "NodeType.", "ActionOutcome.Win", "ActionOutcome.Tie", "ActionOutcome.Loss"
            })
            {
                Assert.That(
                    source,
                    Does.Not.Contain(restatement),
                    "TargetPreview.cs names " + restatement + ", so it decides an outcome the resolver owns.");
            }
        }

        static ActionOutcome WalkedByTheRules(RunState state, IReadOnlyList<int> route, out int power)
        {
            power = state.Power;
            if (route.Count == 0)
            {
                return ActionOutcome.Rejected;
            }

            var consumed = new HashSet<int>(state.ConsumedNodes);
            var outcome = ActionOutcome.Walked;

            for (var step = 1; step < route.Count; step++)
            {
                var node = state.Level.Decisions.Node(route[step]);
                if (consumed.Contains(node.Id))
                {
                    continue;
                }

                if (node.Type == NodeType.Additive)
                {
                    power += node.Value;
                    consumed.Add(node.Id);
                }
                else if (node.Type == NodeType.Multiplier)
                {
                    power *= node.Value;
                    consumed.Add(node.Id);
                }
                else if (node.Type == NodeType.Enemy || node.Type == NodeType.Boss)
                {
                    if (power < node.Value)
                    {
                        return ActionOutcome.Loss;
                    }

                    if (power == node.Value)
                    {
                        return ActionOutcome.Tie;
                    }

                    power += node.Value;
                    consumed.Add(node.Id);
                    outcome = ActionOutcome.Win;
                }
            }

            return outcome;
        }
    }
}
