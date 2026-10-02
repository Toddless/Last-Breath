namespace LastBreathTest.PassiveTree
{
    using Core.PassiveTree.View;
    using Core.Save;
    using Core.Session;
    using Microsoft.Extensions.DependencyInjection;

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

        /// <summary>The frame the canvas assigns to its drawing node, checked against the arithmetic the
        /// picker uses. It is one value and not two readings of "scale" on purpose: the frame carries
        /// POSITIONS, so its multiplier is the position scale, and a frame scaled by the zoom alone draws
        /// a tree at one spread while every click is measured at another.</summary>
        [TestMethod]
        public void TheFrameDrawsANodeWhereTheTransformPlacesIt()
        {
            var view = new CanvasTransform();
            view.SetSpread(2.25f, 0f, 0f);
            view.ZoomBy(1.5f, 0f, 0f);
            view.SetPan(240f, -60f);

            CanvasFrame frame = view.Frame();

            Assert.AreEqual(view.PositionScale, frame.Scale, Tolerance, "the frame is scaled by something other than the position scale");
            Assert.AreEqual(view.PanX, frame.X, Tolerance);
            Assert.AreEqual(view.PanY, frame.Y, Tolerance);
            Assert.AreEqual(view.ScreenX(180f), frame.X + 180f * frame.Scale, Tolerance);
            Assert.AreEqual(view.ScreenY(-90f), frame.Y + -90f * frame.Scale, Tolerance);
        }

        /// <summary>One rule for putting a spread on its grid, used when a value is read out of a
        /// document and when one is written back. Rounding on only one side is how a file comes to hold
        /// 2.3 while the view shows 2.25 — the picture and the document parting company at the first
        /// save.</summary>
        [TestMethod]
        public void NormalizingASpread_SnapsToTheStoredGridAndStaysInsideTheBounds()
        {
            Assert.AreEqual(2.25f, CanvasTransform.NormalizeSpread(2.3f), Tolerance);
            Assert.AreEqual(CanvasTransform.DefaultSpread, CanvasTransform.NormalizeSpread(1.06f), Tolerance);
            Assert.AreEqual(CanvasTransform.MaxSpread, CanvasTransform.NormalizeSpread(400f), Tolerance);
            Assert.AreEqual(CanvasTransform.MinSpread, CanvasTransform.NormalizeSpread(-3f), Tolerance);

            var view = new CanvasTransform();
            view.SetSpread(2.3f, 0f, 0f);
            Assert.AreEqual(CanvasTransform.NormalizeSpread(2.3f), view.Spread, Tolerance,
                "the view snapped a spread by a rule of its own");
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

        /// <summary>Coming back to a wheel that was closed looking at one corner of the tree. The view
        /// comes back whole — the same frame the picker and the GPU were both reading — and a stored zoom
        /// still passes the bounds a wheel click passes, so no remembered value can open the tree at a
        /// scale the controls cannot reach. The spread stays the document's.</summary>
        [TestMethod]
        public void RestoringAView_GivesBackTheSameFrame_AndClampsTheZoomItWasHanded()
        {
            var left = new CanvasTransform();
            left.SetSpread(2f, 0f, 0f);
            left.ZoomBy(2f, 400f, 300f);
            left.MovePan(-120f, 45f);
            CanvasFrame frame = left.Frame();

            var reopened = new CanvasTransform();
            reopened.SetSpread(2f, 0f, 0f);
            reopened.Restore(left.Zoom, left.PanX, left.PanY);

            Assert.AreEqual(frame.X, reopened.Frame().X, Tolerance);
            Assert.AreEqual(frame.Y, reopened.Frame().Y, Tolerance);
            Assert.AreEqual(frame.Scale, reopened.Frame().Scale, Tolerance, "the wheel reopened at a different scale than it was left at");

            reopened.Restore(CanvasTransform.MaxZoom * 10f, 0f, 0f);
            Assert.AreEqual(CanvasTransform.MaxZoom, reopened.Zoom, Tolerance);

            reopened.Restore(0f, 0f, 0f);
            Assert.AreEqual(CanvasTransform.MinZoom, reopened.Zoom, Tolerance);
            Assert.AreEqual(2f, reopened.Spread, Tolerance, "a restored view overruled the layout its document was authored at");
        }

        /// <summary>Where the wheel was left, kept outside the window that was closed. Empty until
        /// somebody leaves something in it — an empty one is what makes a first open fit the whole
        /// tree.</summary>
        [TestMethod]
        public void TheWheelsMemory_IsEmptyUntilAViewIsLeftInIt_AndGivesBackWhatWasLeft()
        {
            var memory = new PassiveWheelViewMemory();
            var left = new CanvasTransform();
            left.ZoomBy(2f, 0f, 0f);
            left.SetPan(-300f, 180f);

            Assert.IsTrue(memory.IsEmpty);
            Assert.IsFalse(memory.Restore(left), "an empty memory answered as though it held a view");

            memory.Remember(left);
            var reopened = new CanvasTransform();

            Assert.IsTrue(memory.Restore(reopened), "the wheel reopened on a memory that was written to");
            Assert.AreEqual(left.Zoom, reopened.Zoom, Tolerance);
            Assert.AreEqual(left.PanX, reopened.PanX, Tolerance);
            Assert.AreEqual(left.PanY, reopened.PanY, Tolerance);
        }

        /// <summary>No stand-in for the wiring: a new playthrough has to reach the memory through the
        /// container the game builds, and the wheel has to find the very same instance there — it asks
        /// optionally, which is a resolve that answers with nothing when the registration is missing.</summary>
        [TestMethod]
        public void TheSessionResetStack_ReachesTheWheelsMemory()
        {
            var services = new ServiceCollection();
            services.AddSingleton<LoadScope>();
            services.AddSingleton<PassiveWheelViewMemory>();
            services.AddSessionReset();

            ServiceProvider provider = services.BuildServiceProvider();
            PassiveWheelViewMemory? memory = provider.GetServices<PassiveWheelViewMemory>().FirstOrDefault();
            Assert.IsNotNull(memory, "the wheel asks the container for its memory and would be handed nothing");

            var left = new CanvasTransform();
            left.ZoomBy(2f, 0f, 0f);
            left.SetPan(-300f, 180f);
            memory.Remember(left);

            provider.GetRequiredService<ISessionResetService>().ResetSession();

            Assert.IsTrue(memory.IsEmpty, "the registered reset stack does not know about the wheel's memory");
            Assert.IsFalse(memory.Restore(new CanvasTransform()), "a new playthrough inherited the last one's view of the wheel");
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
