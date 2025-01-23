namespace Playground.Script.Passives.Attacks
{
    using System;
    using Godot;
    using Playground.Components.Interfaces;
    using Playground.Script.Enums;

    public abstract partial class Ability<T, U> : RefCounted, IAbility
        where T : IGameComponent
        where U : ICharacter
    {
        private T? _target;
        private EffectType _effectType;
        protected U? TargetCharacter;
        public int BuffLasts { get; set; } = 1;
        public int Cooldown { get; set; } = 4;

        protected Ability()
        {
            TargetType = typeof(U);
        }

        public IGameComponent? TargetComponent
        {
            get; set;
        }

        public Type TargetType
        {
            get;
            private set;
        }

        // Methods from IAbility
        public void ActivateAbility() => ActivateAbility(_target);
        public void SetTargetCharacter(ICharacter? character) => SetTargetCharacter(character);

        // Methods from this class
        public abstract void ActivateAbility(T? component);
        public abstract void SetTargetCharacter(U? target);
    }
}
