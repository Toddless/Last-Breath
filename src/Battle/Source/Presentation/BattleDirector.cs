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
        private AbilityVfxPresenter? _vfx;
        private Task _playback = Task.CompletedTask;

        public bool IsPlaying => !_playback.IsCompleted;

        public void Setup(IBattleTimeline timeline, IBattleEventBus battleEventBus, AbilityVfxPresenter? vfx = null)
        {
            _beatHandlers = CreateBeatHandlers();
            _battleEventBus = battleEventBus;
            _vfx = vfx;
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
            // Republish-only beats: no animation of their own (yet), the UI (battle log) reacts at show time.
            [typeof(AttackEvadedEvent)] = evnt => RepublishBeat((AttackEvadedEvent)evnt),
            [typeof(AttackBlockedEvent)] = evnt => RepublishBeat((AttackBlockedEvent)evnt),
            [typeof(EffectAppliedEvent)] = evnt => RepublishBeat((EffectAppliedEvent)evnt),
        };

        private Task RepublishBeat<T>(T evnt)
            where T : IBattleEvent
        {
            Republish(evnt);
            return Task.CompletedTask;
        }

        private static Task PlayAttack(BeforeAttackEvent evnt) =>
            evnt.Context.Attacker.Animations.PlayAnimationAsync(AttackAnimation);

        private async Task PlayDamageTaken(DamageTakenEvent evnt)
        {
            Republish(evnt);
            // Attacks of an ability (SourceAbilityId stamped) show the ability's impact clip with the hurt.
            Task impact = PlayAttackImpact(evnt);
            await evnt.Target.Animations.PlayAnimationAsync(HurtAnimation);
            await impact;
            if (evnt.Vitals.IsDead) ShowDeath(evnt.Target);
        }

        private Task PlayAttackImpact(DamageTakenEvent evnt)
        {
            if (_vfx == null || evnt.Context.SourceAbilityId is not { } abilityId) return Task.CompletedTask;
            var visual = _vfx.GetConfig(abilityId);
            return visual == null ? Task.CompletedTask : _vfx.PlayImpactAsync(visual, evnt.Target.InstanceId);
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
            AttackEvadedEvent evaded => IsShownDead(evaded.Context.Attacker),
            AttackBlockedEvent blocked => IsShownDead(blocked.Context.Attacker),
            EffectAppliedEvent applied => IsShownDead(applied.Target),
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
        /// One ability activation as a single visual phrase: cast animation, the ability's own VFX
        /// (flight/appearance per its visual config), then the hits — Chain plays them sequentially in
        /// recorded order, everything else parallel across targets with staggered numbers within one.
        /// The cast animation is named after the ability id, matching the pre-replay convention.
        /// </summary>
        private async Task PlayCastChord(AbilityActivatedEvent cast, List<object> chord)
        {
            if (IsShownDead(cast.Caster)) return;

            try
            {
                Republish(cast);
                var visual = _vfx?.GetConfig(cast.Ability.Id);
                // The caster's pose and the ability's activation VFX play together.
                Task castVfx = visual != null && _vfx != null ? _vfx.PlayCastAsync(visual, cast.Caster.InstanceId) : Task.CompletedTask;
                await cast.Caster.Animations.PlayAnimationAsync(cast.Ability.Id);
                await castVfx;

                var hits = chord.OfType<DamageTakenEvent>().Where(hit => !IsShownDead(hit.Target)).ToList();
                foreach (object rider in chord.Where(evnt => evnt is not DamageTakenEvent))
                    ReplayInstant(rider);

                if (hits.Count == 0)
                {
                    if (visual != null && _vfx != null) await PlayHitlessCast(cast, chord, visual);
                }
                else if (visual is { Delivery: VfxDeliveryKind.Chain } && _vfx != null)
                    await PlayChain(cast, hits, visual);
                else
                {
                    var hitGroups = hits.GroupBy(hit => hit.Target.InstanceId);
                    await Task.WhenAll(hitGroups.Select(group => PlayHitGroup(group.ToList(), visual, cast.Caster.InstanceId)));
                }

                await WaitAsync(DelayBetweenBeats);
            }
            catch (Exception e)
            {
                Tracker.TrackException($"Cast chord playback failed for {cast.Ability.Id}", e, this);
            }
        }

        /// <summary>
        /// All hits of one cast on one target: the projectile arrives first, then the impact clip,
        /// hurt animation and staggered numbers play together.
        /// </summary>
        private async Task PlayHitGroup(List<DamageTakenEvent> hits, AbilityVisualConfig? visual, string casterInstanceId)
        {
            var target = hits[0].Target;
            if (visual is { Delivery: VfxDeliveryKind.Projectile } && _vfx != null)
                await _vfx.PlayTravelAsync(visual, casterInstanceId, target.InstanceId);

            Task impact = visual != null && _vfx != null ? _vfx.PlayImpactAsync(visual, target.InstanceId) : Task.CompletedTask;
            var hurt = target.Animations.PlayAnimationAsync(HurtAnimation);
            foreach (var hit in hits)
            {
                Republish(hit);
                await WaitAsync(HitStagger);
            }

            await hurt;
            await impact;
            if (hits[^1].Vitals.IsDead) ShowDeath(target);
        }

        /// <summary>
        /// A cast that dealt no direct damage (pure-effect abilities — the poison jar puts a DoT and
        /// touches nobody with a hit): the VFX targets are taken from the applied effects instead,
        /// so the flight/impact still show. Self-casts play the aura.
        /// </summary>
        private async Task PlayHitlessCast(AbilityActivatedEvent cast, List<object> chord, AbilityVisualConfig visual)
        {
            if (visual.Delivery is VfxDeliveryKind.SelfAura)
            {
                await _vfx!.PlayAuraAsync(visual, cast.Caster.InstanceId);
                return;
            }

            var targets = chord.OfType<EffectAppliedEvent>()
                .Where(applied => !applied.Target.IsSame(cast.Caster.InstanceId) && !IsShownDead(applied.Target))
                .Select(applied => applied.Target)
                .DistinctBy(target => target.InstanceId)
                .ToList();

            if (visual.Delivery is VfxDeliveryKind.Chain)
            {
                string from = cast.Caster.InstanceId;
                foreach (var target in targets)
                {
                    await _vfx!.PlayTravelAsync(visual, from, target.InstanceId);
                    await _vfx.PlayImpactAsync(visual, target.InstanceId);
                    from = target.InstanceId;
                }

                return;
            }

            await Task.WhenAll(targets.Select(async target =>
            {
                if (visual.Delivery is VfxDeliveryKind.Projectile)
                    await _vfx!.PlayTravelAsync(visual, cast.Caster.InstanceId, target.InstanceId);
                await _vfx!.PlayImpactAsync(visual, target.InstanceId);
            }));
        }

        /// <summary>Chain delivery: hits play in recorded (jump) order, the clip flies target → target.</summary>
        private async Task PlayChain(AbilityActivatedEvent cast, List<DamageTakenEvent> hits, AbilityVisualConfig visual)
        {
            // TODO:
            // For now logic looks like: travel => hurt => impact => travel
            //                                 > impact
            // Should be : travel => parallel            => travel
            //                                 > hurt
            // so more like actual chain. We get  chain proj, chain proj move on to the next target, previous target react to impact
            string from = cast.Caster.InstanceId;
            foreach (var hit in hits)
            {
                await _vfx!.PlayTravelAsync(visual, from, hit.Target.InstanceId);
                Task impact = _vfx.PlayImpactAsync(visual, hit.Target.InstanceId);
                var hurt = hit.Target.Animations.PlayAnimationAsync(HurtAnimation);
                Republish(hit);
                await hurt;
                await impact;
                if (hit.Vitals.IsDead) ShowDeath(hit.Target);
                from = hit.Target.InstanceId;
            }
        }

        /// <summary>Rider events inside a chord have no animation of their own — the UI just learns about them.</summary>
        private void ReplayInstant(object evnt)
        {
            switch (evnt)
            {
                case EntityHealedEvent healed when !IsShownDead(healed.Healed):
                    Republish(healed);
                    break;
                case EffectAppliedEvent applied when !IsShownDead(applied.Target):
                    Republish(applied);
                    break;
            }
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
