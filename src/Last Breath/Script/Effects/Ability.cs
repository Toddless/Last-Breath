namespace Playground.Script.Passives.Attacks
{
    using System;
    using System.Collections.Generic;
    using Playground.Script.Effects.Interfaces;

    public abstract class Ability : IAbility
    {
        private List<IEffect> _effects;
        public Action<ICharacter, IAbility> OnReceiveAbilityHandler { get; set; } = DoNothing;
        public int Cooldown { get; set; } = 4;
        protected Ability()
        {
            _effects ??= [];
        }
        public List<IEffect> Effects
        {
            get => _effects;
            set => _effects = value;
        }

        public virtual void ActivateAbility(ICharacter character) => OnReceiveAbilityHandler.Invoke(character, this);

        private static void DoNothing(ICharacter character, IAbility ability)
        {
        }
    }
}
