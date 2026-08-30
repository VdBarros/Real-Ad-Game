using System;
using System.Collections.Generic;
using System.Linq;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class CastClipTests
    {
        static readonly PartModel[] Skeletons =
        {
            PartModel.SkeletonMinion,
            PartModel.SkeletonRogue,
            PartModel.SkeletonWarrior,
            PartModel.SkeletonMage
        };

        [Test]
        public void EveryActNamesOneClipInEveryCastPack()
        {
            foreach (var pack in new[] { ArtPack.Adventurers, ArtPack.Skeletons })
            {
                var table = CastClips.TableOf(pack);

                Assert.That(table.Count, Is.EqualTo(FigureActs.Count), pack.ToString());
                Assert.That(table.Names.Distinct().Count(), Is.EqualTo(FigureActs.Count), pack.ToString());

                foreach (var act in FigureActs.All)
                {
                    var clip = table.NameOf(act);

                    Assert.That(clip, Is.Not.Null.And.Not.Empty, act.ToString());
                    Assert.That(table.Wants(clip), Is.True, act.ToString());
                    Assert.That(table.ActOf(clip), Is.EqualTo(act));
                    Assert.That(table.LoopsOf(clip), Is.EqualTo(FigureActs.Loops(act)), act.ToString());
                }
            }
        }

        [Test]
        public void TheAdventurersPlayThePacksOwnTakeNames()
        {
            Assert.That(AdventurerClips.Idle, Is.EqualTo("Idle_A"));
            Assert.That(AdventurerClips.Walk, Is.EqualTo("Walking_A"));
            Assert.That(AdventurerClips.Retreat, Is.EqualTo("Walking_Backwards"));
            Assert.That(AdventurerClips.Strike, Is.EqualTo("Melee_1H_Attack_Chop"));
            Assert.That(AdventurerClips.Clash, Is.EqualTo("Melee_Block_Hit"));
            Assert.That(AdventurerClips.Recoil, Is.EqualTo("Hit_A"));
            Assert.That(AdventurerClips.Take, Is.EqualTo("PickUp"));
            Assert.That(AdventurerClips.Kick, Is.EqualTo("Melee_Unarmed_Attack_Kick"));
            Assert.That(AdventurerClips.Slice, Is.EqualTo("Melee_1H_Attack_Slice_Diagonal"));
            Assert.That(AdventurerClips.Cleave, Is.EqualTo("Melee_2H_Attack_Chop"));
            Assert.That(AdventurerClips.Thrust, Is.EqualTo("Melee_2H_Attack_Stab"));
            Assert.That(AdventurerClips.Sweep, Is.EqualTo("Melee_2H_Attack_Spin"));
            Assert.That(AdventurerClips.Fall, Is.EqualTo("Death_A"));
        }

        [Test]
        public void TheSkeletonsKeepTheTakesBakedIntoTheirOwnMeshes()
        {
            Assert.That(SkeletonClips.Idle, Is.EqualTo("Idle"));
            Assert.That(SkeletonClips.Walk, Is.EqualTo("Walking_A"));
            Assert.That(SkeletonClips.Retreat, Is.EqualTo("Walking_Backwards"));
            Assert.That(SkeletonClips.Strike, Is.EqualTo("1H_Melee_Attack_Chop"));
            Assert.That(SkeletonClips.Clash, Is.EqualTo("Block_Hit"));
            Assert.That(SkeletonClips.Recoil, Is.EqualTo("Hit_A"));
            Assert.That(SkeletonClips.Take, Is.EqualTo("PickUp"));
            Assert.That(SkeletonClips.Kick, Is.EqualTo("Unarmed_Melee_Attack_Kick"));
            Assert.That(SkeletonClips.Slice, Is.EqualTo("1H_Melee_Attack_Slice_Diagonal"));
            Assert.That(SkeletonClips.Cleave, Is.EqualTo("2H_Melee_Attack_Chop"));
            Assert.That(SkeletonClips.Thrust, Is.EqualTo("2H_Melee_Attack_Stab"));
            Assert.That(SkeletonClips.Sweep, Is.EqualTo("2H_Melee_Attack_Spin"));
            Assert.That(SkeletonClips.Fall, Is.EqualTo("Death_A"));
        }

        [Test]
        public void EightOfTheThirteenActsAreNamedDifferentlyByTheTwoPacks()
        {
            var reordered = new List<FigureAct>();

            foreach (var act in FigureActs.All)
            {
                if (!string.Equals(
                    AdventurerClips.NameOf(act), SkeletonClips.NameOf(act), StringComparison.Ordinal))
                {
                    reordered.Add(act);
                }
            }

            Assert.That(reordered.Count, Is.EqualTo(8));
        }

        [Test]
        public void TheMeshAMemberOfTheCastWearsPicksItsClipTable()
        {
            Assert.That(CastClips.TableFor(PartModel.Knight), Is.SameAs(AdventurerClips.Table));

            foreach (var skeleton in Skeletons)
            {
                Assert.That(CastClips.TableFor(skeleton), Is.SameAs(SkeletonClips.Table));
                Assert.That(
                    CastClips.NameOf(skeleton, FigureAct.Idle), Is.EqualTo(SkeletonClips.Idle));
            }

            Assert.That(CastClips.NameOf(PartModel.Knight, FigureAct.Idle), Is.EqualTo(AdventurerClips.Idle));
        }

        [Test]
        public void NoPackOutsideTheCastNamesAClip()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CastClips.TableOf(ArtPack.Dungeon));
            Assert.Throws<ArgumentOutOfRangeException>(() => CastClips.TableFor(PartModel.Pillar));
        }

        [Test]
        public void OnlyTheStandingAndTravellingActsLoopWhicheverPackPlaysThem()
        {
            foreach (var act in FigureActs.All)
            {
                var looping = act == FigureAct.Idle || act == FigureAct.Walk || act == FigureAct.Retreat;

                Assert.That(FigureActs.Loops(act), Is.EqualTo(looping), act.ToString());
                Assert.That(AdventurerClips.Loops(act), Is.EqualTo(looping), act.ToString());
                Assert.That(SkeletonClips.Loops(act), Is.EqualTo(looping), act.ToString());
            }
        }

        [Test]
        public void NoActOutsideTheEnumNamesAClip()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FigureActs.Loops((FigureAct)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => SkeletonClips.NameOf((FigureAct)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => AnimationSets.SetOf((FigureAct)(-1)));
        }

        [Test]
        public void AClipTableNamesOneClipForEveryAct()
        {
            Assert.Throws<ArgumentException>(() => new ClipTable(null));
            Assert.Throws<ArgumentException>(() => new ClipTable(new[] { "Idle_A" }));
        }

        [Test]
        public void EveryActIsCarriedByOneOfTheFourSetsCopiedIntoResources()
        {
            var carried = new List<string>();

            foreach (var act in FigureActs.All)
            {
                var set = AnimationSets.SetOf(act);

                Assert.That(AnimationSets.Carries(set), Is.True, act.ToString());
                carried.Add(AdventurerClips.NameOf(act));
            }

            Assert.That(AnimationSets.Count, Is.EqualTo(4));
            Assert.That(AnimationSets.Assets.Distinct().Count(), Is.EqualTo(4));

            Assert.That(AnimationSets.SetOf(FigureAct.Idle), Is.EqualTo(AnimationSets.General));
            Assert.That(AnimationSets.SetOf(FigureAct.Recoil), Is.EqualTo(AnimationSets.General));
            Assert.That(AnimationSets.SetOf(FigureAct.Take), Is.EqualTo(AnimationSets.General));
            Assert.That(AnimationSets.SetOf(FigureAct.Fall), Is.EqualTo(AnimationSets.General));
            Assert.That(AnimationSets.SetOf(FigureAct.Walk), Is.EqualTo(AnimationSets.MovementBasic));
            Assert.That(AnimationSets.SetOf(FigureAct.Retreat), Is.EqualTo(AnimationSets.MovementAdvanced));

            Assert.That(AnimationSets.ActsOf(AnimationSets.General).Count, Is.EqualTo(4));
            Assert.That(AnimationSets.ActsOf(AnimationSets.MovementBasic).Count, Is.EqualTo(1));
            Assert.That(AnimationSets.ActsOf(AnimationSets.MovementAdvanced).Count, Is.EqualTo(1));
            Assert.That(AnimationSets.ActsOf(AnimationSets.CombatMelee).Count, Is.EqualTo(7));
            Assert.That(carried.Count, Is.EqualTo(13));
        }

        [Test]
        public void ASetsCurvesAreReboundFromItsOwnRigOntoTheOneAnAdventurerOffers()
        {
            Assert.That(AdventurerPack.RigNode, Is.EqualTo("Rig"));
            Assert.That(AnimationSets.RigNode, Is.EqualTo("Rig_Medium"));

            Assert.That(
                AnimationSets.Rebound("Rig_Medium/root/hips/spine/chest/upperarm.r"),
                Is.EqualTo("Rig/root/hips/spine/chest/upperarm.r"));
            Assert.That(AnimationSets.Rebound("Rig_Medium"), Is.EqualTo("Rig"));
            Assert.That(AnimationSets.Rebound("Rig_Medium/root"), Is.EqualTo("Rig/root"));
        }

        [Test]
        public void ReboundLeavesEveryPathThatDoesNotOpenOnTheSetsRigAlone()
        {
            Assert.That(AnimationSets.Rebound(string.Empty), Is.EqualTo(string.Empty));
            Assert.That(AnimationSets.Rebound(null), Is.Null);
            Assert.That(AnimationSets.Rebound("Rig/root/hips"), Is.EqualTo("Rig/root/hips"));
            Assert.That(AnimationSets.Rebound("Rig_Medium_Extra/root"), Is.EqualTo("Rig_Medium_Extra/root"));
            Assert.That(AnimationSets.Rebound("Knight_Cape"), Is.EqualTo("Knight_Cape"));
        }
    }
}
