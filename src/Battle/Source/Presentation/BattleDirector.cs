namespace Battle.Source.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Events;
    using Core.Interfaces.Events.GameEvents;
    using Godot;
    using Utilities;

    /// <summary>
    /// The presentation player: logic resolves a turn instantly and fills the timeline,
    /// this node replays the recorded entries as visual "beats" with real-time pacing.
    /// A beat republishes its event to the battle bus (numbers, bars and other UI react
    /// at the moment the hit is SHOWN) and then plays the matching animation.
    /// It is the ONLY place that knows how an event looks on screen; adding a new visual
    /// is adding one handler to <see cref="CreateBeatHandlers"/>. Events without a handler
    /// are skipped silently. The turn loop awaits <see cref="WaitUntilIdleAsync"/> for pacing.
    /// </summary>
    [GlobalClass]
    public partial class BattleDirector : Node
    {
        private const float DelayBetweenBeats = 0.1f;
        private const string AttackAnimation = "Fight_Attack";
        private const string HurtAnimation = "Fight_Hurt";
        private const string DeadAnimation = "Dead";

        private readonly Queue<TimelineEntry> _pending = new();
        private Dictionary<Type, Func<object, Task>> _beatHandlers = [];
        private IBattleTimeline? _timeline;
        private IBattleEventBus? _battleEventBus;
        private Task _playback = Task.CompletedTask;

        public bool IsPlaying => !_playback.IsCompleted;

        public void Setup(IBattleTimeline timeline, IBattleEventBus battleEventBus)
        {
            _beatHandlers = CreateBeatHandlers();
            _battleEventBus = battleEventBus;
            _timeline = timeline;
            _timeline.EntryRecorded += OnEntryRecorded;
        }

        public void Teardown()
        {
            if (_timeline != null) _timeline.EntryRecorded -= OnEntryRecorded;
            _timeline = null;
            _battleEventBus = null;
            _pending.Clear();
        }

        /// <summary>Completes once everything recorded so far has been shown.</summary>
        public Task WaitUntilIdleAsync() => _playback;

        public override void _ExitTree() => Teardown();

        private void OnEntryRecorded(TimelineEntry entry)
        {
            _pending.Enqueue(entry);
            EnsurePlaybackRunning();
        }

        private void EnsurePlaybackRunning()
        {
            if (IsPlaying) return;
            _playback = PlayPendingAsync();
        }

        private async Task PlayPendingAsync()
        {
            while (_pending.Count > 0 && IsInsideTree())
                await PlayBeat(_pending.Dequeue());
        }

        /// <summary>A failed beat must not stall the whole playback queue.</summary>
        private async Task PlayBeat(TimelineEntry entry)
        {
            if (!_beatHandlers.TryGetValue(entry.Event.GetType(), out var handler)) return;

            try
            {
                await handler(entry.Event);
                await WaitAsync(DelayBetweenBeats);
            }
            catch (Exception e)
            {
                Tracker.TrackException($"Beat playback failed for {entry.Event.GetType().Name}", e, this);
            }
        }

        private Dictionary<Type, Func<object, Task>> CreateBeatHandlers() => new()
        {
            [typeof(BeforeAttackEvent)] = evnt => PlayAttack((BeforeAttackEvent)evnt),
            [typeof(DamageTakenEvent)] = evnt => PlayDamageTaken((DamageTakenEvent)evnt),
            [typeof(EntityHealedEvent)] = evnt => PlayHealed((EntityHealedEvent)evnt),
            [typeof(AbilityActivatedEvent)] = evnt => PlayAbilityActivated((AbilityActivatedEvent)evnt),
        };

        private static Task PlayAttack(BeforeAttackEvent evnt) =>
            evnt.Context.Attacker.Animations.PlayAnimationAsync(AttackAnimation);

        private async Task PlayDamageTaken(DamageTakenEvent evnt)
        {
            Republish(evnt);
            await evnt.Target.Animations.PlayAnimationAsync(HurtAnimation);
            if (evnt.Vitals.IsDead) evnt.Target.Animations.PlayAnimation(DeadAnimation);
        }

        private Task PlayHealed(EntityHealedEvent evnt)
        {
            Republish(evnt);
            return Task.CompletedTask;
        }

        /// <summary>The cast animation is named after the ability id, matching the pre-replay convention.</summary>
        private async Task PlayAbilityActivated(AbilityActivatedEvent evnt)
        {
            Republish(evnt);
            await evnt.Caster.Animations.PlayAnimationAsync(evnt.Ability.Id);
        }

        private void Republish<T>(T evnt)
            where T : IBattleEvent => _battleEventBus?.Publish(evnt);

        private async Task WaitAsync(float seconds)
        {
            if (seconds <= 0 || !IsInsideTree()) return;
            await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
        }
    }
}
