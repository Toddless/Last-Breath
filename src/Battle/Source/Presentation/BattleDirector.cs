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

        private const string AttackAnimation = "Fight_Attack";
        private const string HurtAnimation = "Fight_Hurt";

        private const string StunnedAnimation = "Stunned";

        // Auto-boost: a backed-up queue (big multi-sided fights) plays faster on top of the
        // player-chosen speed and drains back to normal pace as the queue empties.
        private const int AutoBoostQueueLength = 12;
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

        // Phrase coalescing: within one visual phrase, repeated effect applications dedupe to one log
        // line each (first kept) and recoveries sum per target (last event kept — its vitals are final);
        // buffered here, emitted once at phrase end by FlushPhrase. Damage is never buffered.
        private readonly Dictionary<string, EffectAppliedEvent> _phraseEffects = [];
        private readonly Dictionary<string, (float Total, EntityHealedEvent Last)> _phraseHeals = [];
        // Latest vitals per entity in the phrase (damage or heal, whichever touched it last): the summed
        // recovery flushes the TRUE final health, so damage after the last heal isn't undone by a stale snapshot.
        private readonly Dictionary<string, VitalsSnapshot> _phraseVitals = [];
        private bool _coalescing;

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

        /// <summary>
        /// Completes once everything recorded so far has been shown — the queue is EMPTY, not merely
        /// "the pump that was running when you asked has ended". Entries recorded while the caller was
        /// waiting start a fresh pump; the loop picks it up, so the turn gate (and the battle end
        /// behind it) can't slip through between two pumps and leave beats unplayed.
        /// </summary>
        public async Task WaitUntilIdleAsync()
        {
            // The reference comparison is the whole trick: a pump that has work always suspends on its
            // first line (the frame wait), so two live pumps are never the same object — an inequality
            // means new entries arrived and started one. Outside the tree a pump returns synchronously
            // as the shared completed task, matching the field's seed, so the loop exits at once —
            // correct, because a director out of the tree has nothing to show anyway.
            Task playback;
            do
            {
                playback = _playback;
                await playback;
            }
            while (playback != _playback);
        }

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
                    // Each phrase coalesces its own effect/heal republishes; the finally flushes them
                    // even if the phrase throws, and deactivates so the next phrase starts clean.
                    BeginPhrase();
                    try
                    {
                        if (TryCollectCastChord(out var cast, out var chord)) await PlayCastChord(cast, chord);
                        else if (TryCollectMeleeExchange(out var opener, out var window)) await PlayMeleeExchange(opener, window);
                        else await PlayBeat(_pending.Dequeue());
                    }
                    finally
                    {
                        FlushPhrase();
                    }
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
            AbilityExecutedEvent executed => executed.CastId, // closing bracket: extends the window past hits that carry no CastId (attack series)
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
            // Death cuts the corpse's remaining beats — the fall itself is never among them (DeathBeat).
            if (DeathBeat.IsPosthumous(evnt, IsShownDead)) return;
            await handler(evnt);
        }

        /// <summary>
        /// A melee exchange is the ordered run of one basic attack and everything it triggers between
        /// the same two fighters — self-burns, on-damage passives/effects, evade/block, extra blows and
        /// counterattacks, deaths — collected in recorded order and bounded by the next action. The
        /// window ends at the first non-same-pair blow (another pair's action) or an AbilityActivatedEvent
        /// (a nested reaction cast, peeled off as its own chord next pump). Silent entries are kept in
        /// place — playback no-ops them — so nothing is reordered.
        /// </summary>
        private bool TryCollectMeleeExchange(out BeforeAttackEvent opener, out List<object> window)
        {
            opener = null!;
            window = [];
            if (_pending.Peek().Event is not BeforeAttackEvent head) return false;
            opener = head;
            window.Add(_pending.Dequeue().Event); // the opening blow itself — its swing opens the phrase

            while (_pending.Count > 0)
            {
                object evnt = _pending.Peek().Event;
                if (evnt is BeforeAttackEvent blow && !IsSamePair(opener, blow)) break;
                if (evnt is AbilityActivatedEvent) break;
                window.Add(_pending.Dequeue().Event);
            }

            return true;
        }

        private static bool IsSamePair(BeforeAttackEvent first, BeforeAttackEvent next) =>
            (next.Context.Attacker.IsSame(first.Context.Attacker.InstanceId) && next.Context.Target.IsSame(first.Context.Target.InstanceId))
            || (next.Context.Attacker.IsSame(first.Context.Target.InstanceId) && next.Context.Target.IsSame(first.Context.Attacker.InstanceId));

        /// <summary>
        /// Replays a basic-attack exchange as one phrase: the opener approaches its target once, the
        /// whole ordered window plays in recorded order (blows, triggers, riders, deaths), one return.
        /// Same shape as <see cref="PlayMeleeSeries"/> — both delegate to <see cref="PlayApproachedMelee"/>.
        /// </summary>
        private async Task PlayMeleeExchange(BeforeAttackEvent opener, List<object> window)
        {
            try
            {
                await PlayApproachedMelee(opener.Context.Attacker, opener.Context.Target, window, visual: null);
                await WaitAsync(DelayBetweenBeats);
            }
            catch (Exception e)
            {
                Tracker.TrackException("Melee exchange playback failed", e, this);
            }
        }

        /// <summary>
        /// The shared melee body: <paramref name="mover"/> lunges to <paramref name="approachTarget"/>
        /// once (when there is one and the mover is still standing), the ordered <paramref name="window"/>
        /// replays in place — each live-target hit at arm's reach, every other beat through the normal
        /// beat map — then one return home. Both the ability series and the basic-attack exchange use it.
        /// </summary>
        private async Task PlayApproachedMelee(IFightable mover, IFightable? approachTarget, IReadOnlyList<object> window, AbilityVisualConfig? visual)
        {
            if (approachTarget != null && !IsShownDead(mover))
                await MoveToMeleeRangeAsync(mover, approachTarget);

            foreach (object entry in window)
            {
                if (entry is DamageTakenEvent hit && !IsShownDead(hit.Target))
                    await PlayHitGroup([hit], visual, mover.InstanceId);
                else
                    await PlayEvent(entry);
            }

            if (!IsShownDead(mover))
                await ReturnToSpotAsync(mover);
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

        private Task PlayEntityDied(EntityDiedEvent evnt) => ShowDeathAsync(evnt.Entity);

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
            if (evnt.Vitals.IsDead) await ShowDeathAsync(evnt.Target);
        }

        private Task PlayAttackImpact(DamageTakenEvent evnt)
        {
            if (_vfx == null || evnt.Context.SourceAbilityId is not { } abilityId) return Task.CompletedTask;
            var visual = _vfx.GetConfig(abilityId);
            return visual == null ? Task.CompletedTask : _vfx.PlayImpactAsync(visual, evnt.Target.InstanceId);
        }

        /// <summary>
        /// The fall, as a beat of its own: the corpse starts its Dead clip and the queue HOLDS for the
        /// clip's length, so the turn gate — and the battle end waiting behind it — never outruns the
        /// animation (tracker #191). Idempotent: a fighter falls once, a second sighting adds no hold.
        /// The clip is started rather than awaited through PlayAnimationAsync on purpose — that helper
        /// restores the PREVIOUS clip when it ends and would stand the corpse back up.
        /// The hold is scaled by the playback speed like every other wait, so the abort fast-forward
        /// flashes past it instead of holding a quit for seconds.
        /// </summary>
        private async Task ShowDeathAsync(IFightable target)
        {
            if (!_shownDead.Add(target.InstanceId)) return;
            target.Animations.PlayAnimation(DeathBeat.Animation);
            await WaitAsync(DeathBeat.HoldSeconds(target.Animations));
        }


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
        /// (flight/appearance per its visual config), then the hits — a melee series approaches once
        /// and replays its window in recorded order, Chain plays sequentially in recorded order,
        /// everything else parallel across targets with staggered numbers within one.
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
                // A melee series consumes the whole ordered chord itself (one approach, hits in place,
                // one return); the up-front rider strip below would reorder its interleaved beats.
                if (hits.Count > 0 && visual is { MeleeApproach: true })
                {
                    await PlayMeleeSeries(cast, chord, visual);
                }
                else
                {
                    foreach (object rider in chord.Where(evnt => evnt is not DamageTakenEvent))
                        ReplayInstant(rider);

                    if (hits.Count == 0)
                    {
                        if (visual != null && _vfx != null) await PlayHitlessCast(cast, chord, visual);
                    }
                    else switch (visual)
                    {
                        case { Delivery: VfxDeliveryKind.Chain } when _vfx != null:
                            await PlayChain(cast, hits, visual);
                            break;
                        default:
                            {
                                var hitGroups = hits.GroupBy(hit => hit.Target.InstanceId);
                                await Task.WhenAll(hitGroups.Select(group => PlayHitGroup(group.ToList(), visual, cast.Caster.InstanceId)));
                                break;
                            }
                    }
                }

                await WaitAsync(DelayBetweenBeats);
            }
            catch (Exception e)
            {
                Tracker.TrackException($"Cast chord playback failed for {cast.Ability.Id}", e, this);
            }
        }

        /// <summary>
        /// A melee attack-series cast played as one phrase: the caster approaches the enemy target
        /// ONCE, then the whole cast window replays in recorded order — each hit lands at arm's reach,
        /// every other beat plays through the normal beat map (per-blow swing, evade/block, mid-series
        /// deaths, riders) — and one return home. A self-inflicted hit (e.g. the fury burn on the
        /// caster) never picks an approach and is shown where it lands.
        /// </summary>
        private async Task PlayMeleeSeries(AbilityActivatedEvent cast, List<object> chord, AbilityVisualConfig visual)
        {
            var approachTarget = chord.OfType<DamageTakenEvent>()
                .Select(hit => hit.Target)
                .FirstOrDefault(target => !target.IsSame(cast.Caster.InstanceId));
            await PlayApproachedMelee(cast.Caster, approachTarget, chord, visual);
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
            else if (visual is { Delivery: VfxDeliveryKind.InstantOnTarget } && _vfx != null)
                await _vfx.PlayStrikeAsync(visual, target.InstanceId);

            Task impact = visual != null && _vfx != null ? _vfx.PlayImpactAsync(visual, target.InstanceId) : Task.CompletedTask;
            var hurt = target.Animations.PlayAnimationAsync(HurtAnimation, CurrentSpeed);
            foreach (var hit in hits)
            {
                Republish(hit);
                await WaitAsync(HitStagger);
            }

            await hurt;
            await impact;
            if (hits[^1].Vitals.IsDead) await ShowDeathAsync(target);
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
                if (hit.Vitals.IsDead) await ShowDeathAsync(hit.Target);
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
                case AbilityStageActivatedEvent stage when !IsShownDead(stage.Caster):
                    Republish(stage);
                    break;
                case ExhaustionChangedEvent exhaustion when !IsShownDead(exhaustion.Fighter):
                    Republish(exhaustion);
                    break;
            }
        }

        private void Republish<T>(T evnt)
            where T : IBattleEvent
        {
            if (_coalescing)
            {
                // Record vitals BEFORE buffering so damage (which passes through) still updates the snapshot.
                RecordPhraseVitals(evnt);
                if (TryCoalesce(evnt)) return;
            }

            _battleEventBus?.Publish(evnt);
        }

        /// <summary>Latest vitals per entity in the phrase → the summed recovery emits the TRUE final
        /// health; damage landing after the last heal must not be undone by a stale flush snapshot.</summary>
        private void RecordPhraseVitals<T>(T evnt)
            where T : IBattleEvent
        {
            switch (evnt)
            {
                case DamageTakenEvent damage: _phraseVitals[damage.Target.InstanceId] = damage.Vitals; break;
                case EntityHealedEvent healed: _phraseVitals[healed.Healed.InstanceId] = healed.Vitals; break;
            }
        }

        /// <summary>Buffers the two coalescible events for the current phrase and reports it handled:
        /// effect applications dedupe by (id, target) keeping the first; recoveries sum per target
        /// keeping the last event (its vitals are the final health). Damage and everything else — false.</summary>
        private bool TryCoalesce<T>(T evnt)
            where T : IBattleEvent
        {
            switch (evnt)
            {
                case EffectAppliedEvent applied:
                    _phraseEffects.TryAdd(EffectKey(applied), applied);
                    return true;
                case EntityHealedEvent healed:
                    var accumulated = _phraseHeals.GetValueOrDefault(healed.Healed.InstanceId);
                    _phraseHeals[healed.Healed.InstanceId] = (accumulated.Total + healed.Amount, healed);
                    return true;
                default:
                    return false;
            }
        }

        private static string EffectKey(EffectAppliedEvent applied) => $"{applied.Effect.Id}|{applied.Target.InstanceId}";

        /// <summary>Opens a coalescing phrase: buffered effect/heal republishes are held until FlushPhrase.</summary>
        private void BeginPhrase()
        {
            _phraseEffects.Clear();
            _phraseHeals.Clear();
            _phraseVitals.Clear();
            _coalescing = true;
        }

        /// <summary>Closes the phrase: deactivate FIRST (so the flushed republishes go straight through),
        /// then emit one representative event per effect key and one summed recovery per target.</summary>
        private void FlushPhrase()
        {
            _coalescing = false;
            foreach (EffectAppliedEvent applied in _phraseEffects.Values)
                Republish(applied);
            foreach (var (id, (total, last)) in _phraseHeals)
                Republish(new EntityHealedEvent(last.Healed, total, _phraseVitals.GetValueOrDefault(id, last.Vitals)));
            _phraseEffects.Clear();
            _phraseHeals.Clear();
            _phraseVitals.Clear();
        }

        private async Task WaitAsync(float seconds)
        {
            float scaled = seconds / CurrentSpeed;
            if (scaled <= 0 || !IsInsideTree()) return;
            await ToSignal(GetTree().CreateTimer(scaled), SceneTreeTimer.SignalName.Timeout);
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
            [typeof(AbilityStageActivatedEvent)] = evnt => RepublishBeat((AbilityStageActivatedEvent)evnt),
            [typeof(ExhaustionChangedEvent)] = evnt => RepublishBeat((ExhaustionChangedEvent)evnt),
            // Deaths outside a damage beat (Incineration's Kill): the corpse must still fall on screen.
            // No republish — the battle bus received the event at resolve time.
            [typeof(EntityDiedEvent)] = evnt => PlayEntityDied((EntityDiedEvent)evnt),
        };
    }
}
