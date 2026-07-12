namespace Battle.Source.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Core.Battle;
    using Core.Data;
    using Core.Entity;
    using Core.Events;
    using Core.Events.GameEvents;
    using Godot;

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
        // Melee lunge: presentation-only movement between spot anchors — "who approaches" is never
        // a logic decision. Tune the gap/timings to taste.
        private const float MeleeGap = 120f;
        private const float ApproachSeconds = 0.25f;
        private const float ReturnSeconds = 0.20f;
        // How far into the swing clip the hit lands: the target's reaction overlaps the strike
        // point instead of waiting for the full swing to finish.
        private const float SwingImpactFraction = 0.6f;
        private const string AttackAnimation = "Fight_Attack";
        private const string HurtAnimation = "Fight_Hurt";
        private const string DeadAnimation = "Dead";
        private const string StunnedAnimation = "Stunned";
        // Auto-boost: a backed-up queue (big multi-sided fights) plays faster on top of the
        // player-chosen speed and drains back to normal pace as the queue empties.
        private const int AutoBoostQueueLength = 24;
        private const float MaxAutoBoost = 2f;

        private readonly Queue<TimelineEntry> _pending = new();
        private readonly HashSet<string> _shownDead = [];
        private Dictionary<Type, Func<object, Task>> _beatHandlers = [];
        private IBattleTimeline? _timeline;
        private IBattleEventBus? _battleEventBus;
        private AbilityVfxPresenter? _vfx;
        private Func<string, Node2D?>? _findSpot;
        private Task _playback = Task.CompletedTask;
        private float _baseSpeed = 1f;

        private float CurrentSpeed => _baseSpeed * Mathf.Clamp(_pending.Count / (float)AutoBoostQueueLength, 1f, MaxAutoBoost);

        public bool IsPlaying => !_playback.IsCompleted;

        public void Setup(IBattleTimeline timeline, IBattleEventBus battleEventBus, AbilityVfxPresenter? vfx = null, Func<string, Node2D?>? findSpot = null)
        {
            _beatHandlers = CreateBeatHandlers();
            _battleEventBus = battleEventBus;
            _battleEventBus.Subscribe<PlaybackSpeedChangedEvent>(OnPlaybackSpeedChanged);
            _vfx = vfx;
            _findSpot = findSpot;
            _shownDead.Clear();
            _timeline = timeline;
            _timeline.EntryRecorded += OnEntryRecorded;
        }

        public void Teardown()
        {
            if (_timeline != null) _timeline.EntryRecorded -= OnEntryRecorded;
            _timeline = null;
            _battleEventBus?.Unsubscribe<PlaybackSpeedChangedEvent>(OnPlaybackSpeedChanged);
            _battleEventBus = null;
            _pending.Clear();
            _shownDead.Clear();
        }

        /// <summary>Completes once everything recorded so far has been shown.</summary>
        public Task WaitUntilIdleAsync() => _playback;

        public override void _ExitTree() => Teardown();

        private void OnPlaybackSpeedChanged(PlaybackSpeedChangedEvent evnt) => _baseSpeed = Mathf.Max(evnt.Speed, 0.1f);

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
                    _vfx?.SpeedScale = CurrentSpeed;
                    if (TryCollectCastChord(out var cast, out var chord)) await PlayCastChord(cast, chord);
                    else if (TryCollectMeleeExchange(out var opener, out var steps)) await PlayMeleeExchange(opener, steps);
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
            try
            {
                await PlayEvent(entry.Event);
                await WaitAsync(DelayBetweenBeats);
            }
            catch (Exception e)
            {
                Tracker.TrackException($"Beat playback failed for {entry.Event.GetType().Name}", e, this);
            }
        }

        private async Task PlayEvent(object evnt)
        {
            if (!_beatHandlers.TryGetValue(evnt.GetType(), out var handler)) return;
            if (ShouldSkipPosthumous(evnt)) return;
            await handler(evnt);
        }

        /// <summary>One step of a melee exchange: an attack with its resolution (hurt/evade/block),
        /// or an instant rider event (heal/effect) that fired between the blows and replays in order.</summary>
        private readonly record struct ExchangeStep(BeforeAttackEvent? Attack, object? Resolution, object? Instant);

        /// <summary>
        /// A melee exchange is the run of consecutive basic attacks between the same two fighters —
        /// the planned attack plus its extra attacks and counterattacks: one approach by the opener,
        /// every blow lands at melee range, one return at the end. Without the batching every
        /// reaction made its own lunge across the arena to the opponent's (empty) spot.
        /// Attack resolution writes the entries strictly in order, so the resolution is always
        /// the entry right after its BeforeAttackEvent.
        /// </summary>
        private bool TryCollectMeleeExchange(out BeforeAttackEvent opener, out List<ExchangeStep> steps)
        {
            opener = null!;
            steps = [];
            if (_pending.Peek().Event is not BeforeAttackEvent head) return false;
            opener = head;

            while (_pending.Count > 0)
            {
                object evnt = _pending.Peek().Event;
                if (evnt is BeforeAttackEvent attack && IsSamePair(opener, attack))
                {
                    _pending.Dequeue();
                    object? resolution = null;
                    if (_pending.Count > 0 && IsResolutionOf(attack, _pending.Peek().Event))
                        resolution = _pending.Dequeue().Event;
                    steps.Add(new ExchangeStep(attack, resolution, null));
                }
                else if (evnt is EntityHealedEvent or EffectAppliedEvent && NextAttackContinuesExchange(opener))
                    steps.Add(new ExchangeStep(null, null, _pending.Dequeue().Event));
                else break;
            }

            return true;
        }

        private static bool IsSamePair(BeforeAttackEvent first, BeforeAttackEvent next) =>
            (next.Context.Attacker.IsSame(first.Context.Attacker.InstanceId) && next.Context.Target.IsSame(first.Context.Target.InstanceId))
            || (next.Context.Attacker.IsSame(first.Context.Target.InstanceId) && next.Context.Target.IsSame(first.Context.Attacker.InstanceId));

        /// <summary>Instant rider events (on-hit heals/effects) sit between the blows of one exchange;
        /// they join the phrase only when another attack of the same pair follows them.</summary>
        private bool NextAttackContinuesExchange(BeforeAttackEvent opener)
        {
            foreach (var entry in _pending)
            {
                switch (entry.Event)
                {
                    case EntityHealedEvent or EffectAppliedEvent: continue;
                    case BeforeAttackEvent attack: return IsSamePair(opener, attack);
                    default: return false;
                }
            }

            return false;
        }

        private static bool IsResolutionOf(BeforeAttackEvent attack, object evnt) => evnt switch
        {
            DamageTakenEvent damage => ReferenceEquals(damage.Context.Source, attack.Context.Attacker) && damage.Target.IsSame(attack.Context.Target.InstanceId),
            AttackEvadedEvent evaded => ReferenceEquals(evaded.Context, attack.Context),
            AttackBlockedEvent blocked => ReferenceEquals(blocked.Context, attack.Context),
            _ => false
        };

        private async Task PlayMeleeExchange(BeforeAttackEvent opener, List<ExchangeStep> steps)
        {
            try
            {
                var mover = opener.Context.Attacker;
                if (!IsShownDead(mover)) await MoveToMeleeRangeAsync(mover, opener.Context.Target);
                foreach (var step in steps)
                {
                    if (step.Instant != null) await PlayEvent(step.Instant);
                    else await PlayExchangeBlow(step.Attack!, step.Resolution);
                }

                // Re-checked after the blows: a counterattack may have killed the opener mid-exchange.
                if (!IsShownDead(mover)) await ReturnToSpotAsync(mover);
                await WaitAsync(DelayBetweenBeats);
            }
            catch (Exception e)
            {
                Tracker.TrackException("Melee exchange playback failed", e, this);
            }
        }

        /// <summary>The swing and its outcome overlap: the resolution shows at the strike point of the
        /// clip, and the next blow starts only after the swing has fully played out.</summary>
        private async Task PlayExchangeBlow(BeforeAttackEvent attack, object? resolution)
        {
            bool showAttacker = !IsShownDead(attack.Context.Attacker);
            Task swing = showAttacker ? PlayAttack(attack) : Task.CompletedTask;
            if (showAttacker) await WaitAsync(attack.Context.Attacker.Animations.GetClipSeconds(AttackAnimation) * SwingImpactFraction);
            if (resolution != null) await PlayEvent(resolution);
            await swing;
            await WaitAsync(DelayBetweenBeats);
        }

        /// <summary>Runs the attacker's node from its spot to arm's reach of the target's spot.
        /// Missing spot lookup degrades to attacking in place.</summary>
        private async Task MoveToMeleeRangeAsync(IFightable attacker, IFightable target)
        {
            if (attacker is not Node2D node || _findSpot == null) return;
            var attackerSpot = _findSpot(attacker.InstanceId);
            var targetSpot = _findSpot(target.InstanceId);
            if (attackerSpot == null || targetSpot == null || attackerSpot == targetSpot) return;

            var direction = (attackerSpot.GlobalPosition - targetSpot.GlobalPosition).Normalized();
            var anchor = targetSpot.GlobalPosition + (direction * MeleeGap);
            var tween = node.CreateTween();
            // A lunge: bursts forward and decelerates into the strike.
            tween.TweenProperty(node, "global_position", anchor, ApproachSeconds / CurrentSpeed)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            await PlayMovementTween(node, tween);
        }

        /// <summary>Back to the spot origin (the fighter's node lives as the spot's child at zero).</summary>
        private async Task ReturnToSpotAsync(IFightable attacker)
        {
            if (attacker is not Node2D node || node.Position == Vector2.Zero) return;
            var tween = node.CreateTween();
            tween.TweenProperty(node, "position", Vector2.Zero, ReturnSeconds / CurrentSpeed)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
            await PlayMovementTween(node, tween);
        }

        /// <summary>Awaits a fighter-movement tween with the body's collision suspended. The arena
        /// shares world coordinates, so a moving CharacterBody2D would otherwise shove/drag any
        /// overlapping world NPC (physics depenetration / platform carry — the family of bug #24).
        /// Layer and mask are restored when the tween ends, even if it is cancelled.</summary>
        private async Task PlayMovementTween(Node2D node, Tween tween)
        {
            if (node is not CharacterBody2D body) return;
            uint layer = body.CollisionLayer;
            uint mask = body.CollisionMask;
            try
            {
                body.CollisionLayer = 0;
                body.CollisionMask = 0;
                await ToSignal(tween, Tween.SignalName.Finished);
            }
            finally
            {
                if (IsInstanceValid(body))
                {
                    body.CollisionLayer = layer;
                    body.CollisionMask = mask;
                }
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
            // Deaths outside a damage beat (Incineration's Kill): the corpse must still fall on screen.
            // No republish — the battle bus received the event at resolve time.
            [typeof(EntityDiedEvent)] = evnt => PlayEntityDied((EntityDiedEvent)evnt),
        };

        private Task PlayEntityDied(EntityDiedEvent evnt)
        {
            if (!IsShownDead(evnt.Entity)) ShowDeath(evnt.Entity);
            return Task.CompletedTask;
        }

        private Task RepublishBeat<T>(T evnt)
            where T : IBattleEvent
        {
            Republish(evnt);
            return Task.CompletedTask;
        }

        private Task PlayAttack(BeforeAttackEvent evnt) =>
            evnt.Context.Attacker.Animations.PlayAnimationAsync(AttackAnimation, CurrentSpeed);

        private async Task PlayDamageTaken(DamageTakenEvent evnt)
        {
            Republish(evnt);
            // Attacks of an ability (SourceAbilityId stamped) show the ability's impact clip with the hurt.
            Task impact = PlayAttackImpact(evnt);
            await evnt.Target.Animations.PlayAnimationAsync(HurtAnimation, CurrentSpeed);
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
            await evnt.Fighter.Animations.PlayAnimationAsync(StunnedAnimation, CurrentSpeed);
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
                await cast.Caster.Animations.PlayAnimationAsync(cast.Ability.Id, CurrentSpeed);
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
                else if (visual is { MeleeApproach: true })
                {
                    // Melee series: the caster runs up to each target in turn, the hits land at
                    // arm's reach, one return home at the end of the whole cast.
                    foreach (var group in hits.GroupBy(hit => hit.Target.InstanceId))
                    {
                        await MoveToMeleeRangeAsync(cast.Caster, group.First().Target);
                        await PlayHitGroup(group.ToList(), visual, cast.Caster.InstanceId);
                    }

                    await ReturnToSpotAsync(cast.Caster);
                }
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
            var hurt = target.Animations.PlayAnimationAsync(HurtAnimation, CurrentSpeed);
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
                var hurt = hit.Target.Animations.PlayAnimationAsync(HurtAnimation, CurrentSpeed);
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
            float scaled = seconds / CurrentSpeed;
            if (scaled <= 0 || !IsInsideTree()) return;
            await ToSignal(GetTree().CreateTimer(scaled), SceneTreeTimer.SignalName.Timeout);
        }
    }
}
