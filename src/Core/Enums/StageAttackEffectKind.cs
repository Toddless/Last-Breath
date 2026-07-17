namespace Core.Enums
{
    /// <summary>What a boss-stage attack applies on hit (the "attackEffects" entries of the
    /// "stages" section of Npc.json). Extensible: new kinds get a member here and a matching
    /// factory branch in the boss stages controller.</summary>
    public enum StageAttackEffectKind
    {
        /// <summary>A poison DoT stack: damagePercent of the attack's damage per turn.</summary>
        Poison,

        /// <summary>A withering-curse stack: −damagePercent of the victim's maximum health.</summary>
        WitheringCurse,
    }
}
