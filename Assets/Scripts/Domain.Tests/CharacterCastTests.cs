using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class CharacterCastTests
    {
        const float Tolerance = 1e-4f;

        static readonly int[] PowersAcrossEveryTier = { 1, 8, 30, 100, 300 };

        [Test]
        public void ThePlayerWearsARiggedMeshFromTheAdventurersPack()
        {
            var mesh = CharacterCast.MeshOf(PartStyle.Start);

            Assert.That(mesh, Is.Not.EqualTo(PartModel.None));
            Assert.That(ArtPacks.Of(mesh), Is.EqualTo(ArtPack.Adventurers));
            Assert.That(ArtPacks.IsRigged(mesh), Is.True);
            Assert.That(AdventurerPack.Carries(mesh), Is.True);
            Assert.That(PartModels.Of(PartStyle.Start), Is.EqualTo(mesh));
        }

        [Test]
        public void ThePlayerWearsTheSameMeshAtEveryPowerTier()
        {
            var tiers = PowersAcrossEveryTier.Select(PlayerTier.Of).Distinct().ToList();

            Assert.That(tiers.Count, Is.EqualTo(PlayerTier.Count));

            foreach (var power in PowersAcrossEveryTier)
            {
                var blueprint = LevelBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces());
                var player = blueprint.AllParts.First(part => part.Style == PartStyle.Start);

                Assert.That(PlayerLook.Of(power).Tier, Is.EqualTo(PlayerTier.Of(power)));
                Assert.That(player.Model, Is.EqualTo(PartModel.Knight), "power " + power);
            }
        }

        [Test]
        public void TheTierSeamStillChangesHowTheFigureReadsWithoutChangingTheMesh()
        {
            var looks = PowersAcrossEveryTier.Select(PlayerLook.Of).ToList();

            for (var step = 1; step < looks.Count; step++)
            {
                Assert.That(looks[step].Tier, Is.EqualTo(looks[step - 1].Tier + 1));
                Assert.That(
                    looks[step].Scale,
                    Is.EqualTo(looks[step - 1].Scale * PlayerLook.Growth).Within(Tolerance));
            }

            Assert.That(
                looks.Select(look => PartModels.Of(PartStyle.Start)).Distinct().Count(),
                Is.EqualTo(1));
        }

        [Test]
        public void NoReadingOfACharacterCarriesAColourWhileEveryOneStillCarriesASize()
        {
            foreach (var reading in new[] { typeof(PlayerLook), typeof(Promotion), typeof(PowerBeat) })
            {
                Assert.That(Colours(reading), Is.Empty, reading.Name);
                Assert.That(reading.GetProperty("Scale"), Is.Not.Null, reading.Name);
            }

            Assert.That(Colours(typeof(EnemyBands)), Is.Empty);

            var sizes = ((EnemyBand[])Enum.GetValues(typeof(EnemyBand)))
                .Select(EnemyBands.ScaleOf)
                .ToList();

            Assert.That(sizes.Distinct().Count(), Is.EqualTo(sizes.Count));
            Assert.That(
                PowersAcrossEveryTier.Select(power => PlayerLook.Of(power).Scale).Distinct().Count(),
                Is.EqualTo(PowersAcrossEveryTier.Length));
        }

        static List<string> Colours(Type reading)
        {
            const BindingFlags Everything = BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.DeclaredOnly;

            var found = new List<string>();

            foreach (var field in reading.GetFields(Everything))
            {
                if (IsColour(field.FieldType))
                {
                    found.Add(reading.Name + "." + field.Name);
                }
            }

            foreach (var property in reading.GetProperties(Everything))
            {
                if (IsColour(property.PropertyType))
                {
                    found.Add(reading.Name + "." + property.Name);
                }
            }

            foreach (var method in reading.GetMethods(Everything))
            {
                if (IsColour(method.ReturnType))
                {
                    found.Add(reading.Name + "." + method.Name + "()");
                }
            }

            return found;
        }

        static bool IsColour(Type type)
        {
            if (type == typeof(Tint))
            {
                return true;
            }

            return type.IsArray && type.GetElementType() == typeof(Tint);
        }

        [Test]
        public void EveryRoleInTheCastWearsARiggedMeshFromACharacterPack()
        {
            foreach (var role in CharacterCast.Roles)
            {
                Assert.That(PartModels.Of(role), Is.Not.EqualTo(PartModel.None), role.ToString());

                foreach (var mesh in CharacterCast.MeshesOf(role))
                {
                    Assert.That(mesh, Is.Not.EqualTo(PartModel.None), role.ToString());
                    Assert.That(ArtPacks.IsRigged(mesh), Is.True, role + " wears " + mesh);
                }
            }
        }

        [Test]
        public void OnlyTheCastAnswersWhichMeshItWears()
        {
            foreach (PartStyle style in Enum.GetValues(typeof(PartStyle)))
            {
                if (CharacterCast.IsRole(style))
                {
                    Assert.That(() => CharacterCast.MeshOf(style), Throws.Nothing, style.ToString());
                    continue;
                }

                Assert.That(
                    () => CharacterCast.MeshOf(style),
                    Throws.InstanceOf<ArgumentOutOfRangeException>(),
                    style.ToString());
            }
        }

        [Test]
        public void AFigureStandsTheHeightItsOwnScaleAsksFor()
        {
            var mesh = CharacterCast.MeshOf(PartStyle.Start);
            var scale = LevelBlueprintBuilder.FigureScale;

            Assert.That(
                FigureFit.StandingHeight(PartModel.None, scale),
                Is.EqualTo(scale * FigureFit.CapsuleScales).Within(Tolerance));
            Assert.That(
                FigureFit.StandingHeight(mesh, scale),
                Is.EqualTo(scale * AdventurerPack.StandingScales).Within(Tolerance));
            Assert.That(
                AdventurerPack.HeightOf(mesh) * FigureFit.ScaleOf(mesh) * scale,
                Is.EqualTo(FigureFit.StandingHeight(mesh, scale)).Within(Tolerance));
        }

        [Test]
        public void TheTierSeamGrowsTheMeshByTheSameStepItGrewTheCapsuleBy()
        {
            var mesh = CharacterCast.MeshOf(PartStyle.Start);

            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                var below = PlayerLook.Of(PlayerTier.Thresholds[tier - 1] - 1).Scale;
                var above = PlayerLook.Of(PlayerTier.Thresholds[tier - 1]).Scale;

                Assert.That(
                    FigureFit.StandingHeight(mesh, above),
                    Is.EqualTo(FigureFit.StandingHeight(mesh, below) * PlayerLook.Growth).Within(Tolerance),
                    "tier " + tier);
                Assert.That(
                    FigureFit.StandingHeight(mesh, above) / FigureFit.StandingHeight(mesh, below),
                    Is.EqualTo(
                        FigureFit.StandingHeight(PartModel.None, above)
                        / FigureFit.StandingHeight(PartModel.None, below)).Within(Tolerance),
                    "tier " + tier);
            }
        }

        [Test]
        public void AMeshFigurePutsItsFeetOnTheTileTheCapsuleFloatedAbove()
        {
            var player = Player();
            var tile = IsoProjection.Of(PlayerNode().Position);

            Assert.That(player.Model, Is.EqualTo(CharacterCast.MeshOf(PartStyle.Start)));
            Assert.That(player.Position.Y, Is.GreaterThan(tile.Y));
            Assert.That(ModelPose.PositionOf(player).Y, Is.EqualTo(tile.Y).Within(Tolerance));
            Assert.That(ModelPose.PositionOf(player).X, Is.EqualTo(tile.X).Within(Tolerance));
            Assert.That(ModelPose.PositionOf(player).Z, Is.EqualTo(tile.Z).Within(Tolerance));
            Assert.That(FigureFit.LiftOf(player.Model), Is.EqualTo(0f));
        }

        [Test]
        public void AMeshFigureIsFittedInsideTheTileItStandsOn()
        {
            var mesh = CharacterCast.MeshOf(PartStyle.Start);
            var scale = ModelPose.ScaleOf(Player()).Y;

            Assert.That(
                AdventurerPack.WidthOf(mesh) * scale,
                Is.LessThan(IsoProjection.TileEdge));
            Assert.That(
                AdventurerPack.DepthOf(mesh) * scale,
                Is.LessThan(IsoProjection.TileEdge));
            Assert.That(
                AdventurerPack.HeightOf(mesh) * scale,
                Is.LessThan(IsoProjection.StepHeight));
            Assert.That(
                FigureFit.SpreadOf(mesh, LevelBlueprintBuilder.FigureScale),
                Is.LessThan(IsoProjection.TileEdge));
            Assert.That(
                FigureFit.SpreadOf(mesh, LevelBlueprintBuilder.FigureScale),
                Is.GreaterThan(FigureFit.WidthOf(mesh, LevelBlueprintBuilder.FigureScale)));
        }

        [Test]
        public void AMeshFigureHidesLessGroundThanTheCapsuleItReplaces()
        {
            var mesh = CharacterCast.MeshOf(PartStyle.Start);
            var scale = LevelBlueprintBuilder.FigureScale;

            Assert.That(
                IsoProjection.SightReach(FigureFit.StandingHeight(mesh, scale)),
                Is.LessThan(IsoProjection.SightReach(FigureFit.StandingHeight(PartModel.None, scale))));
            Assert.That(
                FigureFit.HiddenGroundOf(mesh, scale),
                Is.LessThan(FigureFit.HiddenGroundOf(PartModel.None, scale)));
        }

        [Test]
        public void AMeshFigureStaysInsideTheOcclusionBoundTheWallWorkSet()
        {
            var mesh = CharacterCast.MeshOf(PartStyle.Start);
            var scale = LevelBlueprintBuilder.FigureScale;
            var bound = IsoProjection.TileEdge * IsoProjection.OcclusionBound * IsoProjection.TileEdge;

            Assert.That(IsoProjection.OcclusionBound, Is.EqualTo(0.5f));
            Assert.That(FigureFit.HiddenGroundOf(mesh, scale), Is.LessThan(bound));
            Assert.That(FigureFit.HiddenSpreadOf(mesh, scale), Is.LessThan(bound));
            Assert.That(
                FigureFit.HiddenSpreadOf(mesh, scale),
                Is.GreaterThan(FigureFit.HiddenGroundOf(mesh, scale)));
            Assert.That(
                IsoProjection.TileEdge * IsoProjection.SightReach(DungeonPack.HeightOf(PartModel.WallPanel)),
                Is.LessThan(bound));
        }

        [Test]
        public void AMeshFigureTurnsItsFaceTowardTheCamera()
        {
            var forward = IsoProjection.CameraForward;
            var toward = Math.Atan2(-forward.X, -forward.Z) * 180.0 / Math.PI;
            var wanted = (float)((toward + 360.0) % 360.0);

            Assert.That(AdventurerPack.Facing, Is.EqualTo(wanted).Within(1e-3f));
            Assert.That(ModelPose.RotationOf(Player()).Y, Is.EqualTo(wanted).Within(1e-3f));
        }

        [Test]
        public void TheAdventurersImportScaleMapsThePacksGridOntoOneTile()
        {
            Assert.That(AdventurerPack.GridUnits, Is.EqualTo(DungeonPack.GridUnits));
            Assert.That(
                AdventurerPack.GridUnits * AdventurerPack.ImportScale,
                Is.EqualTo(IsoProjection.TileEdge).Within(Tolerance));
            Assert.That(AdventurerPack.ImportScale, Is.EqualTo(DungeonPack.ImportScale).Within(Tolerance));
        }

        [Test]
        public void OnlyTheStandingScalesSetAFittedFiguresHeight()
        {
            var mesh = CharacterCast.MeshOf(PartStyle.Start);
            var scale = LevelBlueprintBuilder.FigureScale;

            Assert.That(
                FigureFit.ScaleOf(mesh) * AdventurerPack.HeightOf(mesh),
                Is.EqualTo(AdventurerPack.StandingScales).Within(Tolerance));
            Assert.That(
                FigureFit.ScaleOf(mesh) * AdventurerPack.PackHeightOf(mesh) * AdventurerPack.ImportScale,
                Is.EqualTo(AdventurerPack.StandingScales).Within(Tolerance));
            Assert.That(
                FigureFit.StandingHeight(mesh, scale),
                Is.EqualTo(scale * AdventurerPack.StandingScales).Within(Tolerance));

            Assert.That(
                FigureFit.WidthOf(mesh, scale) / FigureFit.StandingHeight(mesh, scale),
                Is.EqualTo(AdventurerPack.KnightPackWidth / AdventurerPack.KnightPackHeight)
                    .Within(Tolerance));
            Assert.That(
                FigureFit.DepthOf(mesh, scale) / FigureFit.StandingHeight(mesh, scale),
                Is.EqualTo(AdventurerPack.KnightPackDepth / AdventurerPack.KnightPackHeight)
                    .Within(Tolerance));
        }

        [Test]
        public void EveryCastMeshCarriesAMeasuredPackFootprint()
        {
            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (!AdventurerPack.Carries(model))
                {
                    Assert.That(
                        () => AdventurerPack.PackHeightOf(model),
                        Throws.InstanceOf<ArgumentOutOfRangeException>(),
                        model.ToString());
                    continue;
                }

                Assert.That(AdventurerPack.PackHeightOf(model), Is.GreaterThan(0f), model.ToString());
                Assert.That(AdventurerPack.PackWidthOf(model), Is.GreaterThan(0f), model.ToString());
                Assert.That(AdventurerPack.PackDepthOf(model), Is.GreaterThan(0f), model.ToString());
                Assert.That(AdventurerPack.PackBaseOf(model), Is.LessThanOrEqualTo(0f), model.ToString());
                Assert.That(
                    AdventurerPack.HeightOf(model),
                    Is.EqualTo(AdventurerPack.PackHeightOf(model) * AdventurerPack.ImportScale)
                        .Within(Tolerance),
                    model.ToString());
            }
        }

        [Test]
        public void OnlyACastMeshOrAPrimitiveCanStandAsAFigure()
        {
            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (model == PartModel.None || ArtPacks.IsRigged(model))
                {
                    Assert.That(() => FigureFit.ScaleOf(model), Throws.Nothing, model.ToString());
                    continue;
                }

                Assert.That(
                    () => FigureFit.ScaleOf(model),
                    Throws.InstanceOf<ArgumentOutOfRangeException>(),
                    model.ToString());
            }
        }

        [Test]
        public void EveryPartModelBelongsToExactlyOnePack()
        {
            Assert.That(
                () => ArtPacks.Of(PartModel.None),
                Throws.InstanceOf<ArgumentOutOfRangeException>());

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (model == PartModel.None)
                {
                    continue;
                }

                var pack = ArtPacks.Of(model);

                Assert.That(
                    pack == ArtPack.Adventurers,
                    Is.EqualTo(AdventurerPack.Carries(model)),
                    model.ToString());
                Assert.That(
                    ArtPacks.ImportScaleFor(model),
                    Is.EqualTo(ArtPacks.ImportScaleOf(pack)).Within(Tolerance),
                    model.ToString());
            }
        }

        static DecisionNode PlayerNode()
        {
            var graph = LevelGraphFixture.TwoTerraces();

            return graph.Decisions.Nodes.First(node => node.Type == NodeType.Start);
        }

        static WorldPart Player()
        {
            WorldPart prop;
            LevelBlueprintBuilder.TryProp(PlayerNode(), out prop);

            return prop;
        }
    }
}
