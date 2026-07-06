namespace Battle.Source.Presentation
{
    using System;
    using System.Threading.Tasks;
    using Godot;

    /// <summary>
    /// Plays ability VFX in arena space: flight of the travel clip between spots, the impact clip on a
    /// target, an aura on the caster. Anchored to the entity spots (same lookup the combat text uses).
    /// Missing clips/spots skip silently — a stage without a placeholder simply doesn't show.
    /// </summary>
    [GlobalClass]
    public partial class AbilityVfxPresenter : Node2D
    {
        /// <summary>Looping clips have no finish signal; they show for this long.</summary>
        private const float LoopedClipSeconds = 0.8f;

        private Func<string, Node2D?>? _findSpot;
        private AbilityVisualLibrary? _library;

        public void Setup(Func<string, Node2D?> findSpot, AbilityVisualLibrary library)
        {
            _findSpot = findSpot;
            _library = library;
            if (_library.Frames == null)
                GD.PushWarning("AbilityVfxPresenter: the visual library has no SpriteFrames assigned — ability VFX will not show.");
        }

        public AbilityVisualConfig? GetConfig(string abilityId) => _library?.GetConfig(abilityId);

        /// <summary>The travel clip flies from one fighter's spot to another's.</summary>
        public async Task PlayTravelAsync(AbilityVisualConfig config, string fromInstanceId, string toInstanceId)
        {
            if (!TryGetClip(config.TravelClip, out var frames)) return;
            var from = _findSpot?.Invoke(fromInstanceId);
            var to = _findSpot?.Invoke(toInstanceId);
            if (from == null || to == null) return;

            var sprite = SpawnSprite(frames, config.TravelClip, config.Scale);
            sprite.GlobalPosition = from.GlobalPosition;
            sprite.Rotation = (to.GlobalPosition - from.GlobalPosition).Angle();

            float duration = from.GlobalPosition.DistanceTo(to.GlobalPosition) / Mathf.Max(1f, config.TravelSpeed);
            var tween = CreateTween();
            tween.TweenProperty(sprite, "global_position", to.GlobalPosition, duration);
            await ToSignal(tween, Tween.SignalName.Finished);
            sprite.QueueFree();
        }

        /// <summary>Activation VFX on the caster's spot — the director runs it together with the cast pose.</summary>
        public Task PlayCastAsync(AbilityVisualConfig config, string casterInstanceId) =>
            PlayOnSpotAsync(config.CastClip, config.Scale, casterInstanceId);

        /// <summary>The impact clip lands on the target's spot.</summary>
        public Task PlayImpactAsync(AbilityVisualConfig config, string targetInstanceId) =>
            PlayOnSpotAsync(config.ImpactClip, config.Scale, targetInstanceId);

        /// <summary>Self-cast aura: the travel clip plays once on the caster's spot.</summary>
        public Task PlayAuraAsync(AbilityVisualConfig config, string casterInstanceId) =>
            PlayOnSpotAsync(config.TravelClip, config.Scale, casterInstanceId);

        private async Task PlayOnSpotAsync(string clip, float scale, string instanceId)
        {
            if (!TryGetClip(clip, out var frames)) return;
            var spot = _findSpot?.Invoke(instanceId);
            if (spot == null) return;

            var sprite = SpawnSprite(frames, clip, scale);
            sprite.GlobalPosition = spot.GlobalPosition;
            await WaitClipAsync(sprite, frames, clip);
            sprite.QueueFree();
        }

        private bool TryGetClip(string clip, out SpriteFrames frames)
        {
            frames = _library?.Frames!;
            return !string.IsNullOrEmpty(clip) && frames.HasAnimation(clip);
        }

        private AnimatedSprite2D SpawnSprite(SpriteFrames frames, string clip, float scale)
        {
            var sprite = new AnimatedSprite2D { SpriteFrames = frames, Scale = Vector2.One * scale };
            AddChild(sprite);
            sprite.Play(clip);
            return sprite;
        }

        private async Task WaitClipAsync(AnimatedSprite2D sprite, SpriteFrames frames, string clip)
        {
            // Any looping mode (linear/ping-pong) never emits animation_finished — wait a fixed time instead.
            if (frames.GetAnimationLoopMode(clip) is not SpriteFrames.LoopMode.None)
                await ToSignal(GetTree().CreateTimer(LoopedClipSeconds), SceneTreeTimer.SignalName.Timeout);
            else
                await ToSignal(sprite, AnimatedSprite2D.SignalName.AnimationFinished);
        }
    }
}
