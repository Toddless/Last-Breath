namespace LastBreathTest.BattleSystemTests
{
    using Core.PassiveTree.View;

    /// <summary>
    /// The arithmetic behind every click that has to hit what the eye aimed at. It moved out of the
    /// authoring tool into Core so the game's wheel and the tool share it, and a move is only safe if
    /// the answers did not change — these are the answers, written down.
    /// <para>The two multipliers do not mean the same thing and that is the whole point of the class:
    /// the spread stretches DISTANCES and leaves sizes alone, the zoom scales everything. A test that
    /// only ever looked at zoom 1 and spread 1 would pass on a class that confused them.</para>
    /// </summary>
    [TestClass]
    public class CanvasTransformTests
    {
        private const float Tolerance = 0.001f;

        [TestMethod]
        public void APixelAndACoordinate_AreTheSamePointBothWaysRound()
        {
            var view = new CanvasTransform();
            view.ZoomBy(2f, 0f, 0f);
            view.SetSpread(2.5f, 0f, 0f);

            // The pan is set last on purpose: zooming and spreading anchor the view and would move it,
            // and a fixture that ended up panned to the origin would pass on a transform that lost the
            // pan on the way back.
            view.SetPan(120f, -40f);

            Assert.AreEqual(320f, view.DocumentX(view.ScreenX(320f)), Tolerance);
            Assert.AreEqual(-75f, view.DocumentY(view.ScreenY(-75f)), Tolerance);
        }

        [TestMethod]
        public void ASizeTravelsThroughTheZoomAndAPositionThroughBothMultipliers()
        {
            var view = new CanvasTransform();
            view.ZoomBy(2f, 0f, 0f);
            view.SetSpread(3f, 0f, 0f);

            Assert.AreEqual(2f, view.Zoom, Tolerance);
            Assert.AreEqual(6f, view.PositionScale, Tolerance, "a position travels through the zoom AND the spread");
            Assert.AreEqual(2f, view.DocumentLength(12f), Tolerance, "a screen length is worth less document the wider it is spread");
        }

        [TestMethod]
        public void ZoomingAboutAPoint_LeavesTheDocumentUnderThatPixel()
        {
            var view = new CanvasTransform();
            view.SetPan(300f, 200f);

            const float pivotX = 640f;
            const float pivotY = 360f;
            float before = view.DocumentX(pivotX);
            float beforeY = view.DocumentY(pivotY);

            view.ZoomBy(CanvasTransform.ZoomStep, pivotX, pivotY);

            Assert.AreEqual(before, view.DocumentX(pivotX), Tolerance, "the wheel jumped instead of magnifying");
            Assert.AreEqual(beforeY, view.DocumentY(pivotY), Tolerance);
        }

        [TestMethod]
        public void SpreadingAboutAPoint_LeavesTheDocumentUnderThatPixel_AndSnapsToItsOwnStep()
        {
            var view = new CanvasTransform();
            view.SetPan(300f, 200f);
            float before = view.DocumentX(500f);

            Assert.IsTrue(view.SetSpread(2.06f, 500f, 200f));

            Assert.AreEqual(2f, view.Spread, Tolerance, "the spread is stored on its own quarter-step grid");
            Assert.AreEqual(before, view.DocumentX(500f), Tolerance);
            Assert.IsFalse(view.SetSpread(2.01f, 500f, 200f), "a press that changes nothing must not be reported as a change");
        }

        [TestMethod]
        public void TheZoomIsClamped_AndAJumpToANodeOnlyEverRaisesIt()
        {
            var view = new CanvasTransform();

            for (int step = 0; step < 100; step++) view.ZoomBy(CanvasTransform.ZoomStep, 0f, 0f);
            Assert.AreEqual(CanvasTransform.MaxZoom, view.Zoom, Tolerance);

            for (int step = 0; step < 200; step++) view.ZoomBy(1f / CanvasTransform.ZoomStep, 0f, 0f);
            Assert.AreEqual(CanvasTransform.MinZoom, view.Zoom, Tolerance);

            view.RaiseZoomTo(1f);
            Assert.AreEqual(1f, view.Zoom, Tolerance, "arriving on a node two pixels across is not arriving anywhere");

            view.RaiseZoomTo(0.5f);
            Assert.AreEqual(1f, view.Zoom, Tolerance, "framing a node threw away the close-up somebody was working in");
        }

        [TestMethod]
        public void FramingABox_PutsAllOfItOnScreenAndCentresIt()
        {
            const float width = 800f;
            const float height = 600f;
            const float padding = 100f;
            var view = new CanvasTransform();

            view.Fit(-400f, -300f, 400f, 300f, width, height, padding);

            Assert.AreEqual(width * 0.5f, view.ScreenX(0f), Tolerance, "the middle of the box is not in the middle of the view");
            Assert.AreEqual(height * 0.5f, view.ScreenY(0f), Tolerance);
            Assert.IsTrue(view.ScreenX(-400f) >= 0f && view.ScreenX(400f) <= width, "the box does not fit across");
            Assert.IsTrue(view.ScreenY(-300f) >= 0f && view.ScreenY(300f) <= height, "the box does not fit down");
        }

        [TestMethod]
        public void FramingKeepsTheSpreadItWasReadAt()
        {
            var view = new CanvasTransform();
            view.SetSpread(2f, 0f, 0f);

            view.Fit(0f, 0f, 100f, 100f, 800f, 600f, 40f);
            Assert.AreEqual(2f, view.Spread, Tolerance);

            view.ResetZoom(800f, 600f);
            Assert.AreEqual(2f, view.Spread, Tolerance, "resetting the view undid a reading setting its owner chose");
            Assert.AreEqual(1f, view.Zoom, Tolerance);
        }
    }
}
