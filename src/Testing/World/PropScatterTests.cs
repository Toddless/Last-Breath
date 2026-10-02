namespace LastBreathTest.World
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Core.World.DualGrid;

    /// <summary>
    /// The scatter stores nothing: a meadow survives a reload only because the same cell always answers with the
    /// same props. These pins hold that promise from both sides — the layout must never drift, and it must never
    /// be so stable that neighbouring cells grow the same tuft in the same spot.
    /// </summary>
    [TestClass]
    public class PropScatterTests
    {
        private const float CellSize = 128f;

        private static PropScatterSettings Settings() => new()
        {
            CellSize = CellSize,
            Seed = 1337,
            MinPropsPerCell = 2,
            MaxPropsPerCell = 4,
            EdgeMargin = 24f,
            MinScale = 0.9f,
            MaxScale = 1.1f,
            TintJitter = 0.08f
        };

        private static IEnumerable<GridCoordinate> Square(int from, int to)
        {
            for (int x = from; x <= to; x++)
            {
                for (int y = from; y <= to; y++) yield return new GridCoordinate(x, y);
            }
        }

        /// <summary>Every field of every placement, in order — two layouts match only if they match exactly.</summary>
        private static string Signature(IReadOnlyList<PropPlacement> placements) =>
            string.Join(
                "|",
                placements.Select(placement => string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:R};{1:R};{2};{3:R};{4:R}",
                    placement.OffsetX,
                    placement.OffsetY,
                    placement.Mirrored,
                    placement.Scale,
                    placement.TintShift)));

        private static string SignatureOf(GridCoordinate cell, PropScatterSettings settings) =>
            Signature(PropScatter.PlacementsFor(cell, settings));

        [TestMethod]
        public void OneCellAnswersTheSameLayoutEveryTime()
        {
            PropScatterSettings settings = Settings();

            foreach (GridCoordinate cell in Square(-3, 3))
            {
                Assert.AreEqual(
                    SignatureOf(cell, settings),
                    SignatureOf(cell, settings),
                    $"cell {cell} grew a different meadow on the second ask");
            }
        }

        [TestMethod]
        public void EveryCellOfASweepGrowsItsOwnMeadow()
        {
            PropScatterSettings settings = Settings();
            List<GridCoordinate> cells = Square(-8, 7).ToList();

            HashSet<string> layouts = [.. cells.Select(cell => SignatureOf(cell, settings))];

            Assert.AreEqual(
                cells.Count,
                layouts.Count,
                "cells share a layout: the seed is not reaching both coordinates");
        }

        [TestMethod]
        public void NeighboursNeverMatch()
        {
            PropScatterSettings settings = Settings();

            foreach (GridCoordinate cell in Square(-4, 4))
            {
                string here = SignatureOf(cell, settings);
                Assert.AreNotEqual(here, SignatureOf(cell + new GridCoordinate(1, 0), settings), $"cell {cell} matches its right neighbour");
                Assert.AreNotEqual(here, SignatureOf(cell + new GridCoordinate(0, 1), settings), $"cell {cell} matches its lower neighbour");
                Assert.AreNotEqual(here, SignatureOf(cell + new GridCoordinate(1, 1), settings), $"cell {cell} matches its diagonal neighbour");
            }
        }

        /// <summary>Mirrored axes are the classic hash collision: (3, 7) must not answer what (7, 3) answers.</summary>
        [TestMethod]
        public void SwappedCoordinatesAreDifferentCells()
        {
            PropScatterSettings settings = Settings();

            foreach (GridCoordinate cell in Square(1, 6))
            {
                if (cell.X == cell.Y) continue;

                Assert.AreNotEqual(
                    SignatureOf(cell, settings),
                    SignatureOf(new GridCoordinate(cell.Y, cell.X), settings),
                    $"cell {cell} answers the same as its transpose");
            }
        }

        [TestMethod]
        public void CountStaysInsideTheConfiguredRange()
        {
            PropScatterSettings settings = Settings() with { MinPropsPerCell = 2, MaxPropsPerCell = 4 };
            HashSet<int> seen = [];

            foreach (GridCoordinate cell in Square(-10, 10))
            {
                int count = PropScatter.PlacementsFor(cell, settings).Count;
                Assert.IsTrue(count is >= 2 and <= 4, $"cell {cell} grew {count} props, outside the configured 2..4");
                seen.Add(count);
            }

            CollectionAssert.AreEquivalent(new[] { 2, 3, 4 }, seen.ToArray(), "the count never varies across the range");
        }

        [TestMethod]
        public void ASingleCountSettingGrowsExactlyThatMany()
        {
            PropScatterSettings settings = Settings() with { MinPropsPerCell = 3, MaxPropsPerCell = 3 };

            foreach (GridCoordinate cell in Square(-5, 5))
            {
                Assert.AreEqual(3, PropScatter.PlacementsFor(cell, settings).Count, $"cell {cell} ignored the fixed count");
            }
        }

        [TestMethod]
        public void RootsStayInsideTheCellMinusTheMargin()
        {
            PropScatterSettings settings = Settings();
            float low = settings.EdgeMargin;
            float high = CellSize - settings.EdgeMargin;

            foreach (GridCoordinate cell in Square(-12, 12))
            {
                foreach (PropPlacement placement in PropScatter.PlacementsFor(cell, settings))
                {
                    Assert.IsTrue(
                        placement.OffsetX >= low && placement.OffsetX <= high,
                        $"cell {cell} rooted a prop at x {placement.OffsetX}, outside [{low}, {high}]");
                    Assert.IsTrue(
                        placement.OffsetY >= low && placement.OffsetY <= high,
                        $"cell {cell} rooted a prop at y {placement.OffsetY}, outside [{low}, {high}]");
                }
            }
        }

        /// <summary>A margin wider than the cell cannot be honoured; the roots collapse onto the centre instead.</summary>
        [TestMethod]
        public void AnOversizedMarginPutsEveryRootOnTheCentre()
        {
            PropScatterSettings settings = Settings() with { EdgeMargin = 500f };

            foreach (GridCoordinate cell in Square(-4, 4))
            {
                foreach (PropPlacement placement in PropScatter.PlacementsFor(cell, settings))
                {
                    Assert.AreEqual(CellSize / 2f, placement.OffsetX, 0.001f, $"cell {cell} rooted off centre in x");
                    Assert.AreEqual(CellSize / 2f, placement.OffsetY, 0.001f, $"cell {cell} rooted off centre in y");
                }
            }
        }

        [TestMethod]
        public void ScaleAndTintStayInsideTheirRanges()
        {
            PropScatterSettings settings = Settings();

            foreach (GridCoordinate cell in Square(-12, 12))
            {
                foreach (PropPlacement placement in PropScatter.PlacementsFor(cell, settings))
                {
                    Assert.IsTrue(
                        placement.Scale >= settings.MinScale && placement.Scale <= settings.MaxScale,
                        $"cell {cell} scaled a prop to {placement.Scale}, outside [{settings.MinScale}, {settings.MaxScale}]");
                    Assert.IsTrue(
                        placement.TintShift >= -settings.TintJitter && placement.TintShift <= settings.TintJitter,
                        $"cell {cell} tinted a prop by {placement.TintShift}, outside +-{settings.TintJitter}");
                }
            }
        }

        [TestMethod]
        public void CopiesDifferInSizeMirrorAndShade()
        {
            PropScatterSettings settings = Settings();
            List<PropPlacement> placements = [.. Square(-6, 6).SelectMany(cell => PropScatter.PlacementsFor(cell, settings))];

            Assert.IsTrue(placements.Any(placement => placement.Mirrored), "no prop is ever mirrored");
            Assert.IsTrue(placements.Any(placement => !placement.Mirrored), "every prop is mirrored");
            Assert.IsTrue(placements.Select(placement => placement.Scale).Distinct().Count() > 1, "every prop is the same size");
            Assert.IsTrue(placements.Select(placement => placement.TintShift).Distinct().Count() > 1, "every prop is the same shade");
        }

        [TestMethod]
        public void ANewSeedRerollsTheWholeMeadow()
        {
            PropScatterSettings first = Settings() with { Seed = 1 };
            PropScatterSettings second = Settings() with { Seed = 2 };

            foreach (GridCoordinate cell in Square(-8, 8))
            {
                Assert.AreNotEqual(
                    SignatureOf(cell, first),
                    SignatureOf(cell, second),
                    $"cell {cell} kept its layout across a seed change");
            }
        }
    }
}
