namespace Battle.Internal.Components
{
    using System.Collections.Generic;
    using Core.Interfaces;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;

    public class ModifierHandlerComponent : IModifierHandlerComponent
    {
        // TODO: Проверяем на наличие модификатора. Если нет, добавляем новый. Если есть, увеличиваем значение.
        private readonly List<IHealModifier> _healModifiers = [];
        private readonly List<IDamageModifier> _damageModifiers = [];
        private readonly List<IAttackModifier> _attackModifiers = [];

        public void Add<T>(T modifier)
        {
            switch (modifier)
            {
                case IHealModifier healModifier:
                    _healModifiers.Add(healModifier);
                    break;
                case IDamageModifier damageModifier:
                    _damageModifiers.Add(damageModifier);
                    break;
                case IAttackModifier attackModifier:
                    _attackModifiers.Add(attackModifier);
                    break;
            }
        }

        public void Remove<T>(T modifier)
        {
            switch (modifier)
            {
                case IHealModifier healModifier:
                    _healModifiers.Remove(healModifier);
                    break;
                case IDamageModifier damageModifier:
                    _damageModifiers.Remove(damageModifier);
                    break;
                case IAttackModifier attackModifier:
                    _attackModifiers.Remove(attackModifier);
                    break;
            }
        }

        public void Apply<T>(T context)
        {
            switch (context)
            {
                case IHealContext healCtx:
                    foreach (var modifier in _healModifiers)
                        modifier.Apply(healCtx);
                    break;
                case IDamageContext damageCtx:
                    foreach (var modifier in _damageModifiers)
                        modifier.Apply(damageCtx);
                    break;
                case IAttackContext attackCtx:
                    foreach (var modifier in _attackModifiers)
                        modifier.Apply(attackCtx);
                    break;
            }
        }
    }
}
