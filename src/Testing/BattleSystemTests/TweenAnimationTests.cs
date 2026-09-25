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

        // ---------- three static facings are a complete art set ----------

        [TestMethod]
        public void EveryName_TheDirectorPlays_ResolvesToAnAuthoredFacingFrame()
        {
            // Before any state pose is drawn for it, beast art (Bear/Deer, and Wolf/Direwolf until
            // their fight frames were drawn) is Idle_Down, Idle_Up and Idle_Left and nothing else.
            // The stand-in animator shows the step's Clip on the sprite, so a name that resolved to
            // anything outside that set would ask the engine for a clip that does not exist. Right is
            // the mirrored left frame, blows and the fall keep the facing they had. Art that DOES
            // draw a state pose gets it laid over this step afterwards — see the state-art pins.
            string[] authored = [FrontClip, BackClip, SideClip];
            string[] played =
            [
                "Idle_Down", "Idle_Up", "Idle_Left", "Idle_Right",
                "Walk_Down", "Walk_Up", "Walk_Left", "Walk_Right",
                TweenAnimationRules.AttackAnimation,
                TweenAnimationRules.HurtAnimation,
                TweenAnimationRules.DeathAnimation,
                TweenAnimationRules.StunAnimation,
                "Ability_Armageddon", // a cast pose: the id is never a clip name
            ];

            var state = new TweenAnimationState();
            foreach (string animation in played)
                CollectionAssert.Contains(authored, state.Next(animation).Clip, animation);
        }

        [TestMethod]
        public void AimingABlow_AlsoResolvesToAnAuthoredFacingFrame()
        {
            // The blow turns toward what it strikes (the arena's approach offset picks the side);
            // the turn must land on the mirrored left frame, never on a drawn "Idle_Right".
            var state = new TweenAnimationState();
            var blow = state.Next(TweenAnimationRules.AttackAnimation);

            Assert.AreEqual(SideClip, state.Turn(TweenFacing.Right, blow).Clip);
            Assert.AreEqual(SideClip, state.Turn(TweenFacing.Left, blow).Clip);
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
        public void AuthoredFrame_IsExactlyTheLoneFrameAuthoredMotionRefuses()
        {
            // The two rules split the same art between the two animators; a clip must belong to
            // exactly one of them, and a missing clip to neither.
            Assert.IsTrue(TweenAnimationRules.IsAuthoredStateFrame(hasClip: true, frameCount: 1));
            Assert.IsFalse(TweenAnimationRules.IsAuthoredStateFrame(hasClip: true, frameCount: 2), "real motion is the clip animator's");
            Assert.IsFalse(TweenAnimationRules.IsAuthoredStateFrame(hasClip: true, frameCount: 0));
            Assert.IsFalse(TweenAnimationRules.IsAuthoredStateFrame(hasClip: false, frameCount: 1));
        }

        // ---------- a drawn pose for a state: one frame the tween shows instead of the facing ----------

        [TestMethod]
        public void StateArt_ADrawnPose_ReplacesTheFacingFrame()
        {
            // Wolf and Direwolf draw a single Fight_Attack frame. One frame is not motion the engine
            // can play, so the tween animator must show THAT frame and lunge with it — showing the
            // idle frame instead is what made the drawn pose invisible.
            var state = new TweenAnimationState();
            var blow = state.Next(TweenAnimationRules.AttackAnimation);

            var drawn = TweenAnimationRules.WithStateArt(blow, hasClip: true, frameCount: 1);

            Assert.AreEqual(TweenAnimationRules.AttackAnimation, drawn.Clip);
            Assert.IsTrue(drawn.HasAuthoredFrame);
            Assert.AreEqual(TweenAnimationKind.Attack, drawn.Kind, "the motion still plays over the drawn pose");
            Assert.AreEqual(blow.Seconds, drawn.Seconds, 0.0001f);
        }

        [TestMethod]
        public void StateArt_KeepsTheMirrorTheAimAskedFor()
        {
            // The pose is drawn facing left, exactly like the idle art: aiming the blow right must
            // still mirror it, or a wolf striking to its right would bite backwards.
            var state = new TweenAnimationState();
            var aimed = state.Turn(TweenFacing.Right, state.Next(TweenAnimationRules.AttackAnimation));

            var drawn = TweenAnimationRules.WithStateArt(aimed, hasClip: true, frameCount: 1);

            Assert.AreEqual(TweenAnimationRules.AttackAnimation, drawn.Clip);
            Assert.IsTrue(drawn.FlipHorizontally);
            Assert.IsFalse(TweenAnimationRules.WithStateArt(
                state.Turn(TweenFacing.Left, aimed), hasClip: true, frameCount: 1).FlipHorizontally);
        }

        [TestMethod]
        public void StateArt_WithoutADrawnPose_LeavesTheStepAlone()
        {
            // Every beast that has no fight frames must keep the exact step it had before: facing
            // frame, facing mirror, no authored pose.
            var state = new TweenAnimationState();
            var blow = state.Next(TweenAnimationRules.AttackAnimation);

            Assert.AreEqual(blow, TweenAnimationRules.WithStateArt(blow, hasClip: false, frameCount: 0));
            Assert.AreEqual(blow, TweenAnimationRules.WithStateArt(blow, hasClip: true, frameCount: 0), "an empty clip draws nothing");
            Assert.AreEqual(blow, TweenAnimationRules.WithStateArt(blow, hasClip: true, frameCount: 6), "real motion is the clip animator's, not the tween's");
        }

        [TestMethod]
        public void StateArt_IsLookedForOnlyUnderTheStatesOwnName()
        {
            Assert.AreEqual(TweenAnimationRules.AttackAnimation, TweenAnimationRules.StateClipFor(TweenAnimationKind.Attack));
            Assert.AreEqual(TweenAnimationRules.HurtAnimation, TweenAnimationRules.StateClipFor(TweenAnimationKind.Hurt));
            Assert.AreEqual(TweenAnimationRules.DeathAnimation, TweenAnimationRules.StateClipFor(TweenAnimationKind.Death));
            Assert.AreEqual(TweenAnimationRules.StunAnimation, TweenAnimationRules.StateClipFor(TweenAnimationKind.Stun));
            Assert.IsNull(TweenAnimationRules.StateClipFor(TweenAnimationKind.Facing), "a turn has no pose of its own");
            Assert.IsNull(TweenAnimationRules.StateClipFor(TweenAnimationKind.Cast), "an ability id is never a clip name");
        }

        [TestMethod]
        public void StateArt_ACastPose_NeverBorrowsADrawnFrame()
        {
            // A cast is named by its ability id; there is no clip called after it, and it must not
            // pick up the blow's frame just because one exists.
            var pose = new TweenAnimationState().Next("Ability_Armageddon");

            var drawn = TweenAnimationRules.WithStateArt(pose, hasClip: true, frameCount: 1);

            Assert.AreEqual(pose, drawn);
            Assert.IsFalse(drawn.HasAuthoredFrame);
        }

        // ---------- the fall: a corpse the artist already laid down is not toppled ----------

        [TestMethod]
        public void Fall_WithoutADrawnCorpse_StillTopples()
        {
            var fall = new TweenAnimationState().Next(TweenAnimationRules.DeathAnimation);

            Assert.IsTrue(TweenAnimationRules.TopplesOnDeath(fall), "a standing facing frame has to be rotated down");
        }

        [TestMethod]
        public void Fall_WithADrawnCorpse_DoesNotTopple()
        {
            // The drawn Dead frame is already a body lying horizontally: rotating it eighty degrees
            // on top of that would stand the corpse back up on its head.
            var fall = new TweenAnimationState().Next(TweenAnimationRules.DeathAnimation);

            var drawn = TweenAnimationRules.WithStateArt(fall, hasClip: true, frameCount: 1);

            Assert.AreEqual(TweenAnimationRules.DeathAnimation, drawn.Clip);
            Assert.IsFalse(TweenAnimationRules.TopplesOnDeath(drawn));
        }

        [TestMethod]
        public void Fall_WithADrawnCorpse_StillPlaysOnceAndHoldsTheBeat()
        {
            var state = new TweenAnimationState();

            var first = TweenAnimationRules.WithStateArt(state.Next(TweenAnimationRules.DeathAnimation), hasClip: true, frameCount: 1);
            var second = TweenAnimationRules.WithStateArt(state.Next(TweenAnimationRules.DeathAnimation), hasClip: true, frameCount: 1);

            Assert.IsFalse(first.IsRepeat);
            Assert.IsTrue(second.IsRepeat, "a drawn corpse must not be dropped a second time");
            Assert.IsTrue(state.IsFallen);
            Assert.AreEqual(TweenAnimationRules.DeathSeconds, first.Seconds, 0.0001f, "the beat queue still holds for the whole fall");
        }

        [TestMethod]
        public void Toppling_IsOnlyEverAskedOfTheFall()
        {
            var state = new TweenAnimationState();
            var blow = TweenAnimationRules.WithStateArt(state.Next(TweenAnimationRules.AttackAnimation), hasClip: true, frameCount: 1);

            Assert.IsFalse(TweenAnimationRules.TopplesOnDeath(blow), "a blow is not a fall, drawn or not");
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
