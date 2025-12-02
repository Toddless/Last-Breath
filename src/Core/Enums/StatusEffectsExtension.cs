namespace Core.Enums
{
    public static class StatusEffectsExtension
    {
        public static DamageType GetDamageType(this StatusEffects effect)
        {
            return effect switch
            {
                StatusEffects.Bleed => DamageType.Bleed,
                StatusEffects.Burning => DamageType.Burning,
                StatusEffects.Poison => DamageType.Poison,
                _ => DamageType.Normal
            };
        }
    }
}
