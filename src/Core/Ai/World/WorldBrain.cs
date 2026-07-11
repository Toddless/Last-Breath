namespace Core.Ai.World
{
    using System.Collections.Generic;
    using Activities;
    using Components;
    using Godot;
    using Stateless;
    using Time;

    /// <summary>
    /// World-mode mind of one NPC: the alertness FSM (Calm/Suspicious/Alert/Search) around
    /// an activity (idle/wander/patrol). Pure C# — the body is behind <see cref="IWorldAgent"/>,
    /// ticked with real delta by the node. Suspended entirely while the entity fights; battle
    /// itself starts by physical contact (the brain only chases into it).
    /// </summary>
    public class WorldBrain : IWorldBrain
    {
        /// <summary>Close enough to a destination to count as arrived.</summary>
        private const float ArriveDistance = 25f;

        private enum Trigger
        {
            Spot,
            HearNoise,
            LoseTrack,
            Timeout,
            GiveUp,
            Frighten
        }

        private readonly StateMachine<AlertnessState, Trigger> _fsm;
        private readonly IWorldActivity _activity;
        private Vector2 _investigationPoint;
        private Vector2 _lastKnownTargetPosition;
        private Vector2 _threatPoint;
        private float _lingerLeft;
        private float _graceLeft;


        public IWorldAgent Agent { get; }
        public WorldBrainConfig Config { get; }
        public IRandomNumberGenerator Rnd { get; }
        // TODO:
        // По необходимости расширить стейт машину.
        // Ввести иерархию (Если нпс добывает ресурс и в этот появляется противник, нпс переключает внимание на битву. ПОсле битвы нпс возвращается к добыче ресурсов, если он выживает)
        public AlertnessState State => _fsm.State;

        public WorldBrain(IWorldAgent agent, WorldBrainConfig config, IRandomNumberGenerator rnd,
            IReadOnlyList<Vector2>? patrolRoute = null, IWorldClock? clock = null)
        {
            Agent = agent;
            Config = config;
            Rnd = rnd;
            _activity = CreateActivity(config, patrolRoute, clock);
            _fsm = new StateMachine<AlertnessState, Trigger>(AlertnessState.Calm);
            ConfigureFsm();
            _activity.Enter(this);
        }

        public bool IsNear(Vector2 point) => Agent.Position.DistanceTo(point) <= ArriveDistance;

        public void Tick(float delta)
        {
            if (Agent.IsFighting) return;
            if (_graceLeft > 0) _graceLeft -= delta;

            switch (_fsm.State)
            {
                case AlertnessState.Calm:
                    TickCalm(delta);
                    break;
                case AlertnessState.Suspicious:
                    TickSuspicious(delta);
                    break;
                case AlertnessState.Alert:
                    TickAlert();
                    break;
                case AlertnessState.Search:
                    TickSearch(delta);
                    break;
                case AlertnessState.Flee:
                    TickFlee(delta);
                    break;
            }
        }

        /// <summary>
        /// World events routed by the body (the adapter filters by hearing radius).
        /// The aggressive go looking; civilians run away from any commotion.
        /// </summary>
        public void OnStimulus(Stimulus stimulus)
        {
            if (Agent.IsFighting) return;
            if (Agent.Position.DistanceTo(stimulus.Position) > Config.HearingRadius) return;

            if (!Config.Aggressive)
            {
                _threatPoint = stimulus.Position;
                _fsm.Fire(Trigger.Frighten);
                return;
            }

            _investigationPoint = stimulus.Position;
            _fsm.Fire(Trigger.HearNoise);
        }

        /// <summary>Called by the body when its battle ends: calm down and hold aggression briefly.</summary>
        public void OnBattleEnded()
        {
            _graceLeft = Config.PostBattleGraceSeconds;
            if (_fsm.State != AlertnessState.Calm) _fsm.Fire(Trigger.Timeout);
        }

        private void ConfigureFsm()
        {
            _fsm.Configure(AlertnessState.Calm)
                .OnEntry(() => _activity.Enter(this))
                .OnExit(() => _activity.Exit(this))
                .Permit(Trigger.Spot, AlertnessState.Alert)
                .Permit(Trigger.HearNoise, AlertnessState.Suspicious)
                .Permit(Trigger.Frighten, AlertnessState.Flee)
                .Ignore(Trigger.Timeout);

            _fsm.Configure(AlertnessState.Suspicious)
                .OnEntry(() => _lingerLeft = Config.SuspiciousSeconds)
                .PermitReentry(Trigger.HearNoise) // a fresher noise wins
                .Permit(Trigger.Spot, AlertnessState.Alert)
                .Permit(Trigger.Frighten, AlertnessState.Flee)
                .Permit(Trigger.Timeout, AlertnessState.Calm);

            _fsm.Configure(AlertnessState.Alert)
                .Permit(Trigger.LoseTrack, AlertnessState.Search)
                .Permit(Trigger.GiveUp, AlertnessState.Search)
                .Permit(Trigger.Timeout, AlertnessState.Calm)
                .Ignore(Trigger.Spot)
                .Ignore(Trigger.HearNoise); // the chase outranks noises

            _fsm.Configure(AlertnessState.Search)
                .OnEntry(() => _lingerLeft = Config.SearchSeconds)
                .Permit(Trigger.Spot, AlertnessState.Alert)
                .Permit(Trigger.HearNoise, AlertnessState.Suspicious)
                .Permit(Trigger.Frighten, AlertnessState.Flee)
                .Permit(Trigger.Timeout, AlertnessState.Calm);

            _fsm.Configure(AlertnessState.Flee)
                .OnEntry(() => _lingerLeft = Config.FleeSeconds)
                .PermitReentry(Trigger.Frighten) // a fresher threat resets the panic
                .Permit(Trigger.Timeout, AlertnessState.Calm)
                .Ignore(Trigger.HearNoise)
                .Ignore(Trigger.Spot);
        }

        private void TickCalm(float delta)
        {
            if (TrySpot()) return;
            _activity.Tick(this, delta);
        }

        private void TickSuspicious(float delta)
        {
            if (TrySpot()) return;

            if (!IsNear(_investigationPoint))
            {
                Agent.MoveTo(_investigationPoint, Config.MoveSpeed);
                return;
            }

            Agent.StopMoving();
            _lingerLeft -= delta;
            if (_lingerLeft <= 0) _fsm.Fire(Trigger.Timeout);
        }

        private void TickAlert()
        {
            if (Agent.HomePosition.DistanceTo(Agent.Position) > Config.LeashRadius)
            {
                Agent.StopMoving();
                _fsm.Fire(Trigger.GiveUp);
                return;
            }

            var sighting = Agent.GetSighting(Config.VisionRadius);
            if (sighting != null)
            {
                _lastKnownTargetPosition = sighting.Value.Position;
                Agent.MoveTo(_lastKnownTargetPosition, Config.MoveSpeed * Config.ChaseSpeedMultiplier);
                return;
            }

            // No contact: head to where the target was last seen, then start searching.
            if (!IsNear(_lastKnownTargetPosition))
            {
                Agent.MoveTo(_lastKnownTargetPosition, Config.MoveSpeed * Config.ChaseSpeedMultiplier);
                return;
            }

            _fsm.Fire(Trigger.LoseTrack);
        }

        private void TickSearch(float delta)
        {
            if (TrySpot()) return;

            Agent.StopMoving();
            _lingerLeft -= delta;
            if (_lingerLeft <= 0) _fsm.Fire(Trigger.Timeout);
        }

        private void TickFlee(float delta)
        {
            // A threat still in sight keeps the panic (and its position) fresh.
            var sighting = Agent.GetSighting(Config.VisionRadius);
            if (sighting != null)
            {
                _threatPoint = sighting.Value.Position;
                _lingerLeft = Config.FleeSeconds;
            }

            Agent.MoveTo(AwayFromThreat(), Config.MoveSpeed * Config.ChaseSpeedMultiplier);

            _lingerLeft -= delta;
            if (_lingerLeft <= 0) _fsm.Fire(Trigger.Timeout); // Calm's activity walks it home
        }

        /// <summary>A point straight away from the threat; home when the threat is on top of us.</summary>
        private Vector2 AwayFromThreat()
        {
            var fromThreat = Agent.Position - _threatPoint;
            return fromThreat.LengthSquared() < 1f
                ? Agent.HomePosition
                : Agent.Position + fromThreat.Normalized() * Config.FleeDistance;
        }

        /// <summary>Visual contact: the aggressive escalate to a chase, civilians bolt.</summary>
        private bool TrySpot()
        {
            if (_graceLeft > 0) return false;

            var sighting = Agent.GetSighting(Config.VisionRadius);
            if (sighting == null) return false;

            if (!Config.Aggressive)
            {
                _threatPoint = sighting.Value.Position;
                _fsm.Fire(Trigger.Frighten);
                return true;
            }

            _lastKnownTargetPosition = sighting.Value.Position;
            _fsm.Fire(Trigger.Spot);
            return true;
        }

        /// <summary>A schedule (when authored and a clock exists) wraps the base activity as its fallback.</summary>
        private static IWorldActivity CreateActivity(WorldBrainConfig config, IReadOnlyList<Vector2>? patrolRoute, IWorldClock? clock)
        {
            var baseActivity = WorldActivityFactory.Create(config.Activity, patrolRoute);
            if (clock == null || config.Schedule.Count == 0) return baseActivity;

            var slots = new List<ScheduleSlot>();
            foreach (var slot in config.Schedule)
                slots.Add(new ScheduleSlot(slot.FromMinuteOfDay, slot.ToMinuteOfDay,
                    WorldActivityFactory.Create(slot.Activity, patrolRoute, slot.WanderRadius)));

            return new ScheduledActivity(clock, slots, baseActivity);
        }
    }
}
