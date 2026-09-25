namespace Core.Enums
{
    /// <summary>
    /// How an ability picks its targets. Drives the targeting strategy: single-target modes let the
    /// player click one target, <c>Few*</c> modes let him pick up to a capped count, <c>Self</c> resolves
    /// automatically. Serialized as a number in ability data (like <see cref="Stance"/>) — keep the order.
    /// </summary>
    public enum AbilityTargetType : byte
    {
        Enemy,
        FewEnemies,
        Self,
        Ally,
        FewAllies
    }
}
