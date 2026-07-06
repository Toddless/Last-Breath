namespace Core.Ai.World
{
    using System.Collections.Generic;
    using Activities;
    using Godot;
    using Interfaces.Components;
    using Stateless;

    /// <summary>
    /// World-mode mind of one NPC: the alertness FSM (Calm/Suspicious/Alert/Search) around
    /// an activity (idle/wander/patrol). Pure C# — the body is behind <see cref="IWorldAgent"/>,
    /// ticked with real delta by the node. Suspended entirely while the entity fights; battle
    /// itself starts by physical contact (the brain only chases into it).
    /// </summary>
    public class WorldBrain
    {
        /// <summary>Close enough to a destination to count as arrived.</summary>
        private const float ArriveDistance = 25f;

        private enum Trigger
        {
            Spot,
            HearNoise,
            LoseTrack,
            Timeout,
            GiveUp
        }

        private readonly StateMachine<AlertnessState, Trigger> _fsm;
        private readonly IWorldActivity _activity;
        private Vector2 _investigationPoint;
        private Vector2 _lastKnownTargetPosition;
        private float _lingerLeft;
        private float _graceLeft;

        public WorldBrain(IWorldAgent agent, WorldBrainConfig config, IRandomNumberGenerator rnd, IReadOnlyList<Vector2>? patrolRoute = null)
        {
            Agent = agent;
            Config = config;
            Rnd = rnd;
            _activity = CreateActivity(config, patrolRoute);
            _fsm = new StateMachine<AlertnessState, Trigger>(AlertnessState.Calm);
            ConfigureFsm();
            _activity.Enter(this);
        }

        public IWorldAgent Agent { get; }
        public WorldBrainConfig Config { get; }
        public IRandomNumberGenerator Rnd { get; }
        public AlertnessState State => _fsm.State;

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
            }
        }

        /// <summary>World events routed by the body (the adapter filters by hearing radius).</summary>
        public void OnStimulus(Stimulus stimulus)
        {
            if (Agent.IsFighting) return;
            if (Agent.Position.DistanceTo(stimulus.Position) > Config.HearingRadius) return;

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
                .Ignore(Trigger.Timeout);

            _fsm.Configure(AlertnessState.Suspicious)
                .OnEntry(() => _lingerLeft = Config.SuspiciousSeconds)
                .PermitReentry(Trigger.HearNoise) // a fresher noise wins
                .Permit(Trigger.Spot, AlertnessState.Alert)
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
                .Permit(Trigger.Timeout, AlertnessState.Calm);
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

        /// <summary>Escalates to Alert on visual contact. Non-aggressive NPCs never escalate.</summary>
        private bool TrySpot()
        {
            if (!Config.Aggressive || _graceLeft > 0) return false;

            var sighting = Agent.GetSighting(Config.VisionRadius);
            if (sighting == null) return false;

            _lastKnownTargetPosition = sighting.Value.Position;
            _fsm.Fire(Trigger.Spot);
            return true;
        }

        private static IWorldActivity CreateActivity(WorldBrainConfig config, IReadOnlyList<Vector2>? patrolRoute) => config.Activity switch
        {
            WorldActivityType.Wander => new WanderActivity(),
            WorldActivityType.Patrol when patrolRoute is { Count: > 0 } => new PatrolActivity(patrolRoute),
            _ => new IdleActivity()
        };
    }
}
