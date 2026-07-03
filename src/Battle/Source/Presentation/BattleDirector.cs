namespace Battle.Source.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
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
        private const float HitStagger = 0.12f;
        private const string AttackAnimation = "Fight_Attack";
        private const string HurtAnimation = "Fight_Hurt";
        private const string DeadAnimation = "Dead";
        private const string StunnedAnimation = "Stunned";

        private readonly Queue<TimelineEntry> _pending = new();
        private readonly HashSet<string> _shownDead = [];
        private Dictionary<Type, Func<object, Task>> _beatHandlers = [];
        private IBattleTimeline? _timeline;
        private IBattleEventBus? _battleEventBus;
        private Task _playback = Task.CompletedTask;

        public bool IsPlaying => !_playback.IsCompleted;

        public void Setup(IBattleTimeline timeline, IBattleEventBus battleEventBus)
        {
            _beatHandlers = CreateBeatHandlers();
            _battleEventBus = battleEventBus;
            _shownDead.Clear();
            _timeline = timeline;
            _timeline.EntryRecorded += OnEntryRecorded;
        }

        public void Teardown()
        {
            if (_timeline != null) _timeline.EntryRecorded -= OnEntryRecorded;
            _timeline = null;
            _battleEventBus = null;
            _pending.Clear();
            _shownDead.Clear();
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
            if (!IsInsideTree()) return;
            Republish(new PresentationStateChangedEvent(IsPlaying: true));
            try
            {
                // One-frame delay: logic resolves a whole cast synchronously, so by the next
                // frame its entries are all queued and the cast window can be batched into a chord.
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

                while (_pending.Count > 0 && IsInsideTree())
                {
                    if (TryCollectCastChord(out var cast, out var chord)) await PlayCastChord(cast, chord);
                    else await PlayBeat(_pending.Dequeue());
                }
            }
            finally
            {
                Republish(new PresentationStateChangedEvent(IsPlaying: false));
            }
        }

        /// <summary>
        /// A cast chord is the window of entries produced by one ability activation: from its
        /// AbilityActivatedEvent to the LAST queued entry carrying the same CastId. Rider events
        /// without an id (heals, effect applications) that fall inside the window belong to the cast.
        /// </summary>
        private bool TryCollectCastChord(out AbilityActivatedEvent cast, out List<object> chord)
        {
            cast = null!;
            chord = [];
            if (_pending.Peek().Event is not AbilityActivatedEvent head) return false;

            int lastIndex = FindLastEntryOfCast(_pending.ToList(), head.CastId);
            _pending.Dequeue(); // the activation event itself, held separately as the chord opener
            for (int i = 1; i <= lastIndex; i++)
                chord.Add(_pending.Dequeue().Event);

            cast = head;
            return true;
        }

        private static int FindLastEntryOfCast(List<TimelineEntry> entries, string castId)
        {
            for (int i = entries.Count - 1; i > 0; i--)
                if (GetCastId(entries[i].Event) == castId)
                    return i;

            return 0;
        }

        private static string? GetCastId(object evnt) => evnt switch
        {
            AbilityActivatedEvent ability => ability.CastId,
            DamageTakenEvent damage => damage.Context.CastId,
            _ => null,
        };

        /// <summary>A failed beat must not stall the whole playback queue.</summary>
        private async Task PlayBeat(TimelineEntry entry)
        {
            if (!_beatHandlers.TryGetValue(entry.Event.GetType(), out var handler)) return;
            if (ShouldSkipPosthumous(entry.Event)) return;

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
            [typeof(TurnSkippedEvent)] = evnt => PlayTurnSkipped((TurnSkippedEvent)evnt),
        };

        private static Task PlayAttack(BeforeAttackEvent evnt) =>
            evnt.Context.Attacker.Animations.PlayAnimationAsync(AttackAnimation);

        private async Task PlayDamageTaken(DamageTakenEvent evnt)
        {
            Republish(evnt);
            await evnt.Target.Animations.PlayAnimationAsync(HurtAnimation);
            if (evnt.Vitals.IsDead) ShowDeath(evnt.Target);
        }

        private void ShowDeath(IFightable target)
        {
            target.Animations.PlayAnimation(DeadAnimation);
            _shownDead.Add(target.InstanceId);
        }

        /// <summary>
        /// Death cuts the corpse's remaining beats: the killing hit finishes its animation
        /// (handled in <see cref="PlayDamageTaken"/>), everything recorded after it that
        /// involves the dead entity is dropped. Logic already prevents posthumous actions,
        /// so this is a presentation-side guarantee, not a rules check.
        /// </summary>
        private bool ShouldSkipPosthumous(object evnt) => evnt switch
        {
            BeforeAttackEvent attack => IsShownDead(attack.Context.Attacker),
            DamageTakenEvent damage => IsShownDead(damage.Target),
            EntityHealedEvent healed => IsShownDead(healed.Healed),
            AbilityActivatedEvent ability => IsShownDead(ability.Caster),
            TurnSkippedEvent skipped => IsShownDead(skipped.Fighter),
            _ => false,
        };

        private bool IsShownDead(IFightable entity) => _shownDead.Contains(entity.InstanceId);

        private Task PlayHealed(EntityHealedEvent evnt)
        {
            Republish(evnt);
            return Task.CompletedTask;
        }

        /// <summary>The skipped turn: the UI learns about it (status text) and the fighter shows a stun animation.</summary>
        private async Task PlayTurnSkipped(TurnSkippedEvent evnt)
        {
            Republish(evnt);
            await evnt.Fighter.Animations.PlayAnimationAsync(StunnedAnimation);
        }

        /// <summary>
        /// One ability activation as a single visual phrase: cast animation, then all hits —
        /// parallel across targets, staggered numbers within a target, riders replayed inline.
        /// The cast animation is named after the ability id, matching the pre-replay convention.
        /// </summary>
        private async Task PlayCastChord(AbilityActivatedEvent cast, List<object> chord)
        {
            if (IsShownDead(cast.Caster)) return;

            try
            {
                Republish(cast);
                await cast.Caster.Animations.PlayAnimationAsync(cast.Ability.Id);

                var hits = chord.OfType<DamageTakenEvent>().Where(hit => !IsShownDead(hit.Target)).ToList();
                foreach (object rider in chord.Where(evnt => evnt is not DamageTakenEvent))
                    ReplayInstant(rider);

                var hitGroups = hits.GroupBy(hit => hit.Target.InstanceId);
                await Task.WhenAll(hitGroups.Select(group => PlayHitGroup(group.ToList())));
                await WaitAsync(DelayBetweenBeats);
            }
            catch (Exception e)
            {
                Tracker.TrackException($"Cast chord playback failed for {cast.Ability.Id}", e, this);
            }
        }

        /// <summary>All hits of one cast on one target: a single hurt animation with staggered numbers.</summary>
        private async Task PlayHitGroup(List<DamageTakenEvent> hits)
        {
            var target = hits[0].Target;
            var hurt = target.Animations.PlayAnimationAsync(HurtAnimation);
            foreach (var hit in hits)
            {
                Republish(hit);
                await WaitAsync(HitStagger);
            }

            await hurt;
            if (hits[^1].Vitals.IsDead) ShowDeath(target);
        }

        /// <summary>Rider events inside a chord have no animation of their own — the UI just learns about them.</summary>
        private void ReplayInstant(object evnt)
        {
            if (evnt is EntityHealedEvent healed && !IsShownDead(healed.Healed)) Republish(healed);
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
