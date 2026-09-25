namespace Core.Entity.Components
{
    /// <summary>
    /// The tween animator's memory between calls: which way the sprite faces and whether it is
    /// already lying down. Godot-free — the node only executes the step this hands back.
    /// </summary>
    public sealed class TweenAnimationState
    {
        /// <summary>The facing the last named animation set; motions without a facing keep it.</summary>
        public TweenFacing Facing { get; private set; } = TweenFacing.Down;

        /// <summary>The sprite is down: a repeated fall must not roll the corpse again.</summary>
        public bool IsFallen { get; private set; }

        /// <summary>Turns an animation name into the step to play, remembering the facing it names
        /// and whether the fall has already been played.</summary>
        public TweenAnimationStep Next(string animation)
        {
            var kind = TweenAnimationRules.KindOf(animation);
            if (TweenAnimationRules.TryResolveFacing(animation, out var named)) Facing = named;

            bool repeat = kind == TweenAnimationKind.Death && IsFallen;
            IsFallen = kind == TweenAnimationKind.Death; // anything else stands the sprite back up
            return Compose(kind, TweenAnimationRules.Seconds(animation), repeat);
        }

        /// <summary>Turns the body toward what it strikes: the blow rewrites the facing of its own step.</summary>
        public TweenAnimationStep Turn(TweenFacing facing, TweenAnimationStep step)
        {
            Facing = facing;
            return step with
            {
                Facing = facing,
                Clip = TweenAnimationRules.ClipFor(facing),
                FlipHorizontally = TweenAnimationRules.FlipsFor(facing),
            };
        }

        private TweenAnimationStep Compose(TweenAnimationKind kind, float seconds, bool repeat) =>
            new(kind, Facing, TweenAnimationRules.ClipFor(Facing), TweenAnimationRules.FlipsFor(Facing), seconds, repeat);
    }
}
