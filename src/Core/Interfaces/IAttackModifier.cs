namespace Core.Interfaces
{
    using Battle;
    using Enums;

    /// <summary>
    /// A modifier applied to an attack context. Held ability-scoped (on the ability) or entity-scoped
    /// (in the entity's <c>ModifierHandler</c>). Reads/writes the attack via the single context —
    /// positional info (Index/TotalCount/IsFirst/IsLast) now lives on <see cref="IAttackContext"/>.
    /// </summary>
    public interface IAttackModifier : IIdentifiable
    {
        Priority Priority { get; set; }
        void Apply(IAttackContext context);
    }
}
