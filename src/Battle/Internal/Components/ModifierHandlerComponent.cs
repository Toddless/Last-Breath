namespace Battle.Internal.Components
{
    using System.Collections.Generic;
    using Core.Interfaces;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;

    public class ModifierHandlerComponent : IModifierHandlerComponent
    {
        private readonly ModifierList<IAttackContext> _attackModifiers = new();
        private readonly ModifierList<IDamageContext> _damageModifiers = new();
        private readonly ModifierList<IHealContext> _healModifiers = new();
        private readonly ModifierList<IAbilityActivationContext> _abilityActivationModifiers = new();

        public void Add(IAttackModifier modifier) => _attackModifiers.Add(modifier);
        public void Add(IDamageModifier modifier) => _damageModifiers.Add(modifier);
        public void Add(IHealModifier modifier) => _healModifiers.Add(modifier);
        public void Add(IAbilityActivationModifier modifier) => _abilityActivationModifiers.Add(modifier);

        public void Remove(IAttackModifier modifier) => _attackModifiers.Remove(modifier);
        public void Remove(IDamageModifier modifier) => _damageModifiers.Remove(modifier);
        public void Remove(IHealModifier modifier) => _healModifiers.Remove(modifier);
        public void Remove(IAbilityActivationModifier modifier) => _abilityActivationModifiers.Remove(modifier);

        public void Apply(IAttackContext context) => _attackModifiers.Apply(context);
        public void Apply(IDamageContext context) => _damageModifiers.Apply(context);
        public void Apply(IHealContext context) => _healModifiers.Apply(context);
        public void Apply(IAbilityActivationContext context) => _abilityActivationModifiers.Apply(context);

        /// <summary>Priority-ordered list of context mutators for a single pipeline.</summary>
        private sealed class ModifierList<TContext>
        {
            private readonly List<IContextModifier<TContext>> _modifiers = [];

            public void Add(IContextModifier<TContext> modifier)
            {
                _modifiers.Add(modifier);
                _modifiers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }

            public void Remove(IContextModifier<TContext> modifier) => _modifiers.Remove(modifier);

            public void Apply(TContext context)
            {
                foreach (var modifier in _modifiers)
                    modifier.Apply(context);
            }
        }
    }
}
