namespace LastBreath.Components
{
    using System.Threading.Tasks;
    using Core.Entity.Components;
    using Godot;

    /// <summary>
    /// The animator behind the entity contract. It owns the sprite every clip and every tween plays
    /// on, so an owner scene carries whichever animator its art deserves in the same exported slot.
    /// </summary>
    public abstract partial class AnimationsComponentBase : Node, IAnimationsComponent
    {
        /// <summary>What a clip with a broken playback speed is worth: it cannot be timed, so it is
        /// reported as a short beat instead of a division by zero.</summary>
        private const float UnplayableClipSeconds = 0.5f;

        [Export] protected AnimatedSprite2D? _animatedSprite2D;

        public abstract Task PlayAnimationAsync(string animation, float speedScale = 1f);

        public abstract void PlayAnimation(string animation);

        public abstract float GetClipSeconds(string animation);

        public abstract bool HasClip(string animation);

        /// <summary>
        /// Swaps the sprite's clip set (per-NPC art from the visual library) and keeps it showing:
        /// the current clip if the new set has it, the default facing otherwise. Scale multiplies the
        /// scene's base sprite scale — call once per freshly instantiated scene.
        /// </summary>
        public virtual void ApplyVisual(SpriteFrames frames, float scale = 1f)
        {
            if (_animatedSprite2D == null) return;

            string current = _animatedSprite2D.GetAnimation();
            _animatedSprite2D.SpriteFrames = frames;
            if (scale != 1f) _animatedSprite2D.Scale *= scale;

            string shown = frames.HasAnimation(current) ? current : TweenAnimationRules.ClipFor(TweenFacing.Down);
            if (frames.HasAnimation(shown)) _animatedSprite2D.Play(shown);
        }

        /// <summary>Wiring for an animator built in code; a scene fills the export instead.</summary>
        public void UseSprite(AnimatedSprite2D? sprite) => _animatedSprite2D = sprite;

        /// <summary>Length of an authored clip, frames over playback speed.</summary>
        protected static float GetClipDuration(SpriteFrames frames, string animation)
        {
            float speed = (float)frames.GetAnimationSpeed(animation);
            if (speed <= 0) return UnplayableClipSeconds;

            float totalFrames = 0;
            for (int i = 0; i < frames.GetFrameCount(animation); i++)
                totalFrames += (float)frames.GetFrameDuration(animation, i);
            return totalFrames / speed;
        }
    }
}
