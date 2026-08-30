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

        static readonly int[] EnemyNumbers = { 1, 11, 12, 49, 50, 51, 300, 6303 };

        static readonly int[] PlayerPowers = { 1, 5, 40, 400, 4000 };

        static readonly int[] PowersAcrossEveryTier = { 1, 8, 30, 100, 300, 4000 };

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
        public void TheBandStillMovesTheScaleAndOnlyTheScale()
        {
            var scales = new List<float>();

            foreach (EnemyBand band in Enum.GetValues(typeof(EnemyBand)))
            {
                scales.Add(EnemyBands.ScaleOf(band));
            }

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
        public void NoTwoEnemyTiersShareASilhouetteSoTheMeshAloneSeparatesThem()
        {
            var byTier = new List<PartModel>();

            for (var tier = 0; tier < EnemyTier.Count; tier++)
            {
                byTier.Add(CharacterCast.TierMeshOf(tier));
            }

            Assert.That(byTier.Distinct().Count(), Is.EqualTo(EnemyTier.Count), string.Join(", ", byTier));
            Assert.That(CharacterCast.MeshesOf(PartStyle.Enemy).Count, Is.EqualTo(EnemyTier.Count));
            Assert.That(EnemyTier.Count, Is.EqualTo(3));
        }

        [Test]
        public void TheSkeletonPackDressesEveryEnemyTierAndStillKeepsOneMeshBackForTheBoss()
        {
            var carried = 0;

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (SkeletonPack.Carries(model))
                {
                    carried++;
                }
            }

            Assert.That(CharacterCast.MeshesOf(PartStyle.Enemy).Count, Is.EqualTo(carried - 1));
            Assert.That(
                CharacterCast.MeshesOf(PartStyle.Enemy),
                Has.No.Member(CharacterCast.MeshOf(PartStyle.Boss)));

            foreach (var mesh in CharacterCast.MeshesOf(PartStyle.Enemy))
            {
                Assert.That(SkeletonPack.Carries(mesh), Is.True, mesh.ToString());
            }
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
        public void NoEnemyAtAnyBandAndAnyPlayerTierFallsBelowTheReadabilityFloor()
        {
            var read = 0;
            var smallest = float.MaxValue;
            var complaint = new List<string>();

            foreach (var number in EnemyNumbers)
            {
                var mesh = CharacterCast.MeshOf(PartStyle.Enemy, number);

                foreach (var power in PowersAcrossEveryTier)
                {
                    var band = EnemyBands.Of(number, power);
                    var scale = LevelBlueprintBuilder.FigureScale * EnemyBands.ScaleOf(band);
                    var showing = FigureReadability.PixelsShowing(mesh, scale);

                    smallest = Math.Min(smallest, showing);
                    read++;

                    if (!FigureReadability.Reads(mesh, scale))
                    {
                        complaint.Add(
                            number + " read against " + power + " stands " + band + " at "
                            + showing.ToString("0.#") + " pixels");
                    }
                }
            }

            foreach (EnemyBand band in Enum.GetValues(typeof(EnemyBand)))
            {
                foreach (var mesh in CharacterCast.MeshesOf(PartStyle.Enemy))
                {
                    var scale = LevelBlueprintBuilder.FigureScale * EnemyBands.ScaleOf(band);

                    read++;
                    smallest = Math.Min(smallest, FigureReadability.PixelsShowing(mesh, scale));

                    if (!FigureReadability.Reads(mesh, scale))
                    {
                        complaint.Add(mesh + " in band " + band + " falls below the floor");
                    }
                }
            }

            Assert.That(read, Is.GreaterThan(0));
            Assert.That(complaint, Is.Empty);
            Assert.That(
                smallest,
                Is.GreaterThanOrEqualTo(FigureReadability.ReadablePixels),
                "the smallest enemy anywhere in the cross product stands " + smallest.ToString("0.#")
                + " pixels tall");
        }

        [Test]
        public void ThePlayersOwnTierMovesNeitherAnEnemysBandNorItsSize()
        {
            foreach (var number in EnemyNumbers)
            {
                var mesh = CharacterCast.MeshOf(PartStyle.Enemy, number);
                var byBand = new Dictionary<EnemyBand, float>();

                foreach (var power in PowersAcrossEveryTier)
                {
                    var band = EnemyBands.Of(number, power);
                    var showing = FigureReadability.PixelsShowing(
                        mesh, LevelBlueprintBuilder.FigureScale * EnemyBands.ScaleOf(band));

                    if (byBand.ContainsKey(band))
                    {
                        Assert.That(
                            byBand[band],
                            Is.EqualTo(showing).Within(Tolerance),
                            number + " read against " + power);
                        continue;
                    }

                    byBand.Add(band, showing);
                }

                Assert.That(byBand.Count, Is.GreaterThan(1), "power " + number);
            }
        }

        [Test]
        public void TheReadabilityFloorIsReadOffTheFramingRatherThanPinnedToAScale()
        {
            Assert.That(
                FigureReadability.ShareOfScreen,
                Is.EqualTo(FigureReadability.ReadablePixels / ScreenFrame.Height).Within(Tolerance));
            Assert.That(
                FigureReadability.Height,
                Is.EqualTo(FigureReadability.ShareOfScreen * 2f * LevelFraming.PlaySize).Within(Tolerance));
            Assert.That(
                FigureReadability.ShareOfScreen,
                Is.LessThan(LevelFraming.FigureHeightFraction),
                "a figure that reads is allowed to stand shorter than the player #136 framed");

            foreach (var mesh in CharacterCast.MeshesOf(PartStyle.Enemy))
            {
                var floor = FigureReadability.ScaleOf(mesh);

                Assert.That(
                    FigureFit.StandingHeight(mesh, floor),
                    Is.EqualTo(FigureReadability.Height).Within(Tolerance),
                    mesh.ToString());
                Assert.That(
                    FigureReadability.ShareShowing(mesh, floor),
                    Is.EqualTo(FigureReadability.ShareOfScreen).Within(Tolerance),
                    mesh.ToString());
                Assert.That(FigureReadability.Reads(mesh, floor), Is.True, mesh.ToString());
                Assert.That(FigureReadability.Reads(mesh, floor * 0.99f), Is.False, mesh.ToString());
            }

            var wider = LevelFraming.PlaySize * 2f;

            Assert.That(
                LevelFraming.HeightShowing(FigureReadability.ShareOfScreen, wider),
                Is.EqualTo(FigureReadability.Height * 2f).Within(Tolerance),
                "pull the framing out and the floor rises with it, because it is a share of the screen "
                + "and never a scale");
            Assert.That(
                () => LevelFraming.HeightShowing(0f, LevelFraming.PlaySize),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => LevelFraming.HeightShowing(FigureReadability.ShareOfScreen, 0f),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TheBandRampStillSeparatesTheFourBandsWithTheFloorUnderIt()
        {
            var trivial = EnemyBands.ScaleOf(EnemyBand.Trivial);

            Assert.That(EnemyBands.ScaleOf(EnemyBand.Close) / trivial, Is.GreaterThan(1.1f));
            Assert.That(EnemyBands.ScaleOf(EnemyBand.OutOfReach) / trivial, Is.GreaterThan(1.2f));

            foreach (EnemyBand band in Enum.GetValues(typeof(EnemyBand)))
            {
                if (band == EnemyBand.Trivial)
                {
                    continue;
                }

                Assert.That(EnemyBands.ScaleOf(band), Is.GreaterThan(trivial), band.ToString());
            }
        }

        [Test]
        public void TheBossOutgrowsEveryEnemyAtEveryPairingOfBands()
        {
            var boss = CharacterCast.MeshOf(PartStyle.Boss);
            var tightest = float.MaxValue;

            foreach (EnemyBand wearing in Enum.GetValues(typeof(EnemyBand)))
            {
                var standing = FigureFit.StandingHeight(
                    boss, LevelBlueprintBuilder.BossScale * EnemyBands.ScaleOf(wearing));

                Assert.That(
                    FigureReadability.Reads(
                        boss, LevelBlueprintBuilder.BossScale * EnemyBands.ScaleOf(wearing)),
                    Is.True,
                    wearing.ToString());

                foreach (EnemyBand against in Enum.GetValues(typeof(EnemyBand)))
                {
                    foreach (var mesh in CharacterCast.MeshesOf(PartStyle.Enemy))
                    {
                        var enemy = FigureFit.StandingHeight(
                            mesh, LevelBlueprintBuilder.FigureScale * EnemyBands.ScaleOf(against));

                        tightest = Math.Min(tightest, standing / enemy);

                        Assert.That(
                            standing,
                            Is.GreaterThan(enemy),
                            "the boss in band " + wearing + " against " + mesh + " in band " + against);
                    }
                }
            }

            Assert.That(
                tightest,
                Is.GreaterThan(1.2f),
                "the tightest pairing leaves the boss " + tightest.ToString("0.###")
                + " times the height of the enemy");
        }

        [Test]
        public void TheSkeletonsStandAtTheRateTheKnightStandsAtAndNoneOfThemIsStretchedToDoIt()
        {
            Assert.That(
                AdventurerPack.StandingPerPackUnit,
                Is.EqualTo(AdventurerPack.StandingScales / AdventurerPack.KnightPackHeight)
                    .Within(Tolerance));

            var shortest = float.MaxValue;
            var shortestMesh = PartModel.None;

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (!SkeletonPack.Carries(model))
                {
                    continue;
                }

                var height = SkeletonPack.PackHeightOf(model);

                Assert.That(
                    SkeletonPack.StandingScales,
                    Is.LessThanOrEqualTo(AdventurerPack.StandingPerPackUnit * height),
                    model + " would have to be stretched past the height its own mesh measures");

                if (height < shortest)
                {
                    shortest = height;
                    shortestMesh = model;
                }
            }

            Assert.That(SkeletonPack.ShortestPackHeight, Is.EqualTo(shortest).Within(Tolerance));
            Assert.That(shortestMesh, Is.EqualTo(PartModel.SkeletonMinion));
            Assert.That(
                SkeletonPack.StandingScales,
                Is.EqualTo(AdventurerPack.StandingPerPackUnit * shortest).Within(Tolerance));
            Assert.That(
                SkeletonPack.StandingScales,
                Is.LessThan(AdventurerPack.StandingScales),
                "the shortest skeleton mesh is shorter than the Knight's, so the pack stands shorter");
            Assert.That(
                SkeletonPack.StandingScales,
                Is.GreaterThan(1.4f),
                "and far taller than the 1.3 it was pinned at before the player grew");
        }

        [Test]
        public void NoAdversaryTurnedOnItsTileHidesMoreGroundThanTheCapsuleItReplaces()
        {
            var tightest = 0f;
            var where = "nothing";

            foreach (var role in CharacterCast.Roles)
            {
                if (role == PartStyle.Start)
                {
                    continue;
                }

                var basis = role == PartStyle.Boss
                    ? LevelBlueprintBuilder.BossScale
                    : LevelBlueprintBuilder.FigureScale;

                foreach (var mesh in CharacterCast.MeshesOf(role))
                {
                    foreach (EnemyBand band in Enum.GetValues(typeof(EnemyBand)))
                    {
                        var scale = basis * EnemyBands.ScaleOf(band);
                        var turned = FigureFit.TileReachOf(mesh, scale)
                            * IsoProjection.SightReach(FigureFit.StandingHeight(mesh, scale));
                        var capsule = FigureFit.HiddenGroundOf(PartModel.None, scale);
                        var share = turned / capsule;

                        if (share > tightest)
                        {
                            tightest = share;
                            where = role + " wearing " + mesh + " in band " + band;
                        }

                        Assert.That(
                            turned,
                            Is.LessThan(capsule),
                            role + " wearing " + mesh + " in band " + band);
                    }
                }
            }

            Assert.That(
                tightest,
                Is.LessThan(1f),
                where + " hides " + tightest.ToString("0.###")
                + " of what its capsule hid, which is the ceiling the standing scales answer to");
        }

        [Test]
        public void EveryAdversaryHidesLessGroundThanTheCapsuleItReplacesAtEveryBand()
        {
            foreach (var role in CharacterCast.Roles)
            {
                if (role == PartStyle.Start)
                {
                    continue;
                }

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
        public void TheCapsuleOcclusionBudgetIsAnAdversaryPropertyTheHeroAlreadyStandsOutsideOf()
        {
            var scale = LevelBlueprintBuilder.FigureScale;
            var capsule = FigureFit.HiddenGroundOf(PartModel.None, scale);
            var square = new List<string>();

            foreach (var guise in PlayerGuises.All)
            {
                var mesh = PlayerKit.BodyOf(guise);
                var turned = FigureFit.TileReachOf(mesh, scale)
                    * IsoProjection.SightReach(FigureFit.StandingHeight(mesh, scale));

                Assert.That(
                    turned,
                    Is.GreaterThan(capsule),
                    guise + " stands inside the budget its adversaries answer to, so the hero could be "
                    + "held to it after all");

                square.Add(guise + " hides " + FigureFit.HiddenGroundOf(mesh, scale).ToString("0.#####")
                    + " squared on, " + turned.ToString("0.#####") + " turned, against the capsule's "
                    + capsule.ToString("0.#####"));
            }

            Assert.That(square.Count, Is.EqualTo(PlayerGuises.Count), string.Join("; ", square.ToArray()));

            foreach (var role in CharacterCast.Roles)
            {
                if (role == PartStyle.Start)
                {
                    continue;
                }

                foreach (var mesh in CharacterCast.MeshesOf(role))
                {
                    Assert.That(
                        FigureFit.TileReachOf(mesh, scale)
                            * IsoProjection.SightReach(FigureFit.StandingHeight(mesh, scale)),
                        Is.LessThan(capsule),
                        role + " wearing " + mesh);
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
        public void EverySilhouetteReachesATypicalRunSoTheWholeRampIsVisibleInPlay()
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
            Assert.That(worn.Count, Is.EqualTo(EnemyTier.Count));
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
