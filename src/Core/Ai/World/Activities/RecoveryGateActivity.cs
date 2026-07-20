namespace Core.Ai.World.Activities
{
    /// <summary>
    /// Wraps the WHOLE calm routine (single activity, schedule or cycle): below the retreat
    /// threshold the NPC abandons whatever it was doing, walks home and rests in the Rest pose
    /// until full — the spawn point's recovery zone does the actual healing. Home without a zone
    /// (wild risen NPCs) → after a few fruitless game minutes it gives up and lives with the
    /// wounds until something else heals it past the threshold. Recovery-in-progress survives
    /// battle interruptions (fields are progress; the resumability contract).
    /// </summary>
    public class RecoveryGateActivity(IWorldActivity inner, WorldActivityContext context) : IWorldActivity
    {
        private bool _recovering;
        private bool _gaveUp;
        private bool _posed;
        private float _stallSeconds;
        private float _lastHealthPercent;

        private float Threshold => context.Recovery?.NpcRetreatHealthPercent ?? 0.5f;

        /// <summary>Real seconds of resting without gains before giving up (one real second
        /// approximates one game minute at the default clock speed).</summary>
        private float GiveUpSeconds => context.Recovery?.NpcGiveUpMinutes ?? 3f;

        public void Enter(WorldBrain brain)
        {
            if (!_recovering) inner.Enter(brain);
        }

        public void Tick(WorldBrain brain, float delta)
        {
            if (!_recovering)
            {
                TickRoutine(brain, delta);
                return;
            }

            TickRecovery(brain, delta);
        }

        public void Exit(WorldBrain brain)
        {
            if (_recovering) DropPose(brain); // the body may be dragged into a battle mid-rest
            else inner.Exit(brain);
        }

        private void TickRoutine(WorldBrain brain, float delta)
        {
            float health = brain.Agent.HealthPercent;
            // Healed past the threshold by any other means — the gate re-arms after a give-up.
            if (_gaveUp && health >= Threshold) _gaveUp = false;

            if (!_gaveUp && health < Threshold)
            {
                inner.Exit(brain);
                _recovering = true;
                _stallSeconds = 0;
                _lastHealthPercent = health;
                return;
            }

            inner.Tick(brain, delta);
        }

        private void TickRecovery(WorldBrain brain, float delta)
        {
            float health = brain.Agent.HealthPercent;
            if (health >= 1f)
            {
                EndRecovery(brain);
                return;
            }

            if (!brain.IsNear(brain.Agent.HomePosition))
            {
                DropPose(brain);
                brain.Agent.MoveTo(brain.Agent.HomePosition, brain.Config.MoveSpeed);
                return;
            }

            brain.Agent.StopMoving();
            if (!_posed)
            {
                _posed = true;
                brain.Agent.SetActivityPose(ActivityPoses.Rest);
            }

            // No zone at home heals nobody: track the stall and give up instead of standing forever.
            if (health > _lastHealthPercent)
            {
                _lastHealthPercent = health;
                _stallSeconds = 0;
                return;
            }

            _stallSeconds += delta;
            if (_stallSeconds < GiveUpSeconds) return;

            _gaveUp = true;
            EndRecovery(brain);
        }

        private void EndRecovery(WorldBrain brain)
        {
            DropPose(brain);
            _recovering = false;
            inner.Enter(brain);
        }

        private void DropPose(WorldBrain brain)
        {
            if (!_posed) return;
            _posed = false;
            brain.Agent.ClearActivityPose();
        }
    }
}
