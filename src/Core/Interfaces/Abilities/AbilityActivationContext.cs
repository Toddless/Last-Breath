namespace Core.Interfaces.Abilities
{
    using System.Collections.Generic;
    using Battle;
    using Entity;

    public record AbilityActivationContext
    {
        public required IFightable Caster { get; init; }
        public required IBattleField Field { get; init; }
        public List<IFightable> Targets { get; init; } = [];
        public List<(IFightable entity, IEffect effect)> PendingEffects { get; } = [];

        public void AddEffectOn(IFightable entity, IEffect effect) => PendingEffects.Add((entity, effect));
    }
}
