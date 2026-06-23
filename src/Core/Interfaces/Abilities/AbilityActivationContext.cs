namespace Core.Interfaces.Abilities
{
    using Battle;
    using Entity;
    using System.Collections.Generic;

    public record AbilityActivationContext
    {
        public required IEntity Caster { get; init; }
        public required IBattleField Field { get; init; }
        public List<IEntity> Targets { get; init; } = [];
        public List<(IEntity entity, IEffect effect)> PendingEffects { get; } = [];

        public void AddEffectOn(IEntity entity, IEffect effect) => PendingEffects.Add((entity, effect));
    }
}
