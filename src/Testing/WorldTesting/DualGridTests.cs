namespace LastBreathTest.WorldTesting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.World.DualGrid;

    [TestClass]
    public class DualGridTests
    {
        private const int TileSize = 128;

        /// <summary>
        /// The frozen contract between the mask table and every transition sheet: swapping two rows here means
        /// repainting sixteen tiles, never editing this list. Read it against the placeholder atlas, where each
        /// tile carries its own mask number.
        /// </summary>
        private static readonly (TerrainCorner Mask, GridCoordinate Cell)[] s_pinnedAtlas =
        [
            (TerrainCorner.None, new GridCoordinate(0, 0)),
            (TerrainCorner.TopLeft, new GridCoordinate(1, 0)),
            (TerrainCorner.TopRight, new GridCoordinate(2, 0)),
            (TerrainCorner.TopLeft | TerrainCorner.TopRight, new GridCoordinate(3, 0)),
            (TerrainCorner.BottomLeft, new GridCoordinate(0, 1)),
            (TerrainCorner.TopLeft | TerrainCorner.BottomLeft, new GridCoordinate(1, 1)),
            (TerrainCorner.TopRight | TerrainCorner.BottomLeft, new GridCoordinate(2, 1)),
            (TerrainCorner.TopLeft | TerrainCorner.TopRight | TerrainCorner.BottomLeft, new GridCoordinate(3, 1)),
            (TerrainCorner.BottomRight, new GridCoordinate(0, 2)),
            (TerrainCorner.TopLeft | TerrainCorner.BottomRight, new GridCoordinate(1, 2)),
            (TerrainCorner.TopRight | TerrainCorner.BottomRight, new GridCoordinate(2, 2)),
            (TerrainCorner.TopLeft | TerrainCorner.TopRight | TerrainCorner.BottomRight, new GridCoordinate(3, 2)),
            (TerrainCorner.BottomLeft | TerrainCorner.BottomRight, new GridCoordinate(0, 3)),
            (TerrainCorner.TopLeft | TerrainCorner.BottomLeft | TerrainCorner.BottomRight, new GridCoordinate(1, 3)),
            (TerrainCorner.TopRight | TerrainCorner.BottomLeft | TerrainCorner.BottomRight, new GridCoordinate(2, 3)),
            (TerrainCorner.All, new GridCoordinate(3, 3))
        ];

        private static IEnumerable<TerrainCorner> AllMasks() =>
            Enumerable.Range(0, DualGridAtlas.MaskCount).Select(index => (TerrainCorner)index);

        private static IEnumerable<GridCoordinate> Square(int from, int to)
        {
            for (int x = from; x <= to; x++)
            {
                for (int y = from; y <= to; y++) yield return new GridCoordinate(x, y);
            }
        }

        private static List<KeyValuePair<GridCoordinate, TerrainCorner>> DisplayMap(DualGridProjection projection) =>
            projection.DisplayCells()
                .Select(cell => new KeyValuePair<GridCoordinate, TerrainCorner>(cell, projection.MaskAt(cell)))
                .OrderBy(entry => entry.Key.X)
                .ThenBy(entry => entry.Key.Y)
                .ToList();

        [TestMethod]
        public void AtlasTableCoversEverySixteenthMask()
        {
            List<GridCoordinate> coordinates = AllMasks().Select(DualGridAtlas.CoordinateOf).ToList();

            Assert.AreEqual(DualGridAtlas.MaskCount, coordinates.Count);
            Assert.IsTrue(coordinates.All(c => c.X >= 0 && c.X < DualGridAtlas.Columns), "atlas column out of range");
            Assert.IsTrue(coordinates.All(c => c.Y >= 0 && c.Y < DualGridAtlas.Rows), "atlas row out of range");
        }

        [TestMethod]
        public void AtlasTableGivesEveryMaskItsOwnTile()
        {
            List<GridCoordinate> coordinates = AllMasks().Select(DualGridAtlas.CoordinateOf).ToList();

            Assert.AreEqual(DualGridAtlas.MaskCount, coordinates.Distinct().Count());
        }

        [TestMethod]
        public void EveryMaskKeepsTheAtlasCellTheSheetWasDrawnFor()
        {
            Assert.AreEqual(DualGridAtlas.MaskCount, s_pinnedAtlas.Length, "the pin must name every mask");

            foreach ((TerrainCorner mask, GridCoordinate cell) in s_pinnedAtlas)
            {
                Assert.AreEqual(cell, DualGridAtlas.CoordinateOf(mask), $"mask {(int)mask} moved in the atlas");
            }
        }

        [TestMethod]
        public void AtlasRejectsAMaskOutsideTheFourCornerBits()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => DualGridAtlas.CoordinateOf((TerrainCorner)DualGridAtlas.MaskCount));
        }

        [TestMethod]
        public void DisplayTileCoversTheFourWorldCellsAroundItsCentre()
        {
            var display = new GridCoordinate(0, 0);

            Assert.AreEqual(new GridCoordinate(-1, -1), DualGridLayout.WorldCellOf(display, TerrainCorner.TopLeft));
            Assert.AreEqual(new GridCoordinate(0, -1), DualGridLayout.WorldCellOf(display, TerrainCorner.TopRight));
            Assert.AreEqual(new GridCoordinate(-1, 0), DualGridLayout.WorldCellOf(display, TerrainCorner.BottomLeft));
            Assert.AreEqual(new GridCoordinate(0, 0), DualGridLayout.WorldCellOf(display, TerrainCorner.BottomRight));
        }

        [TestMethod]
        public void WorldAndDisplayAddressesRoundTripAcrossZeroAndNegatives()
        {
            foreach (GridCoordinate cell in Square(-3, 3))
            {
                foreach (TerrainCorner corner in DualGridLayout.Corners)
                {
                    Assert.AreEqual(cell, DualGridLayout.DisplayCellOf(DualGridLayout.WorldCellOf(cell, corner), corner));
                    Assert.AreEqual(cell, DualGridLayout.WorldCellOf(DualGridLayout.DisplayCellOf(cell, corner), corner));
                }
            }
        }

        [TestMethod]
        public void WorldCellEditInvalidatesExactlyFourDisplayCells()
        {
            foreach (GridCoordinate cell in Square(-3, 3))
            {
                var projection = new DualGridProjection();

                IReadOnlyCollection<GridCoordinate> dirty = projection.SetCell(cell, present: true);

                Assert.AreEqual(DualGridLayout.DisplayCellsPerWorldCell, dirty.Count, $"world cell {cell}");
            }
        }

        [TestMethod]
        public void InvalidatedDisplayCellsAreTheOnesActuallyCoveringTheWorldCell()
        {
            var world = new GridCoordinate(-1, -1);
            var projection = new DualGridProjection();

            IReadOnlyCollection<GridCoordinate> dirty = projection.SetCell(world, present: true);

            CollectionAssert.AreEquivalent(
                new[]
                {
                    new GridCoordinate(-1, -1),
                    new GridCoordinate(0, -1),
                    new GridCoordinate(-1, 0),
                    new GridCoordinate(0, 0)
                },
                dirty.ToArray());
            Assert.IsTrue(dirty.All(display => projection.MaskAt(display) != TerrainCorner.None));
        }

        [TestMethod]
        public void ASingleWorldCellShowsUpInEachDisplayTileUnderItsOwnCornerBit()
        {
            var world = new GridCoordinate(0, 0);
            var projection = new DualGridProjection();
            projection.SetCell(world, present: true);

            foreach (TerrainCorner corner in DualGridLayout.Corners)
            {
                Assert.AreEqual(corner, projection.MaskAt(DualGridLayout.DisplayCellOf(world, corner)));
            }
        }

        [TestMethod]
        public void MaskBitsFollowTheCellsThatAreActuallyPresent()
        {
            var projection = new DualGridProjection();
            projection.SetCell(new GridCoordinate(-1, -1), present: true);
            projection.SetCell(new GridCoordinate(0, 0), present: true);

            Assert.AreEqual(TerrainCorner.TopLeft | TerrainCorner.BottomRight, projection.MaskAt(new GridCoordinate(0, 0)));
            Assert.AreEqual(TerrainCorner.None, projection.MaskAt(new GridCoordinate(4, 4)));
        }

        [TestMethod]
        public void WritingTheValueACellAlreadyHoldsInvalidatesNothing()
        {
            var projection = new DualGridProjection();
            projection.SetCell(new GridCoordinate(2, -2), present: true);

            Assert.AreEqual(0, projection.SetCell(new GridCoordinate(2, -2), present: true).Count);
            Assert.AreEqual(0, projection.SetCell(new GridCoordinate(5, 5), present: false).Count);
        }

        [TestMethod]
        public void ErasingAWorldCellClearsTheDisplayTilesItHeld()
        {
            var world = new GridCoordinate(-2, 3);
            var projection = new DualGridProjection();
            projection.SetCell(world, present: true);

            IReadOnlyCollection<GridCoordinate> dirty = projection.SetCell(world, present: false);

            Assert.AreEqual(DualGridLayout.DisplayCellsPerWorldCell, dirty.Count);
            Assert.IsTrue(dirty.All(display => projection.MaskAt(display) == TerrainCorner.None));
            Assert.AreEqual(0, projection.DisplayCells().Count);
        }

        [TestMethod]
        public void ReconcileReportsOnlyWhatChangedSinceTheLastSnapshot()
        {
            List<GridCoordinate> cells = Square(-2, 2).ToList();
            var projection = new DualGridProjection();
            projection.Reconcile(cells);

            Assert.AreEqual(0, projection.Reconcile(cells).Count, "an unchanged world must not repaint anything");

            cells.Add(new GridCoordinate(9, -9));

            Assert.AreEqual(DualGridLayout.DisplayCellsPerWorldCell, projection.Reconcile(cells).Count);
        }

        [TestMethod]
        public void AFullRebuildMatchesTheSameEditsAppliedOneByOne()
        {
            List<GridCoordinate> cells = Square(-3, 3).Where(cell => (cell.X + cell.Y) % 3 != 0).ToList();
            var rebuilt = new DualGridProjection();
            rebuilt.Reconcile(cells);

            var incremental = new DualGridProjection();
            foreach (GridCoordinate cell in cells.OrderByDescending(cell => cell.X * 31 + cell.Y))
            {
                incremental.SetCell(cell, present: true);
            }

            CollectionAssert.AreEqual(DisplayMap(rebuilt), DisplayMap(incremental));
        }

        [TestMethod]
        public void PointEditsReportEveryDisplayCellAFullRebuildWouldPaint()
        {
            List<GridCoordinate> cells = Square(-3, 0).ToList();
            var projection = new DualGridProjection();
            HashSet<GridCoordinate> reported = [];

            foreach (GridCoordinate cell in cells) reported.UnionWith(projection.SetCell(cell, present: true));

            CollectionAssert.AreEquivalent(projection.DisplayCells().ToArray(), reported.ToArray());
        }

        [TestMethod]
        public void TheDisplayLayerSitsHalfACellUpAndLeft()
        {
            Assert.AreEqual(-TileSize / 2f, DualGridLayout.DisplayPixelOffset(TileSize));
        }
    }
}
