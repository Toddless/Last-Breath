namespace Core.Battle.Abilities
{
    /// <summary>How an effect's authored figure turns into the number its target actually gets.</summary>
    public enum EffectValueShape
    {
        /// <summary>The figure IS the change.</summary>
        Plain,

        /// <summary>The figure is the share GAINED: the change is one plus it.</summary>
        ShareGained,

        /// <summary>The figure is the share LOST: the change is one minus it, never below nothing.</summary>
        ShareLost
    }

    /// <summary>
    /// A number an effect was written with, before the effectiveness of the cast laying it. It does not
    /// convert to float: the only way out is <c>Effect.Effective</c>, which is where the multiplier is
    /// applied. Details and limits — <c>Docs/PLAN-Augments.md</c>, "Эффективность".
    /// </summary>
    public readonly record struct EffectValue(float Authored)
    {
        public static implicit operator EffectValue(float authored) => new(authored);

        public static implicit operator EffectValue(int authored) => new(authored);
    }
}
