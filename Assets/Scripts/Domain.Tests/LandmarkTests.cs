using System;
using System.Collections.Generic;
using System.Linq;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class LandmarkTests
    {
        const int Seeds = 120;

        const int StressSeeds = 20;

        const float Tolerance = 1e-4f;

        const float ReadableShare = 0.08f;

        static readonly Dictionary<string, List<LevelGraph>> SweepByPreset =
            new Dictionary<string, List<LevelGraph>>();

        static IEnumerable<MazePreset> EveryPreset()
        {
            yield return MazePreset.Tiny;
            yield return MazePreset.Ship;
        }

        static IEnumerable<MazePreset> EveryPlayedPreset()
        {
            yield return MazePreset.Ship;
            yield return MazePreset.Stress;
        }

        static int Places(LevelGraph graph)
        {
            var places = 0;

            foreach (var tile in graph.Tiles.Tiles)
            {
                TileSide open;

                if (graph.Decisions.NodeAt(tile.Position) != null
                    || TileFootings.Under(graph.Tiles, tile.Position) == TileFooting.Flight
                    || !Landmarks.TryOpenSide(graph.Tiles, tile.Position, out open))
                {
                    continue;
                }

                if (Landmarks.MarksADecision(graph.Tiles, tile.Position)
                    || Landmarks.Flanks(graph.Tiles, tile.Position))
                {
                    places++;
                }
            }

            return places;
        }

        static IEnumerable<LandmarkKind> EveryKind()
        {
            return LandmarkForm.Kinds;
        }

        static int SeedsOf(MazePreset preset)
        {
            return preset == MazePreset.Stress ? StressSeeds : Seeds;
        }

        static List<LevelGraph> Sweep(MazePreset preset)
        {
            List<LevelGraph> sweep;
            if (SweepByPreset.TryGetValue(preset.Name, out sweep))
            {
                return sweep;
            }

            var seeds = SeedsOf(preset);
            sweep = new List<LevelGraph>(seeds);
            for (var seed = 1; seed <= seeds; seed++)
            {
                sweep.Add(LevelGenerator.Generate(seed, preset).Graph);
            }

            SweepByPreset.Add(preset.Name, sweep);
            return sweep;
        }

        [TestCaseSource(nameof(EveryPlayedPreset))]
        public void EveryLevelThePlayerIsDealtCarriesThreeToFiveLandmarks(MazePreset preset)
        {
            var fewest = int.MaxValue;
            var most = 0;

            foreach (var graph in Sweep(preset))
            {
                var standing = Landmarks.Of(graph).Count;

                fewest = Math.Min(fewest, standing);
                most = Math.Max(most, standing);

                Assert.That(
                    standing,
                    Is.InRange(Landmarks.Fewest, Landmarks.Most),
                    "Seed " + graph.Seed + " of " + preset + " raised " + standing + " landmarks.");
            }

            Console.WriteLine(
                "  " + preset + ": between " + fewest + " and " + most + " landmarks over "
                + SeedsOf(preset) + " seeds");
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void ALevelNeverCarriesMoreThanFiveNorFewerThanThePlacesItOffers(MazePreset preset)
        {
            var starved = 0;

            foreach (var graph in Sweep(preset))
            {
                var standing = Landmarks.Of(graph).Count;
                var places = Places(graph);

                if (places < Landmarks.Fewest)
                {
                    starved++;
                }

                Assert.That(standing, Is.LessThanOrEqualTo(Landmarks.Most));
                Assert.That(
                    standing,
                    Is.GreaterThanOrEqualTo(Math.Min(Landmarks.Fewest, places)),
                    "Seed " + graph.Seed + " of " + preset + " left " + places
                    + " places unmarked and raised " + standing + " landmarks.");
            }

            Console.WriteLine(
                "  " + preset + ": " + starved + " of " + SeedsOf(preset)
                + " seeds offer fewer than " + Landmarks.Fewest + " places worth marking");
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void ALandmarkAlwaysMarksAJunctionOrAnAreaEntranceRatherThanACorridor(MazePreset preset)
        {
            var junctions = 0;
            var beside = 0;
            var entrances = 0;
            var flanking = 0;

            foreach (var graph in Sweep(preset))
            {
                foreach (var spot in Landmarks.Of(graph))
                {
                    var junction = Landmarks.IsJunction(graph.Tiles, spot.Tile);
                    var entrance = Landmarks.IsAreaEntrance(graph.Tiles, spot.Tile);
                    var besideAJunction = Landmarks.FlanksAJunction(graph.Tiles, spot.Tile);
                    var flank = Landmarks.Flanks(graph.Tiles, spot.Tile);

                    Assert.That(
                        junction || entrance || flank,
                        Is.True,
                        "Seed " + graph.Seed + " stood a " + spot + " in the middle of a corridor.");

                    if (junction)
                    {
                        junctions++;
                    }
                    else if (entrance)
                    {
                        entrances++;
                    }
                    else if (besideAJunction)
                    {
                        beside++;
                    }
                    else
                    {
                        flanking++;
                    }
                }
            }

            Console.WriteLine(
                "  " + preset + ": " + junctions + " on a junction, " + beside
                + " beside one, " + entrances + " on an area entrance, " + flanking
                + " beside an entrance");
            Assert.That(junctions + beside, Is.GreaterThan(0), "No landmark marks a junction at all.");
            Assert.That(junctions + beside + entrances, Is.GreaterThan(flanking));
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void NoLandmarkTileCarriesADecisionNode(MazePreset preset)
        {
            foreach (var graph in Sweep(preset))
            {
                foreach (var spot in Landmarks.Of(graph))
                {
                    Assert.That(
                        graph.Decisions.NodeAt(spot.Tile),
                        Is.Null,
                        "Seed " + graph.Seed + " stood a " + spot + " on a decision node.");
                }
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void NoLandmarkEverCarriesANodeNameOrANodeStyle(MazePreset preset)
        {
            foreach (var graph in Sweep(preset))
            {
                var nodeNames = new HashSet<string>(
                    graph.Decisions.Nodes.Select(node => PartNames.Node(node.Id)));

                foreach (var terrace in LevelBlueprintBuilder.Build(graph).Terraces)
                {
                    foreach (var part in terrace.Landmarks)
                    {
                        Assert.That(part.Style, Is.EqualTo(PartStyle.Landmark));
                        Assert.That(nodeNames, Has.No.Member(part.Name));
                        Assert.That(terrace.Nodes.Select(node => node.Name), Has.No.Member(part.Name));
                    }
                }
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void NothingTheFingerCanAimAtEverStandsOnALandmarkTile(MazePreset preset)
        {
            foreach (var graph in Sweep(preset))
            {
                var tiles = new HashSet<TilePosition>(Landmarks.Of(graph).Select(spot => spot.Tile));
                var state = RunState.Begin(graph, LevelPlan.For(MazePreset.Named(graph.Preset), 1).StartingPower);

                foreach (var nodeId in TapAim.Aimable(state))
                {
                    Assert.That(
                        tiles,
                        Has.No.Member(graph.Decisions.Node(nodeId).Position),
                        "Seed " + graph.Seed + " aims at node " + nodeId + " on a landmark tile.");
                }
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void NoTwoLandmarksInOneLevelAreTheSameKind(MazePreset preset)
        {
            foreach (var graph in Sweep(preset))
            {
                var kinds = Landmarks.Of(graph).Select(spot => spot.Kind).ToList();

                Assert.That(
                    kinds.Distinct().Count(),
                    Is.EqualTo(kinds.Count),
                    "Seed " + graph.Seed + " raised two landmarks of one kind.");
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void PlacementIsTheSameEveryTimeTheSameSeedIsRead(MazePreset preset)
        {
            foreach (var graph in Sweep(preset))
            {
                var again = LevelGenerator.Generate(graph.Seed, preset).Graph;

                Assert.That(Landmarks.Of(again), Is.EqualTo(Landmarks.Of(graph)));
                Assert.That(Landmarks.Of(graph), Is.EqualTo(Landmarks.Of(graph)));
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void ALandmarkNeverStandsOnAStaircaseFlight(MazePreset preset)
        {
            foreach (var graph in Sweep(preset))
            {
                foreach (var spot in Landmarks.Of(graph))
                {
                    Assert.That(
                        TileFootings.Under(graph.Tiles, spot.Tile),
                        Is.Not.EqualTo(TileFooting.Flight),
                        "Seed " + graph.Seed + " stood a " + spot + " on a flight of stairs.");
                }
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void ALandmarkStandsAgainstASideNoTileLiesBeyond(MazePreset preset)
        {
            foreach (var graph in Sweep(preset))
            {
                foreach (var spot in Landmarks.Of(graph))
                {
                    var beyond = TileSides.Step(spot.Tile, spot.Against);

                    Assert.That(
                        graph.Tiles.ContainsPlace(beyond.X, beyond.Y),
                        Is.False,
                        "Seed " + graph.Seed + " leaned a " + spot + " into the tile next door.");
                }
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void NoLandmarkReachesTheLineTheFigureWalksAlong(MazePreset preset)
        {
            var figure = FigureFit.BoxSpreadOf(
                CharacterCast.MeshOf(PartStyle.Start), LevelBlueprintBuilder.FigureScale) * 0.5f;
            var tightest = float.MaxValue;

            foreach (var graph in Sweep(preset))
            {
                foreach (var spot in Landmarks.Of(graph))
                {
                    var standing = Landmarks.StandingOf(spot);

                    foreach (var tile in graph.Tiles.Tiles)
                    {
                        var centre = IsoProjection.Of(tile.Position);
                        var apart = Math.Max(
                            Math.Abs(standing.X - centre.X), Math.Abs(standing.Z - centre.Z));

                        tightest = Math.Min(tightest, apart - LandmarkForm.ReachOf(spot.Kind));
                    }
                }
            }

            Console.WriteLine(
                "  " + preset + ": the tightest landmark leaves " + tightest.ToString("0.####")
                + " of clearance around a walked tile centre, against the figure's "
                + figure.ToString("0.####") + " half-spread");

            Assert.That(
                tightest,
                Is.GreaterThan(figure),
                "A landmark reaches into the line the figure walks along, so it blocks the walk.");
        }

        [Test]
        public void TheClearanceALandmarkLeavesIsWiderThanTheFigureThatWalksPastIt()
        {
            var figure = FigureFit.BoxSpreadOf(
                CharacterCast.MeshOf(PartStyle.Start), LevelBlueprintBuilder.FigureScale) * 0.5f;

            Assert.That(Landmarks.Clearance, Is.GreaterThan(figure));
            Assert.That(LandmarkForm.Reach, Is.EqualTo(LandmarkForm.Span * 0.5f));
        }

        [TestCaseSource(nameof(EveryKind))]
        public void EveryKindIsCutInsideTheBoxTheBuilderLaysItOutIn(LandmarkKind kind)
        {
            var pieces = LandmarkForm.Pieces(kind);

            Assert.That(pieces, Is.Not.Empty);
            Assert.That(
                LandmarkForm.ReachOf(kind),
                Is.LessThanOrEqualTo(LandmarkForm.Reach + Tolerance),
                kind + " spills out of the span the walk clearance is measured against.");
            Assert.That(
                LandmarkForm.StandingHeightOf(kind),
                Is.LessThanOrEqualTo(LandmarkForm.Height + Tolerance),
                kind + " stands taller than the box its part is sized by.");
        }

        [TestCaseSource(nameof(EveryKind))]
        public void EveryKindIsPackMeshesAloneAndRestsOnTheTileFloor(LandmarkKind kind)
        {
            var floor = -LandmarkForm.Height * 0.5f;
            var lowest = float.MaxValue;

            foreach (var piece in LandmarkForm.Pieces(kind))
            {
                Assert.That(piece.Model, Is.Not.EqualTo(PartModel.None), piece.ToString());
                Assert.That(ArtPacks.Of(piece.Model), Is.EqualTo(ArtPack.Dungeon), piece.ToString());
                Assert.That(piece.Shape, Is.EqualTo(PartShape.Landmark), piece.ToString());
                Assert.That(piece.Style, Is.EqualTo(PartStyle.Landmark), piece.ToString());
                Assert.That(PartNames.IsLandmarkPiece(piece.Name), Is.True, piece.ToString());

                lowest = Math.Min(lowest, piece.Position.Y - piece.Scale.Y * 0.5f);
            }

            var names = LandmarkForm.Pieces(kind).Select(piece => piece.Name).ToList();

            Assert.That(names.Distinct().Count(), Is.EqualTo(names.Count));
            Assert.That(lowest, Is.EqualTo(floor).Within(Tolerance), kind + " floats above its tile.");
        }

        [TestCaseSource(nameof(EveryKind))]
        public void EveryPieceIsSizedByTheProportionsOfTheMeshItWears(LandmarkKind kind)
        {
            foreach (var piece in LandmarkForm.Pieces(kind))
            {
                var mesh = DungeonPack.HeightOf(piece.Model);

                Assert.That(
                    piece.Scale.X,
                    Is.EqualTo(DungeonPack.WidthOf(piece.Model) * piece.Scale.Y / mesh).Within(Tolerance),
                    piece.ToString());
                Assert.That(
                    piece.Scale.Z,
                    Is.EqualTo(DungeonPack.DepthOf(piece.Model) * piece.Scale.Y / mesh).Within(Tolerance),
                    piece.ToString());

                var fit = ModelPose.ScaleOf(piece);

                Assert.That(fit.X, Is.EqualTo(fit.Y).Within(Tolerance), piece + " is stretched across.");
                Assert.That(fit.Z, Is.EqualTo(fit.Y).Within(Tolerance), piece + " is stretched through.");
            }
        }

        [TestCaseSource(nameof(EveryKind))]
        public void EveryPieceStandsOnTheOneBelowItWithNoGapAndNoOverlap(LandmarkKind kind)
        {
            var pieces = LandmarkForm.Pieces(kind);
            var laid = -LandmarkForm.Height * 0.5f;

            Assert.That(pieces.Count, Is.GreaterThan(1), kind + " is a single mesh rather than a build.");

            foreach (var piece in pieces)
            {
                Assert.That(
                    piece.Position.Y - piece.Scale.Y * 0.5f,
                    Is.EqualTo(laid).Within(Tolerance),
                    piece + " does not rest on the course below it.");

                laid += piece.Scale.Y;
            }

            Assert.That(
                laid + LandmarkForm.Height * 0.5f,
                Is.EqualTo(LandmarkForm.StandingHeightOf(kind)).Within(Tolerance));
        }

        [TestCaseSource(nameof(EveryKind))]
        public void TheReachOfAKindCoversTheMeshEvenWhereItSitsOffItsOwnPivot(LandmarkKind kind)
        {
            var reach = LandmarkForm.ReachOf(kind);

            foreach (var piece in LandmarkForm.Pieces(kind))
            {
                var off = LandmarkForm.OffCentreOf(piece);
                var turn = piece.Rotation.Y * Math.PI / 180.0;
                var across = Math.Abs(Math.Cos(turn));
                var along = Math.Abs(Math.Sin(turn));
                var sideways = piece.Scale.X * 0.5f * across + piece.Scale.Z * 0.5f * along;
                var forwards = piece.Scale.X * 0.5f * along + piece.Scale.Z * 0.5f * across;

                Assert.That(
                    Math.Abs(piece.Position.X + off.X) + sideways,
                    Is.LessThanOrEqualTo(reach + Tolerance),
                    piece + " spills past the reach its kind promises.");
                Assert.That(
                    Math.Abs(piece.Position.Z + off.Z) + forwards,
                    Is.LessThanOrEqualTo(reach + Tolerance),
                    piece + " spills past the reach its kind promises.");
            }

            Assert.That(
                reach,
                Is.LessThanOrEqualTo(LandmarkForm.Reach + Tolerance),
                kind + " claims more ground than the placement budgets for.");
        }

        [Test]
        public void AMeshThatSitsSquareOnItsPivotIsNeverShiftedOffTheLandmarkAxis()
        {
            var centred = 0;

            foreach (var kind in LandmarkForm.Kinds)
            {
                foreach (var piece in LandmarkForm.Pieces(kind))
                {
                    if (DungeonPack.PackShiftAcrossOf(piece.Model) != 0f
                        || DungeonPack.PackShiftAlongOf(piece.Model) != 0f)
                    {
                        continue;
                    }

                    var off = LandmarkForm.OffCentreOf(piece);

                    Assert.That(off.X, Is.EqualTo(0f).Within(Tolerance), piece.ToString());
                    Assert.That(off.Z, Is.EqualTo(0f).Within(Tolerance), piece.ToString());
                    centred++;
                }
            }

            Assert.That(centred, Is.GreaterThan(0));
        }

        [TestCaseSource(nameof(EveryKind))]
        public void TheMeshOfEveryPieceStandsOnTheCourseTheStackPutItOn(LandmarkKind kind)
        {
            foreach (var piece in LandmarkForm.Pieces(kind))
            {
                var posed = ModelPose.PositionOf(piece);
                var fit = ModelPose.ScaleOf(piece);
                var foot = posed.Y + DungeonPack.BaseOf(piece.Model) * fit.Y;

                Assert.That(
                    foot,
                    Is.EqualTo(piece.Position.Y - piece.Scale.Y * 0.5f).Within(Tolerance),
                    piece + " sinks below the course it was laid on or floats above it.");
            }
        }

        [TestCaseSource(nameof(EveryKind))]
        public void EveryKindStandsOverTheWallsItIsMeantToBeSeenAcross(LandmarkKind kind)
        {
            Assert.That(
                LandmarkForm.StandingHeightOf(kind),
                Is.GreaterThan(IsoProjection.WallHeight),
                kind + " hides behind the masonry it stands against.");
        }

        [TestCaseSource(nameof(EveryKind))]
        public void EveryKindFillsEnoughOfThePlayFramingToBeRecognised(LandmarkKind kind)
        {
            var share = LevelFraming.ShareOfScreen(
                LandmarkForm.StandingHeightOf(kind), LevelFraming.PlaySize);

            Console.WriteLine(
                "  " + kind + " stands " + LandmarkForm.StandingHeightOf(kind).ToString("0.##")
                + " tall, " + share.ToString("0.###") + " of the play framing against the figure's "
                + LevelFraming.FigureHeightFraction.ToString("0.###"));

            Assert.That(
                share,
                Is.GreaterThanOrEqualTo(ReadableShare),
                kind + " is smaller on screen than the figure the framing is cut to.");
            Assert.That(share, Is.GreaterThan(LevelFraming.FigureHeightFraction));
        }

        [Test]
        public void NoTwoKindsShareASilhouetteOrAColour()
        {
            var crowns = new List<Tint>();
            var profiles = new List<string>();

            foreach (var kind in LandmarkForm.Kinds)
            {
                crowns.Add(LandmarkLook.Of(kind));
                profiles.Add(Profile(kind));
            }

            Assert.That(crowns.Distinct().Count(), Is.EqualTo(crowns.Count), "Two kinds glow the same colour.");
            Assert.That(
                profiles.Distinct().Count(),
                Is.EqualTo(profiles.Count),
                "Two kinds are stacked the same way, so they read as one thing: "
                + string.Join(" / ", profiles.ToArray()));

            for (var one = 0; one < crowns.Count; one++)
            {
                for (var other = one + 1; other < crowns.Count; other++)
                {
                    Assert.That(
                        Apart(crowns[one], crowns[other]),
                        Is.GreaterThan(0.3f),
                        LandmarkForm.Kinds[one] + " and " + LandmarkForm.Kinds[other]
                        + " are near enough in colour to be mistaken for each other.");
                }
            }
        }

        [Test]
        public void NoTwoKindsAreCrownedWithTheSameMesh()
        {
            var crowns = new List<PartModel>();

            foreach (var kind in LandmarkForm.Kinds)
            {
                var pieces = LandmarkForm.Pieces(kind);
                crowns.Add(pieces[pieces.Count - 1].Model);
            }

            Assert.That(
                crowns.Distinct().Count(),
                Is.EqualTo(crowns.Count),
                "Two kinds end in the same mesh, so the piece a glance lands on first is the same: "
                + string.Join(" / ", crowns.Select(model => model.ToString()).ToArray()));
        }

        [Test]
        public void ALandmarkPartIsAnEmptyMarkerRatherThanAProp()
        {
            var graph = LevelGenerator.Generate(LevelGraphFixture.Seed, MazePreset.Ship).Graph;
            var blueprint = LevelBlueprintBuilder.Build(graph);
            var spots = Landmarks.Of(graph);
            var raised = blueprint.Terraces.SelectMany(terrace => terrace.Landmarks).ToList();

            Assert.That(raised.Count, Is.EqualTo(spots.Count));

            foreach (var spot in spots)
            {
                var part = raised.First(candidate => candidate.Name == PartNames.Landmark(spot.Tile));

                Assert.That(part.Shape, Is.EqualTo(PartShape.Landmark));
                Assert.That(part.Model, Is.EqualTo(PartModel.None));
                Assert.That(part.Style, Is.EqualTo(PartStyle.Landmark));
                Assert.That(
                    part.Position.Y,
                    Is.EqualTo(IsoProjection.Of(spot.Tile).Y + LandmarkForm.Height * 0.5f).Within(Tolerance));
                Assert.That(part.Rotation.Y, Is.EqualTo(TileSides.InwardYaw(spot.Against)));
            }
        }

        [Test]
        public void EveryLandmarkHangsUnderTheTerraceItsTileStandsOn()
        {
            var graph = LevelGenerator.Generate(LevelGraphFixture.Seed, MazePreset.Ship).Graph;
            var blueprint = LevelBlueprintBuilder.Build(graph);
            var placed = 0;

            foreach (var terrace in blueprint.Terraces)
            {
                foreach (var part in terrace.Landmarks)
                {
                    var spot = Landmarks.Of(graph)
                        .First(candidate => PartNames.Landmark(candidate.Tile) == part.Name);

                    Assert.That(
                        Terraces.ElevationOf(Terraces.TerraceUnder(spot.Tile.Elevation)),
                        Is.EqualTo(terrace.Elevation));
                    placed++;
                }
            }

            Assert.That(placed, Is.InRange(Landmarks.Fewest, Landmarks.Most));
        }

        [Test]
        public void ALevelWithNothingToMarkRaisesNoLandmarkRatherThanThrowing()
        {
            var grid = new TileGrid(
                new[]
                {
                    new Tile(new TilePosition(elevation: 0, x: 0, y: 0), regionId: 0),
                    new Tile(new TilePosition(elevation: 0, x: 1, y: 0), regionId: 0)
                });
            var decisions = new DecisionGraph(
                new[]
                {
                    new DecisionNode(0, new TilePosition(elevation: 0, x: 0, y: 0), NodeType.Start, 0),
                    new DecisionNode(1, new TilePosition(elevation: 0, x: 1, y: 0), NodeType.Boss, 1)
                },
                new[] { new Corridor(0, 1, Array.Empty<TilePosition>()) });

            Assert.That(Landmarks.Of(new LevelGraph(1, "tiny", grid, decisions)), Is.Empty);
        }

        static string Profile(LandmarkKind kind)
        {
            return string.Join(
                "-",
                LandmarkForm.Pieces(kind)
                    .Select(piece => piece.Model + ":" + piece.Scale.Y.ToString("0.##"))
                    .ToArray());
        }

        static float Apart(Tint one, Tint other)
        {
            return Math.Abs(one.Red - other.Red)
                + Math.Abs(one.Green - other.Green)
                + Math.Abs(one.Blue - other.Blue);
        }
    }
}
