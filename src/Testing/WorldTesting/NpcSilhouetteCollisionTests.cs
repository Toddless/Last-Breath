namespace LastBreathTest.WorldTesting
{
    using Core.Entity.Silhouette;

    /// <summary>
    /// The collision-fit calculator: NPC scenes are authored against the humanoid placeholder,
    /// and animal art gets the shapes refitted by silhouette proportions. The pin tests are the
    /// contract: feeding the humanoid reference silhouette back in MUST return exactly the
    /// authored scene numbers, otherwise every humanoid NPC's collision silently shifts.
    /// </summary>
    [TestClass]
    public class NpcSilhouetteCollisionTests
    {
        private const float Epsilon = 1e-3f;

        /// <summary>An animal silhouette: lower (feet at frame bottom minus the 15px art inset,
        /// body much shorter) and wider than the humanoid placeholder.</summary>
        private static readonly SilhouetteRect s_lowWideAnimal = new(90f, 315f, 300f, 150f);

        // ---- invariant pins: the humanoid reference must reproduce the authored scene numbers exactly ----

        [TestMethod]
        public void HumanoidReference_MainScene_ReturnsAuthoredNumbersExactly()
        {
            var profile = NpcSilhouetteCollisionCalculator.Main.Calculate(
                NpcSilhouetteCollisionCalculator.HumanoidReferenceRect, 1f, 1f);

            AssertCapsule(profile.BodyCapsule, 0f, 92f, 37.4f, 136.39f);
            AssertCapsule(profile.Interaction, 0f, 44f, 35f, 239.69f);
            AssertCircle(profile.Dialogue, 0f, 85f, 77.64f);
            Assert.IsNull(profile.BodyCircle);
        }

        [TestMethod]
        public void HumanoidReference_BattleScene_ReturnsAuthoredNumbersExactly()
        {
            var profile = NpcSilhouetteCollisionCalculator.Battle.Calculate(
                NpcSilhouetteCollisionCalculator.HumanoidReferenceRect, 0.35f, 0.35f);

            AssertCircle(profile.BodyCircle, 0f, 0f, 32f);
            AssertCapsule(profile.Interaction, 0f, 0f, 35f, 120f);
            Assert.IsNull(profile.BodyCapsule);
            Assert.IsNull(profile.Dialogue);
        }

        // ---- degenerate input: the safe fallback is the authored layout, untouched ----

        [TestMethod]
        public void EmptySilhouette_FallsBackToAuthoredLayout()
        {
            var profile = NpcSilhouetteCollisionCalculator.Main.Calculate(default, 1f, 1f);

            AssertCapsule(profile.BodyCapsule, 0f, 92f, 37.4f, 136.39f);
            AssertCapsule(profile.Interaction, 0f, 44f, 35f, 239.69f);
            AssertCircle(profile.Dialogue, 0f, 85f, 77.64f);
        }

        [TestMethod]
        public void ZeroScale_FallsBackToAuthoredLayout()
        {
            var profile = NpcSilhouetteCollisionCalculator.Battle.Calculate(s_lowWideAnimal, 0f, 0f);

            AssertCircle(profile.BodyCircle, 0f, 0f, 32f);
            AssertCapsule(profile.Interaction, 0f, 0f, 35f, 120f);
        }

        // ---- a low, wide animal: the body capsule widens and drops to the animal's feet ----

        [TestMethod]
        public void LowWideAnimal_BodyCapsuleWidensAndDropsToFeet()
        {
            var profile = NpcSilhouetteCollisionCalculator.Main.Calculate(s_lowWideAnimal, 1f, 1f);

            Assert.IsNotNull(profile.BodyCapsule);
            var body = profile.BodyCapsule.Value;
            Assert.IsTrue(body.Radius > 37.4f, $"wider art must widen the capsule, got r={body.Radius}");

            // Feet in NPC-local coords: 315 + 150 - 240 = 225. The authored capsule bottom edge sat
            // 17.195px below the humanoid's feet; scaled by the height ratio it stays glued to the feet.
            float bottomEdge = body.CenterY + body.Height / 2f;
            float authoredBottomEdge = 92f + 136.39f / 2f;
            Assert.IsTrue(bottomEdge > authoredBottomEdge, $"the capsule must drop with the lower art, got bottom={bottomEdge}");
            Assert.AreEqual(225f + 17.195f * (150f / 297f), bottomEdge, Epsilon, "capsule bottom must track the animal's feet");

            Assert.IsTrue(body.Height >= 2f * body.Radius, "Godot capsules cannot be wider than tall");
        }

        [TestMethod]
        public void LowWideAnimal_InteractionAndDialogueFollowTheBody()
        {
            var profile = NpcSilhouetteCollisionCalculator.Main.Calculate(s_lowWideAnimal, 1f, 1f);

            Assert.IsTrue(profile.Interaction.Radius > 35f, "wider art must widen the interaction zone");
            Assert.IsNotNull(profile.Dialogue);
            Assert.IsTrue(profile.Dialogue.Value.CenterY > 85f, "the dialogue circle must move down with the lower art");
            Assert.IsTrue(profile.Dialogue.Value.Radius > 77.64f, "wider art must widen the dialogue circle");
        }

        // ---- sprite scale scales the whole output proportionally ----

        [TestMethod]
        public void DoubledSpriteScale_DoublesEveryOutput()
        {
            var single = NpcSilhouetteCollisionCalculator.Battle.Calculate(s_lowWideAnimal, 0.35f, 0.35f);
            var doubled = NpcSilhouetteCollisionCalculator.Battle.Calculate(s_lowWideAnimal, 0.7f, 0.7f);

            Assert.IsNotNull(single.BodyCircle);
            Assert.IsNotNull(doubled.BodyCircle);
            AssertCircle(doubled.BodyCircle,
                single.BodyCircle.Value.CenterX * 2f, single.BodyCircle.Value.CenterY * 2f,
                single.BodyCircle.Value.Radius * 2f, Epsilon);
            AssertCapsule(doubled.Interaction,
                single.Interaction.CenterX * 2f, single.Interaction.CenterY * 2f,
                single.Interaction.Radius * 2f, single.Interaction.Height * 2f, Epsilon);
        }

        private static void AssertCapsule(CapsuleLayout? actual, float centerX, float centerY, float radius, float height, float delta = 0f)
        {
            Assert.IsNotNull(actual);
            AssertCapsule(actual.Value, centerX, centerY, radius, height, delta);
        }

        private static void AssertCapsule(CapsuleLayout actual, float centerX, float centerY, float radius, float height, float delta = 0f)
        {
            Assert.AreEqual(centerX, actual.CenterX, delta, "capsule CenterX");
            Assert.AreEqual(centerY, actual.CenterY, delta, "capsule CenterY");
            Assert.AreEqual(radius, actual.Radius, delta, "capsule Radius");
            Assert.AreEqual(height, actual.Height, delta, "capsule Height");
        }

        private static void AssertCircle(CircleLayout? actual, float centerX, float centerY, float radius, float delta = 0f)
        {
            Assert.IsNotNull(actual);
            Assert.AreEqual(centerX, actual.Value.CenterX, delta, "circle CenterX");
            Assert.AreEqual(centerY, actual.Value.CenterY, delta, "circle CenterY");
            Assert.AreEqual(radius, actual.Value.Radius, delta, "circle Radius");
        }
    }
}
