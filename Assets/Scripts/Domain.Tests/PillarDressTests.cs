using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class PillarDressTests
    {
        const float Frame = 1f / 60f;

        const float Tolerance = 0.0001f;

        const float Stride = 0.25f;

        static PillarReel At(float seconds)
        {
            return seconds <= 0f ? PillarReel.Opening : PillarReel.Opening.Advanced(seconds);
        }

        static bool Crossed(PillarReel from, PillarReel to, PillarRole role)
        {
            var here = PillarDress.MarkOf(from, role).Position;
            var there = PillarDress.MarkOf(to, role).Position;

            return Math.Abs(here.X - there.X) > Tolerance || Math.Abs(here.Z - there.Z) > Tolerance;
        }

        [Test]
        public void EveryLookOnThePillarsWearsARiggedPackMesh()
        {
            foreach (CastLook look in Enum.GetValues(typeof(CastLook)))
            {
                var mesh = PillarDress.MeshOf(look);

                Assert.That(mesh, Is.Not.EqualTo(PartModel.None), look + " wears no mesh at all.");
                Assert.That(
                    ArtPacks.IsRiggedCharacter(mesh),
                    Is.True,
                    look + " wears " + mesh + ", which no cast pack carries.");
            }
        }

        [Test]
        public void NoTwoLooksShareASilhouette()
        {
            var worn = new List<PartModel>();

            foreach (CastLook look in Enum.GetValues(typeof(CastLook)))
            {
                var mesh = PillarDress.MeshOf(look);

                Assert.That(worn, Has.No.Member(mesh), mesh + " dresses two looks at once.");
                worn.Add(mesh);
            }
        }

        [Test]
        public void EveryLookIsDressedByAStyleThatDrawsOffItsOwnPack()
        {
            foreach (CastLook look in Enum.GetValues(typeof(CastLook)))
            {
                var mesh = PillarDress.MeshOf(look);
                var style = PillarDress.StyleOf(look);

                Assert.That(
                    CharacterCast.IsRole(style),
                    Is.True,
                    look + " is dressed by " + style + ", which is not a member of the cast.");
                Assert.That(
                    ArtPacks.Of(PartModels.Of(style)),
                    Is.EqualTo(ArtPacks.Of(mesh)),
                    look + " wears a " + ArtPacks.Of(mesh) + " mesh under a " + style
                        + " material, so it would be textured off the wrong atlas.");
            }
        }

        [Test]
        public void EveryRoleRaisesExactlyTheLooksTheReelEverAsksItToWear()
        {
            var asked = new Dictionary<PillarRole, List<CastLook>>();

            foreach (var role in PillarDress.Roles)
            {
                asked[role] = new List<CastLook>();
            }

            var reel = PillarReel.Opening;

            while (true)
            {
                foreach (var role in PillarDress.Roles)
                {
                    var look = PillarDress.MarkOf(reel, role).Look;

                    if (!asked[role].Contains(look))
                    {
                        asked[role].Add(look);
                    }
                }

                if (reel.IsOver)
                {
                    break;
                }

                reel = reel.Advanced(Frame);
            }

            foreach (var role in PillarDress.Roles)
            {
                var raised = PillarDress.LooksOf(role);

                Assert.That(
                    raised.Count,
                    Is.EqualTo(asked[role].Count),
                    role + " raises " + raised.Count + " looks where the reel asks for " + asked[role].Count + ".");

                foreach (var look in asked[role])
                {
                    Assert.That(
                        raised,
                        Has.Member(look),
                        role + " is asked to wear " + look + " but never raises it.");
                }
            }
        }

        [Test]
        public void AFigureThatCrossesTheGroundIsWalkingWhileItDoes()
        {
            var reel = PillarReel.Opening;

            while (!reel.IsOver)
            {
                var next = reel.Advanced(Frame);

                foreach (var role in PillarDress.Roles)
                {
                    if (!Crossed(reel, next, role))
                    {
                        continue;
                    }

                    var act = PillarDress.CueOf(role, reel.Elapsed).Act;

                    Assert.That(
                        act,
                        Is.EqualTo(FigureAct.Walk),
                        role + " slides across the ground at " + reel.Elapsed + "s while playing " + act + ".");
                }

                reel = next;
            }
        }

        [Test]
        public void NobodyWalksWithoutGoingAnywhere()
        {
            var reel = PillarReel.Opening;

            while (!reel.IsOver)
            {
                foreach (var role in PillarDress.Roles)
                {
                    if (PillarDress.CueOf(role, reel.Elapsed).Act != FigureAct.Walk)
                    {
                        continue;
                    }

                    var before = At(reel.Elapsed - Stride);
                    var after = At(reel.Elapsed + Stride);

                    Assert.That(
                        Crossed(before, after, role),
                        Is.True,
                        role + " walks on the spot at " + reel.Elapsed + "s.");
                }

                reel = reel.Advanced(Frame);
            }
        }

        [Test]
        public void TheRivalStandsStillAndIdlesForTheWholeReel()
        {
            var reel = PillarReel.Opening;
            var opening = reel.Rival.Position;

            while (!reel.IsOver)
            {
                Assert.That(
                    PillarDress.CueOf(PillarRole.Rival, reel.Elapsed).Act,
                    Is.EqualTo(FigureAct.Idle));
                Assert.That(reel.Rival.Position, Is.EqualTo(opening));

                reel = reel.Advanced(Frame);
            }
        }

        [Test]
        public void ThePlayerThrowsOnTheThrowBeatAndTakesTheFallOnTheLast()
        {
            var thrown = PillarDress.CueOf(PillarRole.Player, PillarStage.Throw);

            Assert.That(thrown.Act, Is.EqualTo(FigureAct.Strike));
            Assert.That(thrown.Loops, Is.False);
            Assert.That(
                thrown.Beat,
                Is.EqualTo(PillarStage.Drain - PillarStage.Throw).Within(Tolerance));

            var falling = PillarDress.CueOf(
                PillarRole.Player, PillarStage.Fall + PillarStage.PortalSeconds);

            Assert.That(falling.Act, Is.EqualTo(FigureAct.Recoil));
            Assert.That(falling.Loops, Is.False);
            Assert.That(
                falling.Beat,
                Is.EqualTo(PillarStage.Total - PillarStage.Fall - PillarStage.PortalSeconds).Within(Tolerance));

            Assert.That(PillarDress.CueOf(PillarRole.Player, 0f).Act, Is.EqualTo(FigureAct.Idle));
            Assert.That(PillarDress.CueOf(PillarRole.Player, PillarStage.Drain).Act, Is.EqualTo(FigureAct.Idle));
        }

        [Test]
        public void EveryCuedBeatIsLongEnoughToPlayThroughOnce()
        {
            var reel = PillarReel.Opening;

            while (!reel.IsOver)
            {
                foreach (var role in PillarDress.Roles)
                {
                    var cue = PillarDress.CueOf(role, reel.Elapsed);

                    Assert.That(
                        cue.Loops || cue.Beat > 0f,
                        Is.True,
                        role + " is cut to a beat of nothing at " + reel.Elapsed + "s.");
                    Assert.That(
                        AdventurerClips.Wants(cue.Clip),
                        Is.True,
                        role + " is cued " + cue.Clip + ", which the pack does not import.");
                }

                reel = reel.Advanced(Frame);
            }
        }

        [Test]
        public void ACutsceneFigureReadsTheSameSizeAsAMazeFigureOfTheSameMesh()
        {
            var reel = PillarReel.Opening;

            while (!reel.IsOver)
            {
                foreach (var role in PillarDress.Roles)
                {
                    var mark = PillarDress.MarkOf(reel, role);
                    var mesh = PillarDress.MeshOf(mark.Look);

                    Assert.That(
                        PillarDress.FigureScaleOf(mark),
                        Is.EqualTo(mark.Scale * FigureFit.ScaleOf(mesh)).Within(Tolerance));
                    Assert.That(
                        PillarDress.StandingHeightOf(mark),
                        Is.EqualTo(FigureFit.StandingHeight(mesh, mark.Scale)).Within(Tolerance));
                    Assert.That(
                        PillarDress.FacingOf(mark.Look),
                        Is.EqualTo(ArtPacks.FacingOf(mesh)).Within(Tolerance));
                    Assert.That(PillarDress.LiftOf(mark.Look), Is.EqualTo(FigureFit.LiftOf(mesh)));
                }

                reel = reel.Advanced(Frame);
            }
        }

        [Test]
        public void ACutsceneFigureStandsAsTallAsTheLookItWears()
        {
            foreach (CastLook look in Enum.GetValues(typeof(CastLook)))
            {
                var mark = new CastMark(
                    1, look, BadgeStyle.Player, 1f, new WorldPoint(0f, 0f, 0f), new WorldPoint(0f, 1f, 0f));

                Assert.That(
                    PillarDress.StandingHeightOf(mark),
                    Is.EqualTo(CastLooks.ScaleOf(look) * ArtPacks.StandingScalesOf(PillarDress.MeshOf(look)))
                        .Within(Tolerance));
                Assert.That(PillarDress.LiftOf(look), Is.Zero);
            }
        }

        [Test]
        public void TheStageIsBuiltFromADungeonPackMesh()
        {
            Assert.That(PillarDress.StageModel, Is.Not.EqualTo(PartModel.None));
            Assert.That(ArtPacks.Of(PillarDress.StageModel), Is.EqualTo(ArtPack.Dungeon));
            Assert.That(ArtPacks.IsRiggedCharacter(PillarDress.StageModel), Is.False);
        }

        [Test]
        public void TheGroundSpansItsReachAndSitsAsDeepAsItIsAsked()
        {
            var scale = PillarDress.GroundScale;
            var mesh = ArtPacks.HeightOf(PillarDress.StageModel);

            Assert.That(
                scale.X * IsoProjection.TileEdge,
                Is.EqualTo(PillarDress.GroundReach).Within(Tolerance));
            Assert.That(
                scale.Z * IsoProjection.TileEdge,
                Is.EqualTo(PillarDress.GroundReach).Within(Tolerance));
            Assert.That(scale.Y * mesh, Is.EqualTo(PillarDress.GroundDepth).Within(Tolerance));
        }

        [Test]
        public void APillarStretchesThePackMeshToExactlyTheHeightTheReelAsksFor()
        {
            var mesh = ArtPacks.HeightOf(PillarDress.StageModel);
            var reel = PillarReel.Opening;

            while (!reel.IsOver)
            {
                foreach (var role in PillarDress.Roles)
                {
                    var height = PillarDress.MarkOf(reel, role).PillarHeight;
                    var scale = PillarDress.PillarScaleOf(height);

                    Assert.That(scale.Y * mesh, Is.EqualTo(height).Within(Tolerance));
                    Assert.That(
                        scale.X * IsoProjection.TileEdge,
                        Is.EqualTo(PillarDress.PillarWidth).Within(Tolerance));
                    Assert.That(scale.Z, Is.EqualTo(scale.X).Within(Tolerance));
                }

                reel = reel.Advanced(Frame);
            }
        }

        [Test]
        public void APillarRefusesToRiseIntoTheGround()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PillarDress.PillarScaleOf(-0.01f));
        }

        [Test]
        public void NothingOnTheStageIsDressedByARoleThatHasNoMesh()
        {
            foreach (var role in PillarDress.Roles)
            {
                Assert.That(PillarDress.LooksOf(role).Count, Is.GreaterThan(0));
            }

            Assert.That(PillarDress.Roles.Count, Is.EqualTo(Enum.GetValues(typeof(PillarRole)).Length));
        }
    }
}
