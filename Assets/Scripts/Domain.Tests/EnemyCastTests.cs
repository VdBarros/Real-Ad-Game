using System;
using System.Collections.Generic;
using System.Linq;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class EnemyCastTests
    {
        const float Tolerance = 1e-4f;

        static readonly int[] EnemyNumbers = { 1, 7, 8, 29, 30, 99, 100, 299, 300, 6303 };

        static readonly int[] PlayerPowers = { 1, 5, 40, 400, 4000 };

        [Test]
        public void AnEnemysOwnNumberDecidesItsSilhouetteAndTheBandNeverDoes()
        {
            for (var tier = 0; tier < EnemyTier.Count; tier++)
            {
                Assert.That(CharacterCast.TierMeshOf(tier), Is.Not.EqualTo(PartModel.None), "tier " + tier);
            }

            Assert.That(
                () => CharacterCast.TierMeshOf(EnemyTier.Count),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => CharacterCast.TierMeshOf(-1),
                Throws.InstanceOf<ArgumentOutOfRangeException>());

            foreach (var number in EnemyNumbers)
            {
                var wanted = CharacterCast.TierMeshOf(EnemyTier.Of(number));

                Assert.That(
                    CharacterCast.MeshOf(PartStyle.Enemy, number), Is.EqualTo(wanted), "power " + number);

                foreach (var playerPower in PlayerPowers)
                {
                    Assert.That(
                        CharacterCast.MeshOf(PartStyle.Enemy, number),
                        Is.EqualTo(wanted),
                        number + " read against " + playerPower
                        + " in band " + EnemyBands.Of(number, playerPower));
                }
            }
        }

        [Test]
        public void TheBandStillMovesTheTintAndTheScaleAndOnlyThose()
        {
            var tints = new List<Tint>();
            var scales = new List<float>();

            foreach (EnemyBand band in Enum.GetValues(typeof(EnemyBand)))
            {
                tints.Add(EnemyBands.TintOf(band));
                scales.Add(EnemyBands.ScaleOf(band));
            }

            Assert.That(tints.Distinct().Count(), Is.EqualTo(tints.Count));
            Assert.That(scales.Distinct().Count(), Is.EqualTo(scales.Count));

            foreach (var number in EnemyNumbers)
            {
                var bands = new List<EnemyBand>();
                var meshes = new List<PartModel>();

                foreach (var playerPower in PlayerPowers)
                {
                    bands.Add(EnemyBands.Of(number, playerPower));
                    meshes.Add(CharacterCast.MeshOf(PartStyle.Enemy, number));
                }

                Assert.That(bands.Distinct().Count(), Is.GreaterThan(1), "power " + number);
                Assert.That(meshes.Distinct().Count(), Is.EqualTo(1), "power " + number);
            }
        }

        [Test]
        public void TheSilhouetteRampNeverStepsBackAsAnEnemysNumberGrows()
        {
            var seen = new List<PartModel>();

            for (var tier = 0; tier < EnemyTier.Count; tier++)
            {
                var mesh = CharacterCast.TierMeshOf(tier);

                if (seen.Count > 0 && seen[seen.Count - 1] == mesh)
                {
                    continue;
                }

                Assert.That(
                    seen.Contains(mesh),
                    Is.False,
                    "tier " + tier + " reuses a silhouette an earlier tier had already left behind");
                seen.Add(mesh);
            }

            Assert.That(seen.Count, Is.EqualTo(3));
            Assert.That(seen[0], Is.EqualTo(PartModel.SkeletonMinion));
            Assert.That(seen[1], Is.EqualTo(PartModel.SkeletonRogue));
            Assert.That(seen[2], Is.EqualTo(PartModel.SkeletonWarrior));
        }

        [Test]
        public void TheBossWearsASilhouetteNoEnemyCanEverWear()
        {
            var boss = CharacterCast.MeshOf(PartStyle.Boss);

            Assert.That(boss, Is.EqualTo(PartModel.SkeletonMage));
            Assert.That(CharacterCast.MeshesOf(PartStyle.Boss), Has.Member(boss));
            Assert.That(CharacterCast.MeshesOf(PartStyle.Enemy), Has.No.Member(boss));
            Assert.That(CharacterCast.MeshesOf(PartStyle.Start), Has.No.Member(boss));

            for (var tier = 0; tier < EnemyTier.Count; tier++)
            {
                Assert.That(CharacterCast.TierMeshOf(tier), Is.Not.EqualTo(boss), "tier " + tier);
            }

            foreach (var number in EnemyNumbers)
            {
                Assert.That(CharacterCast.MeshOf(PartStyle.Boss, number), Is.EqualTo(boss), "power " + number);
                Assert.That(
                    CharacterCast.MeshOf(PartStyle.Enemy, number), Is.Not.EqualTo(boss), "power " + number);
            }
        }

        [Test]
        public void TheBossStandsTallerThanAnyEnemyAtItsOwnFigureScale()
        {
            var boss = CharacterCast.MeshOf(PartStyle.Boss);
            var bossHeight = FigureFit.StandingHeight(boss, LevelBlueprintBuilder.BossScale);

            for (var tier = 0; tier < EnemyTier.Count; tier++)
            {
                Assert.That(
                    FigureFit.StandingHeight(
                        CharacterCast.TierMeshOf(tier), LevelBlueprintBuilder.FigureScale),
                    Is.LessThan(bossHeight),
                    "tier " + tier);
            }

            Assert.That(
                bossHeight,
                Is.EqualTo(
                    FigureFit.StandingHeight(
                        CharacterCast.TierMeshOf(0), LevelBlueprintBuilder.FigureScale)
                    * (LevelBlueprintBuilder.BossScale / LevelBlueprintBuilder.FigureScale))
                    .Within(Tolerance));
        }

        [Test]
        public void EverySkeletonStandsTheSameHeightSoOnlyTheBandMovesAFiguresSize()
        {
            var heights = new List<float>();

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (!SkeletonPack.Carries(model))
                {
                    continue;
                }

                heights.Add(FigureFit.StandingHeight(model, LevelBlueprintBuilder.FigureScale));
                Assert.That(
                    FigureFit.StandingScalesOf(model),
                    Is.EqualTo(SkeletonPack.StandingScales),
                    model.ToString());
            }

            Assert.That(heights.Count, Is.EqualTo(4));
            Assert.That(heights.Distinct().Count(), Is.EqualTo(1));
        }

        [Test]
        public void EveryCastFigureHidesLessGroundThanTheCapsuleItReplacesAtEveryBand()
        {
            foreach (var role in CharacterCast.Roles)
            {
                var basis = role == PartStyle.Boss
                    ? LevelBlueprintBuilder.BossScale
                    : LevelBlueprintBuilder.FigureScale;

                foreach (var mesh in CharacterCast.MeshesOf(role))
                {
                    foreach (EnemyBand band in Enum.GetValues(typeof(EnemyBand)))
                    {
                        var scale = basis * EnemyBands.ScaleOf(band);
                        var where = role + " wearing " + mesh + " in band " + band;

                        Assert.That(
                            FigureFit.HiddenGroundOf(mesh, scale),
                            Is.LessThan(FigureFit.HiddenGroundOf(PartModel.None, scale)),
                            where);
                        Assert.That(
                            IsoProjection.SightReach(FigureFit.StandingHeight(mesh, scale)),
                            Is.LessThan(
                                IsoProjection.SightReach(FigureFit.StandingHeight(PartModel.None, scale))),
                            where);
                    }
                }
            }
        }

        [Test]
        public void EveryEnemyStaysInsideTheOcclusionBoundTheWallWorkSetAtItsOwnFigureScale()
        {
            var bound = IsoProjection.TileEdge * IsoProjection.OcclusionBound * IsoProjection.TileEdge;
            var scale = LevelBlueprintBuilder.FigureScale;

            foreach (var mesh in CharacterCast.MeshesOf(PartStyle.Enemy))
            {
                Assert.That(FigureFit.HiddenGroundOf(mesh, scale), Is.LessThan(bound), mesh.ToString());
                Assert.That(FigureFit.HiddenSpreadOf(mesh, scale), Is.LessThan(bound), mesh.ToString());
            }
        }

        [Test]
        public void EveryCastFigureStaysOnItsOwnTileAtTheLargestScaleItsSeamGrowsItTo()
        {
            var widest = 0f;

            foreach (EnemyBand band in Enum.GetValues(typeof(EnemyBand)))
            {
                if (EnemyBands.ScaleOf(band) > widest)
                {
                    widest = EnemyBands.ScaleOf(band);
                }
            }

            foreach (var role in CharacterCast.Roles)
            {
                var scale = Widest(role, widest);

                foreach (var mesh in CharacterCast.MeshesOf(role))
                {
                    Assert.That(
                        FigureFit.TileReachOf(mesh, scale),
                        Is.LessThan(IsoProjection.TileEdge),
                        role + " wearing " + mesh + " at scale " + scale);
                    Assert.That(
                        FigureFit.TileReachOf(mesh, scale),
                        Is.GreaterThan(FigureFit.WidthOf(mesh, scale) * 0.5f),
                        role + " wearing " + mesh);
                }
            }
        }

        [Test]
        public void TheCastPacksAgreeOnWhatOneImportSettingHasToSettle()
        {
            Assert.That(
                ArtPacks.CastImportScale,
                Is.EqualTo(ArtPacks.ImportScaleOf(ArtPack.Adventurers)).Within(Tolerance));
            Assert.That(
                ArtPacks.CastImportScale,
                Is.EqualTo(ArtPacks.ImportScaleOf(ArtPack.Skeletons)).Within(Tolerance));
            Assert.That(ArtPacks.CastSlotNode, Is.EqualTo(AdventurerPack.SlotNode));
            Assert.That(ArtPacks.CastSlotNode, Is.EqualTo(SkeletonPack.SlotNode));
            Assert.That(SkeletonPack.GridUnits, Is.EqualTo(DungeonPack.GridUnits));
            Assert.That(
                SkeletonPack.GridUnits * SkeletonPack.ImportScale,
                Is.EqualTo(IsoProjection.TileEdge).Within(Tolerance));
        }

        [Test]
        public void EverySkeletonCarriesAMeasuredPackFootprintAndNothingElseDoes()
        {
            var carried = 0;

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (!SkeletonPack.Carries(model))
                {
                    Assert.That(
                        () => SkeletonPack.PackHeightOf(model),
                        Throws.InstanceOf<ArgumentOutOfRangeException>(),
                        model.ToString());
                    continue;
                }

                carried++;

                Assert.That(ArtPacks.Of(model), Is.EqualTo(ArtPack.Skeletons), model.ToString());
                Assert.That(AdventurerPack.Carries(model), Is.False, model.ToString());
                Assert.That(SkeletonPack.PackHeightOf(model), Is.GreaterThan(0f), model.ToString());
                Assert.That(SkeletonPack.PackWidthOf(model), Is.GreaterThan(0f), model.ToString());
                Assert.That(SkeletonPack.PackDepthOf(model), Is.GreaterThan(0f), model.ToString());
                Assert.That(SkeletonPack.PackBaseOf(model), Is.LessThanOrEqualTo(0f), model.ToString());
                Assert.That(
                    ArtPacks.PackHeightOf(model),
                    Is.EqualTo(SkeletonPack.PackHeightOf(model)).Within(Tolerance),
                    model.ToString());
                Assert.That(
                    ArtPacks.WidthOf(model),
                    Is.EqualTo(SkeletonPack.WidthOf(model)).Within(Tolerance),
                    model.ToString());
                Assert.That(
                    ArtPacks.DepthOf(model),
                    Is.EqualTo(SkeletonPack.DepthOf(model)).Within(Tolerance),
                    model.ToString());
                Assert.That(
                    ArtPacks.BaseOf(model),
                    Is.EqualTo(SkeletonPack.BaseOf(model)).Within(Tolerance),
                    model.ToString());
            }

            Assert.That(carried, Is.EqualTo(4));
        }

        [Test]
        public void EveryEnemyAndTheBossCarryTheMeshTheirOwnNumberNames()
        {
            var graph = LevelGenerator.Generate(20250824L, MazePreset.Ship).Graph;
            var enemies = 0;
            var bosses = 0;

            foreach (var node in graph.Decisions.Nodes)
            {
                WorldPart prop;
                if (!LevelBlueprintBuilder.TryProp(node, out prop))
                {
                    continue;
                }

                if (node.Type == NodeType.Enemy)
                {
                    enemies++;

                    Assert.That(
                        prop.Model,
                        Is.EqualTo(CharacterCast.MeshOf(PartStyle.Enemy, node.Value)),
                        prop.Name + " holding " + node.Value);
                    Assert.That(
                        prop.Model,
                        Is.EqualTo(CharacterCast.TierMeshOf(EnemyTier.Of(node.Value))),
                        prop.Name);
                }
                else if (node.Type == NodeType.Boss)
                {
                    bosses++;

                    Assert.That(prop.Model, Is.EqualTo(CharacterCast.MeshOf(PartStyle.Boss)), prop.Name);
                }
            }

            Assert.That(enemies, Is.GreaterThan(0));
            Assert.That(bosses, Is.EqualTo(1));
        }

        [Test]
        public void MoreThanOneSilhouetteReachesAShippedLevelSoTheRampIsVisibleInPlay()
        {
            var worn = new List<PartModel>();

            for (var levelNumber = 1; levelNumber <= 16; levelNumber++)
            {
                var graph = Level(levelNumber).Graph;

                foreach (var node in graph.Decisions.Nodes)
                {
                    if (node.Type != NodeType.Enemy)
                    {
                        continue;
                    }

                    var mesh = CharacterCast.MeshOf(PartStyle.Enemy, node.Value);

                    if (!worn.Contains(mesh))
                    {
                        worn.Add(mesh);
                    }
                }
            }

            Assert.That(worn.Count, Is.EqualTo(CharacterCast.MeshesOf(PartStyle.Enemy).Count));
        }

        [Test]
        public void AnEliteIsNeverAffordableOnArrivalSoTheBandAlreadyShowsItAsADoor()
        {
            var doors = 0;
            var beatable = 0;

            for (var levelNumber = 1; levelNumber <= 16; levelNumber++)
            {
                var placed = Level(levelNumber);
                var cheapest = new Dictionary<int, int>();

                foreach (var region in PowerEnvelope.Of(placed.Graph, placed.Tuning).Regions)
                {
                    cheapest[region.RegionId] = region.CheapestEntry;
                }

                foreach (var locked in Elites.Of(placed.Graph, placed.Tuning))
                {
                    var node = placed.Graph.Decisions.Node(locked);
                    var arrival = cheapest[placed.Graph.RegionOf(locked)];

                    Assert.That(
                        arrival,
                        Is.GreaterThan(0),
                        "level " + levelNumber + " node " + locked
                        + " stands in a region no route arrives at holding power, so no band can be "
                        + "read against it");

                    doors++;
                    var band = EnemyBands.Of(node.Value, arrival);

                    if (EnemyBands.IsBeatable(band))
                    {
                        beatable++;
                    }

                    Assert.That(
                        EnemyBands.IsBeatable(band),
                        Is.False,
                        "level " + levelNumber + " node " + locked + " holding " + node.Value
                        + " reads " + band + " on an arrival of " + arrival);
                }
            }

            Assert.That(doors, Is.GreaterThan(0));
            Assert.That(beatable, Is.EqualTo(0));
        }

        static float Widest(PartStyle role, float widestBand)
        {
            if (role == PartStyle.Start)
            {
                return PlayerLook.Of(PlayerTier.Thresholds[PlayerTier.Count - 2]).Scale;
            }

            var basis = role == PartStyle.Boss
                ? LevelBlueprintBuilder.BossScale
                : LevelBlueprintBuilder.FigureScale;

            return basis * widestBand;
        }

        static PlacedLevel Level(int levelNumber)
        {
            var plan = LevelPlan.For(levelNumber);
            LevelGenerationReport ignored;

            return LevelGenerator.Generate(
                LevelSupply.Scattered(7919L, levelNumber),
                plan.Preset,
                plan.Recipe,
                plan.Tuning,
                out ignored);
        }
    }
}
