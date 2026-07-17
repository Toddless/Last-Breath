namespace Core.Battle.Abilities
{
    /// <summary>
    /// The anti-oneshot floor of a staged boss: while a stage transition is pending, damage cannot
    /// push health below the transition threshold (the boss takes exactly enough to cross it).
    /// Granted/removed by the boss stages controller; consumed by the damage resolution chain.
    /// </summary>
    public interface IStageGuardEffect : IEffect
    {
        /// <summary>Health may not drop below this value while the guard holds.</summary>
        float FloorHealth { get; }
    }
}
