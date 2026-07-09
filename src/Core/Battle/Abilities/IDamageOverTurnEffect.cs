namespace Core.Battle.Abilities
{
    /// <summary>Core-visible surface of DoT effects, so pipeline modifiers can scale ticks
    /// without referencing project-side effect classes.</summary>
    public interface IDamageOverTurnEffect : IEffect
    {
        float DamagePerTick { get; set; }
    }
}
