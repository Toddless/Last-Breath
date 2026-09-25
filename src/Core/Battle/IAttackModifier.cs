namespace Core.Battle
{
    using Context;

    /// <summary>
    /// A modifier applied to an attack context. Held ability-scoped (on the ability) or entity-scoped
    /// (in the entity's <c>ModifierHandler</c>). Reads/writes the attack via the single context —
    /// positional info (Index/TotalCount/IsFirst/IsLast) now lives on <see cref="IAttackContext"/>.
    /// </summary>
    public interface IAttackModifier : IContextModifier<IAttackContext>;
}
