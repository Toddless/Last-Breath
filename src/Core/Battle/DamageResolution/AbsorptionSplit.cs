namespace Core.Battle.DamageResolution
{
    using Context;
    using Enums;

    /// <summary>
    /// A post-mitigation blow cut in two by the only rule absorption knows about damage types:
    /// <see cref="Absorbable"/> is what shields and barriers may eat on the way to health, while
    /// <see cref="Bypassing"/> reaches health directly. The stage guard reads <see cref="Total"/> —
    /// the anti-oneshot floor of a staged boss holds against the whole blow, bypassing part included.
    /// </summary>
    public readonly record struct AbsorptionSplit(float Absorbable, float Bypassing)
    {
        /// <summary>Types no absorption layer sees: Blight damages health directly.</summary>
        private const DamageType BypassingTypes = DamageType.Blight;

        /// <summary>The whole blow — what health would take with nothing standing in the way.</summary>
        public float Total => Absorbable + Bypassing;

        /// <summary>Splits what the context carries; per-type reduction has already been applied.
        /// Summed in double like <see cref="IDamageContext.TotalDamage"/>, so a blow carrying nothing
        /// bypassing splits into exactly the blow and not into a rounding of it.</summary>
        public static AbsorptionSplit Of(IDamageContext context)
        {
            double absorbable = 0;
            double bypassing = 0;
            foreach ((DamageType type, float damage) in context.DamageComponents)
            {
                if ((type & BypassingTypes) != 0) bypassing += damage;
                else absorbable += damage;
            }

            return new AbsorptionSplit((float)absorbable, (float)bypassing);
        }

        /// <summary>Brings the whole blow down to <paramref name="total"/> keeping the proportion: a layer
        /// that clamps the blow as a whole (the stage guard) cuts neither part in particular.</summary>
        public AbsorptionSplit ScaledTo(float total)
        {
            float current = Total;
            if (current <= 0) return this;
            return new AbsorptionSplit(Absorbable * total / current, Bypassing * total / current);
        }
    }
}
