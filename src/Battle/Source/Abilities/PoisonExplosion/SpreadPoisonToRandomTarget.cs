namespace Battle.Source.Abilities.PoisonExplosion
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Effects;

    public class SpreadPoisonToRandomTarget : IPoisonSpreadMode
    {
        // список целей на которые была активирована способность
        private List<IEntity> _entities = [];

        public void SpreadPoison(List<DamageOverTurnEffect> originalStacks, IEntity originalTarget, IEntity owner, IBattleField field, string source)
        {
            if (_entities.Contains(originalTarget)) return;

            originalTarget.Dead += OnEntityDied;
            _entities.Add(originalTarget);
            return;

            void OnEntityDied(IEntity deadEntity)
            {
                originalTarget.Dead -= OnEntityDied;
                _entities.Remove(originalTarget);
                var poisonStacks = originalTarget.Effects
                    .GetBy(e => e.Status == StatusEffects.Poison)
                    .OfType<DamageOverTurnEffect>()
                    .ToList();

                var newTarget = field.GetRandomEntity(originalTarget);

                foreach (DamageOverTurnEffect stack in poisonStacks)
                {
                    var copy = (DamageOverTurnEffect)stack.Copy();
                    copy.Apply(new EffectApplyingContext { Caster = owner, Target = newTarget, Source = source, Damage = stack.DamagePerTick });
                }
            }
        }
    }
}
