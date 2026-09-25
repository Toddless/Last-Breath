namespace Core.Modifiers
{
    using System;
    using Entity.Components;

    /// <summary>Inclusive value bounds of one rollable modifier line. A fixed value is Min == Max.</summary>
    public readonly record struct ValueRange(float Min, float Max)
    {
        public bool IsFixed => Math.Abs(Min - Max) < 0.01f;

        /// <summary>A fixed value NEVER touches the RNG — legacy single-value content must stay
        /// bit-identical AND consume zero rolls, so seeded sequences don't shift.</summary>
        public float Roll(IRandomNumberGenerator rnd) => IsFixed ? Min : rnd.RandFloatRange(Min, Max);

        public ValueRange Scale(float multiplier) => new(Min * multiplier, Max * multiplier);

        /// <summary>Legacy bridge: every pre-range call site hands a plain float.</summary>
        public static implicit operator ValueRange(float value) => new(value, value);
    }
}
