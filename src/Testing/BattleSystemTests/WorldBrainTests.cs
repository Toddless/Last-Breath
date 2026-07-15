namespace LastBreathTest.BattleSystemTests
{
    using Core.Ai.World;
    using Core.Entity.Components;
    using Godot;

    [TestClass]
    public class WorldBrainTests
    {
        [TestMethod]
        public void CalmSpotsTargetAndStartsChasing()
        {
            var agent = CreateAgent();
            var brain = CreateBrain(agent);
            agent.Sighting = new TargetSighting(new Vector2(200, 0));

            brain.Tick(0.1f); // spot
            brain.Tick(0.1f); // chase

            Assert.AreEqual(AlertnessState.Alert, brain.State);
            Assert.AreEqual(new Vector2(200, 0), agent.LastDestination);
            Assert.AreEqual(CreateConfig().MoveSpeed * CreateConfig().ChaseSpeedMultiplier, agent.LastSpeed);
        }

        [TestMethod]
        public void LostTargetIsChasedToLastKnownPositionThenSearched()
        {
            var agent = CreateAgent();
            var brain = CreateBrain(agent);
            var lastSeenAt = new Vector2(300, 0);
            agent.Sighting = new TargetSighting(lastSeenAt);
            brain.Tick(0.1f); // Alert

            agent.Sighting = null;
            brain.Tick(0.1f); // heads to the last known position
            Assert.AreEqual(AlertnessState.Alert, brain.State);
            Assert.AreEqual(lastSeenAt, agent.LastDestination);

            agent.Position = lastSeenAt; // arrived, nobody here
            brain.Tick(0.1f);
            Assert.AreEqual(AlertnessState.Search, brain.State);
        }

        [TestMethod]
        public void SearchTimesOutBackToCalm()
        {
            var agent = CreateAgent();
            var brain = CreateBrain(agent);
            agent.Sighting = new TargetSighting(agent.Position); // spotted right here
            brain.Tick(0.1f);
            agent.Sighting = null;
            brain.Tick(0.1f); // already near last known -> Search
            Assert.AreEqual(AlertnessState.Search, brain.State);

            brain.Tick(10f); // longer than SearchSeconds
            Assert.AreEqual(AlertnessState.Calm, brain.State);
        }

        [TestMethod]
        public void LeashLimitBreaksTheChase()
        {
            var agent = CreateAgent();
            var brain = CreateBrain(agent);
            agent.Sighting = new TargetSighting(new Vector2(200, 0));
            brain.Tick(0.1f); // Alert

            agent.Position = new Vector2(1000, 0); // dragged beyond the 900 leash
            agent.Sighting = new TargetSighting(new Vector2(1200, 0)); // target still in sight
            brain.Tick(0.1f);

            Assert.AreEqual(AlertnessState.Search, brain.State);
        }

        [TestMethod]
        public void NoiseIsInvestigatedThenForgotten()
        {
            var agent = CreateAgent();
            var brain = CreateBrain(agent);
            var noiseAt = new Vector2(150, 0);

            brain.OnStimulus(new Stimulus(StimulusType.Noise, noiseAt));
            Assert.AreEqual(AlertnessState.Suspicious, brain.State);

            brain.Tick(0.1f); // walks to the point
            Assert.AreEqual(noiseAt, agent.LastDestination);

            agent.Position = noiseAt; // arrived, looks around
            brain.Tick(10f); // longer than SuspiciousSeconds
            Assert.AreEqual(AlertnessState.Calm, brain.State);
        }

        [TestMethod]
        public void NoiseBeyondHearingRadiusIsIgnored()
        {
            var agent = CreateAgent();
            var brain = CreateBrain(agent);

            brain.OnStimulus(new Stimulus(StimulusType.Noise, new Vector2(5000, 0)));

            Assert.AreEqual(AlertnessState.Calm, brain.State);
        }

        [TestMethod]
        public void NonAggressiveNpcFleesFromASeenEnemy()
        {
            var agent = CreateAgent();
            var brain = CreateBrain(agent, aggressive: false);
            agent.Sighting = new TargetSighting(new Vector2(100, 0));

            brain.Tick(0.1f); // frighten
            brain.Tick(0.1f); // run

            Assert.AreEqual(AlertnessState.Flee, brain.State);
            Assert.IsNotNull(agent.LastDestination);
            Assert.IsTrue(agent.LastDestination.Value.X < 0, "must run AWAY from the threat at +X");
        }

        [TestMethod]
        public void NonAggressiveNpcFleesFromNoiseAndCalmsDown()
        {
            var agent = CreateAgent();
            var brain = CreateBrain(agent, aggressive: false);

            brain.OnStimulus(new Stimulus(StimulusType.Noise, new Vector2(50, 0)));
            Assert.AreEqual(AlertnessState.Flee, brain.State);

            brain.Tick(10f); // longer than FleeSeconds, no threat in sight
            Assert.AreEqual(AlertnessState.Calm, brain.State);
        }

        [TestMethod]
        public void FightingSuspendsTheBrain()
        {
            var agent = CreateAgent();
            var brain = CreateBrain(agent);
            agent.IsFighting = true;
            agent.Sighting = new TargetSighting(new Vector2(100, 0));

            brain.Tick(0.1f);
            brain.OnStimulus(new Stimulus(StimulusType.Noise, agent.Position));

            Assert.AreEqual(AlertnessState.Calm, brain.State);
        }

        [TestMethod]
        public void PostBattleGraceBlocksInstantReAggression()
        {
            var agent = CreateAgent();
            var brain = CreateBrain(agent);
            agent.Sighting = new TargetSighting(new Vector2(100, 0));
            brain.Tick(0.1f); // Alert

            brain.OnBattleEnded();
            Assert.AreEqual(AlertnessState.Calm, brain.State);

            brain.Tick(0.1f); // the player is still in sight, but the grace holds
            Assert.AreEqual(AlertnessState.Calm, brain.State);

            brain.Tick(10f); // grace expired
            Assert.AreEqual(AlertnessState.Alert, brain.State);
        }

        // ---- helpers ----

        private static WorldBrainConfig CreateConfig(bool aggressive = true) => new()
        {
            Aggressive = aggressive,
            Activity = WorldActivityType.Idle, // no activity movement noise in assertions
        };

        private static WorldBrain CreateBrain(FakeAgent agent, bool aggressive = true) =>
            new(agent, CreateConfig(aggressive), new DefaultRandomNumberGenerator(seed: 42));

        private class FakeAgent : IWorldAgent
        {
            public Vector2 Position { get; set; } = Vector2.Zero;
            public Vector2 HomePosition { get; set; } = Vector2.Zero;
            public bool IsFighting { get; set; }
            public TargetSighting? Sighting { get; set; }
            public Vector2? LastDestination { get; private set; }
            public float LastSpeed { get; private set; }

            public void MoveTo(Vector2 destination, float speed)
            {
                LastDestination = destination;
                LastSpeed = speed;
            }

            public void StopMoving() => LastDestination = null;

            public TargetSighting? GetSighting(float visionRadius) =>
                Sighting is { } sighting && Position.DistanceTo(sighting.Position) <= visionRadius ? sighting : null;
        }

        private static FakeAgent CreateAgent() => new();
    }
}
