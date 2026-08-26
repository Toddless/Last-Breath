namespace Core.Entity.Components
{
    using System;

    /// <summary>The facings static art is drawn for; the right side is the left frame mirrored.</summary>
    public enum TweenFacing
    {
        Down,
        Up,
        Left,
        Right,
    }

    /// <summary>The motion an animation name asks the tween animator for.</summary>
    public enum TweenAnimationKind
    {
        Facing,
        Attack,
        Hurt,
        Death,
        Stun,
        Cast,
    }

    /// <summary>
    /// One animation name turned into work: the static frame that carries the facing, whether it is
    /// mirrored, which motion plays, for how long, and whether the fall already happened.
    /// </summary>
    public readonly record struct TweenAnimationStep(
        TweenAnimationKind Kind,
        TweenFacing Facing,
        string Clip,
        bool FlipHorizontally,
        float Seconds,
        bool IsRepeat);

    /// <summary>
    /// The naming convention behind the tween animator: which motion a name asks for, how long that
    /// motion honestly lasts and which frame shows the facing. Godot-free, so timings and the mirror
    /// rule are pinned without a scene tree.
    /// </summary>
    public static class TweenAnimationRules
    {
        public const string DeathAnimation = "Dead";
        public const string AttackAnimation = "Fight_Attack";
        public const string HurtAnimation = "Fight_Hurt";
        public const string StunAnimation = "Stunned";

        /// <summary>Facing frames are authored as idle clips: Down, Up and Left.</summary>
        public const string FacingClipPrefix = "Idle_";

        public const float AttackSeconds = 0.36f;
        public const float HurtSeconds = 0.28f;
        public const float DeathSeconds = 0.45f;
        public const float StunSeconds = 0.5f;

        /// <summary>A cast pose keeps the pause an unauthored clip used to hold, so the beat rhythm survives.</summary>
        public const float CastSeconds = 0.5f;

        private const char FacingSeparator = '_';

        /// <summary>The names that carry a facing. An ability id ending in "_Down" is a cast pose,
        /// not a turn: only these prefixes are read as directions.</summary>
        private static readonly string[] FacingPrefixes = [FacingClipPrefix, "Walk_"];

        public static TweenAnimationKind KindOf(string animation) => animation switch
        {
            DeathAnimation => TweenAnimationKind.Death,
            AttackAnimation => TweenAnimationKind.Attack,
            HurtAnimation => TweenAnimationKind.Hurt,
            StunAnimation => TweenAnimationKind.Stun,
            _ => TryResolveFacing(animation, out _) ? TweenAnimationKind.Facing : TweenAnimationKind.Cast,
        };

        /// <summary>Unscaled length of the motion, in seconds; turning to a facing costs no time.</summary>
        public static float Seconds(string animation) => KindOf(animation) switch
        {
            TweenAnimationKind.Attack => AttackSeconds,
            TweenAnimationKind.Hurt => HurtSeconds,
            TweenAnimationKind.Death => DeathSeconds,
            TweenAnimationKind.Stun => StunSeconds,
            TweenAnimationKind.Cast => CastSeconds,
            _ => 0f,
        };

        /// <summary>
        /// Whether the animator shows something of its own for the name: an activity pose it cannot
        /// play answers false and degrades to idle, the fall answers true and holds the beat queue.
        /// </summary>
        public static bool Handles(string animation) => KindOf(animation) != TweenAnimationKind.Cast;

        /// <summary>Reads the facing out of a movement name (Idle_Left, Walk_Up); anything else,
        /// including an ability id that happens to end in a direction or a number, is not a turn.</summary>
        public static bool TryResolveFacing(string animation, out TweenFacing facing)
        {
            facing = TweenFacing.Down;
            if (string.IsNullOrEmpty(animation) || !CarriesFacing(animation)) return false;

            int separator = animation.LastIndexOf(FacingSeparator);
            if (separator <= 0 || separator == animation.Length - 1) return false;

            string suffix = animation[(separator + 1)..];
            // A numeric suffix is a clip index, never a direction: Enum.TryParse would read it as one.
            return char.IsLetter(suffix[0]) && Enum.TryParse(suffix, ignoreCase: true, out facing) && Enum.IsDefined(facing);
        }

        /// <summary>Whether an authored clip is real MOTION: a lone frame is art for a facing and the
        /// tween animator is what moves it, an empty or missing clip is nothing at all.</summary>
        public static bool IsAuthoredMotion(bool hasClip, int frameCount) => hasClip && frameCount > 1;

        private static bool CarriesFacing(string animation)
        {
            foreach (string prefix in FacingPrefixes)
                if (animation.StartsWith(prefix, StringComparison.Ordinal))
                    return true;

            return false;
        }

        /// <summary>The frame a facing is shown with; the right side borrows the left one.</summary>
        public static string ClipFor(TweenFacing facing) =>
            FacingClipPrefix + (facing == TweenFacing.Right ? TweenFacing.Left : facing);

        /// <summary>The right facing is the left art mirrored — no right-side frame is ever drawn.</summary>
        public static bool FlipsFor(TweenFacing facing) => facing == TweenFacing.Right;

        /// <summary>Playback speed shortens a motion the way it shortens a clip; a dead speed is ignored.</summary>
        public static float Scaled(float seconds, float speedScale) => speedScale > 0 ? seconds / speedScale : seconds;
    }
}
