namespace Battle.Source.Abilities.Targeting
{
    using System.Collections.Generic;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    /// <summary>Targets the caster; no player clicks — the controller resolves and commits at once.</summary>
    public sealed class SelfTargeting : ITargetingStrategy
    {
        public int MaxTargets => 1;
        public bool RequiresManualSelection => false;

        public IReadOnlyList<IFightable> GetValidTargets(IFightable caster, IBattleField field) => [caster];

        public IReadOnlyList<IFightable> ResolveAutomatic(IFightable caster, IBattleField field) => [caster];
    }
}
