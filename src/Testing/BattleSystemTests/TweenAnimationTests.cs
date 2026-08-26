namespace LastBreathTest.BattleSystemTests
{
    using Core.Entity.Components;

    /// <summary>
    /// The Godot-free half of the stand-in animator: art is one static frame per facing, so which
    /// frame is shown (and whether it is mirrored), how long a motion honestly lasts, and the fact
    /// that a corpse falls exactly once are decided here — the node only runs the tweens.
    /// </summary>
    [TestClass]
    public class TweenAnimationTests
    {
        private const string FrontClip = "Idle_Down";
        private const string SideClip = "Idle_Left";
        private const string BackClip = "Idle_Up";

        // ---------- facings: the right side is the left frame mirrored ----------

        [TestMethod]
        public void Facing_Right_ShowsTheLeftFrameMirrored()
        {
            var step = new TweenAnimationState().Next("Idle_Right");

            Assert.AreEqual(SideClip, step.Clip);
            Assert.IsTrue(step.FlipHorizontally, "the right facing is drawn as the mirrored left frame");
        }

        [TestMethod]
        public void Facing_Left_ShowsTheLeftFrameUnmirrored()
        {
            var step = new TweenAnimationState().Next("Idle_Left");

            Assert.AreEqual(SideClip, step.Clip);
            Assert.IsFalse(step.FlipHorizontally);
        }

        [TestMethod]
        public void Facing_ReadsTheSuffixWhateverThePrefixIs()
        {
            var state = new TweenAnimationState();

            Assert.AreEqual(BackClip, state.Next("Walk_Up").Clip);
            Assert.AreEqual(FrontClip, state.Next("Walk_Down").Clip);
        }

        [TestMethod]
        public void Facing_IsAFacingSwapAndCostsNoTime()
        {
            var step = new TweenAnimationState().Next("Idle_Up");

            Assert.AreEqual(TweenAnimationKind.Facing, step.Kind);
            Assert.AreEqual(0f, step.Seconds, 0.0001f);
        }

        [TestMethod]
        public void Facing_Survives_AMotionThatNamesNone()
        {
            var state = new TweenAnimationState();
            state.Next("Idle_Right");

            var blow = state.Next(TweenAnimationRules.AttackAnimation);

            Assert.AreEqual(TweenFacing.Right, blow.Facing);
            Assert.IsTrue(blow.FlipHorizontally, "the blow keeps the facing the sprite already had");
        }

        [TestMethod]
        public void Turn_AimsTheBlowAndSticks()
        {
            var state = new TweenAnimationState();
            var blow = state.Next(TweenAnimationRules.AttackAnimation);

            var aimed = state.Turn(TweenFacing.Right, blow);

            Assert.AreEqual(SideClip, aimed.Clip);
            Assert.IsTrue(aimed.FlipHorizontally);
            Assert.AreEqual(TweenFacing.Right, state.Next("Fight_Hurt").Facing);
        }

        // ---------- honest durations: the beat queue holds for what actually plays ----------

        [TestMethod]
        public void Handles_TheFall_AndNamesItsRealLength()
        {
            Assert.IsTrue(TweenAnimationRules.Handles(TweenAnimationRules.DeathAnimation), "the fall must hold the beat queue");
            Assert.IsTrue(TweenAnimationRules.Seconds(TweenAnimationRules.DeathAnimation) > 0f, "a zero hold outruns the fall");
        }

        [TestMethod]
        public void Handles_EveryMotionItPlays_WithARealLength()
        {
            string[] motions = [TweenAnimationRules.AttackAnimation, TweenAnimationRules.HurtAnimation, TweenAnimationRules.StunAnimation];

            foreach (string motion in motions)
            {
                Assert.IsTrue(TweenAnimationRules.Handles(motion), motion);
                Assert.IsTrue(TweenAnimationRules.Seconds(motion) > 0f, motion);
            }
        }

        [TestMethod]
        public void Handles_AnActivityPose_IsFalse()
        {
            // No pose art exists, so the world must degrade it to idle instead of showing nothing.
            Assert.IsFalse(TweenAnimationRules.Handles("Activity_Work"));
        }

        [TestMethod]
        public void CastPose_WithANumberedSuffix_IsNotAFacing()
        {
            // Enum.TryParse reads "2" as a facing value: an ability id ending in a number would
            // become a free facing swap and collapse the cast beat to zero seconds.
            var step = new TweenAnimationState().Next("Ability_Strike_2");

            Assert.AreEqual(TweenAnimationKind.Cast, step.Kind);
            Assert.AreEqual(0.5f, step.Seconds, 0.0001f);
        }

        [TestMethod]
        public void Facing_WithANumberedSuffix_IsNotAFacingEither()
        {
            Assert.IsFalse(TweenAnimationRules.TryResolveFacing("Idle_2", out _), "a clip index is not a direction");
        }

        [TestMethod]
        public void CastPose_EndingInADirection_StaysACastPose()
        {
            // Only movement names carry a facing; an ability id is never read as a turn.
            var step = new TweenAnimationState().Next("Ability_Slam_Down");

            Assert.AreEqual(TweenAnimationKind.Cast, step.Kind);
            Assert.AreEqual(0.5f, step.Seconds, 0.0001f);
        }

        // ---------- what counts as a real clip ----------

        [TestMethod]
        public void AuthoredMotion_NeedsMoreThanOneFrame()
        {
            Assert.IsTrue(TweenAnimationRules.IsAuthoredMotion(hasClip: true, frameCount: 2), "two frames are an animation");
            Assert.IsFalse(TweenAnimationRules.IsAuthoredMotion(hasClip: true, frameCount: 1), "a lone frame is a facing the tweens move");
            Assert.IsFalse(TweenAnimationRules.IsAuthoredMotion(hasClip: true, frameCount: 0), "an empty clip shows nothing");
            Assert.IsFalse(TweenAnimationRules.IsAuthoredMotion(hasClip: false, frameCount: 8));
        }

        [TestMethod]
        public void CastPose_KeepsTheRhythmOfTheMissingClipPause()
        {
            var step = new TweenAnimationState().Next("Ability_Armageddon");

            Assert.AreEqual(TweenAnimationKind.Cast, step.Kind);
            Assert.AreEqual(0.5f, step.Seconds, 0.0001f, "the cast pose replaces the half-second pause of an unauthored clip");
        }

        // ---------- the fall happens once ----------

        [TestMethod]
        public void Fall_PlaysOnce_AndTheSecondSightingIsARepeat()
        {
            var state = new TweenAnimationState();

            Assert.IsFalse(state.Next(TweenAnimationRules.DeathAnimation).IsRepeat);
            Assert.IsTrue(state.Next(TweenAnimationRules.DeathAnimation).IsRepeat, "a lying body must not roll again");
        }

        [TestMethod]
        public void Fall_AfterStandingUp_PlaysAgain()
        {
            var state = new TweenAnimationState();
            state.Next(TweenAnimationRules.DeathAnimation);
            state.Next(FrontClip); // risen or revived

            Assert.IsFalse(state.IsFallen);
            Assert.IsFalse(state.Next(TweenAnimationRules.DeathAnimation).IsRepeat);
        }

        // ---------- playback speed ----------

        [TestMethod]
        public void Scaled_ShortensAMotionByThePlaybackSpeed()
        {
            Assert.AreEqual(0.12f, TweenAnimationRules.Scaled(0.36f, 3f), 0.0001f);
            Assert.AreEqual(0.18f, TweenAnimationRules.Scaled(0.36f, 2f), 0.0001f);
        }

        [TestMethod]
        public void Scaled_IgnoresADeadSpeed()
        {
            Assert.AreEqual(0.36f, TweenAnimationRules.Scaled(0.36f, 0f), 0.0001f);
            Assert.AreEqual(0.36f, TweenAnimationRules.Scaled(0.36f, -1f), 0.0001f);
        }
    }
}
