namespace LastBreath.Components
{
    using System.Collections.Generic;
    using Core.Interfaces;
    using Core.Interfaces.Components;

    public class ModifierHandlerComponent : IModifierHandlerComponent
    {
        private readonly List<IHealModifier> _healModifiers = [];
        private readonly List<IDamageModifier> _damageModifiers = [];

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
            }
        }
    }
}
