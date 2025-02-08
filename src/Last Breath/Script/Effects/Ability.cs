namespace Playground.Script.Passives.Attacks
{
    using System;
    using System.Collections.Generic;
    using Playground;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Scenes;

    public abstract class Ability : IAbility
    {
        private List<IEffect> _effects;
        public Action<ICharacter, IAbility> AbilityHandler { get; set; } = DoNothing;
        public int Cooldown { get; set; } = 4;
        protected Ability(List<IEffect> effects)
        {
            _effects = effects;
        }
        public List<IEffect> Effects
        {
            get => _effects;
            set => _effects = value;
        }

        public virtual void ActivateAbility(IBattleContext context) => AbilityHandler.Invoke(SetTarget(context), this);

        protected virtual ICharacter SetTarget(IBattleContext context) => context.Opponent;

        private static void DoNothing(ICharacter character, IAbility ability)
        {
        }
    }
}
