namespace Core.Data
{
    using Entity;

    /// <summary>
    /// Point-in-time copy of an entity's survival numbers, captured when an event is published.
    /// The presentation layer replays events after logic has fully resolved, so it must never
    /// read live entity state (that state is "from the future") — it reads this snapshot instead.
    /// </summary>
    public readonly record struct VitalsSnapshot(
        float Health,
        float MaxHealth,
        float Barrier,
        float MaxBarrier,
        float Mana,
        float MaxMana)
    {
        public bool IsDead => Health <= 0;

        public static VitalsSnapshot From(IFightable entity) => new(
            entity.CurrentHealth,
            entity.Parameters.MaxHealth,
            entity.CurrentBarrier,
            entity.Parameters.MaxBarrier,
            entity.CurrentMana,
            entity.Parameters.MaxMana);
    }
}
