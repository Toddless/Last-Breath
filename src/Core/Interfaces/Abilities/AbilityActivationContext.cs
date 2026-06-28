namespace Core.Interfaces.Abilities
{
    using System.Collections.Generic;
    using Battle;
    using Entity;

    public record AbilityActivationContext
    {
        public required IEntity Caster { get; init; }
        public required IBattleField Field { get; init; }
        public List<IEntity> Targets { get; init; } = [];
        public List<(IEntity entity, IEffect effect)> PendingEffects { get; } = [];

        public void AddEffectOn(IEntity entity, IEffect effect) => PendingEffects.Add((entity, effect));
    }
}
