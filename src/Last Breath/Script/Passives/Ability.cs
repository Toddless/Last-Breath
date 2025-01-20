namespace Playground.Script.Passives.Attacks
{
    using Godot;
    using Playground.Components.Interfaces;
    using Playground.Script.Enums;

    public abstract partial class Ability<T> : RefCounted, IAbility where T : IGameComponent
    {
        private T? _target;
        private GlobalRarity _rarity;
        private EffectType _effectType;
        public int BuffLasts { get; set; } = 1;

        public int Cooldown { get; set; } = 4;

        protected Ability(T component)
        {
            _target = component;
        }
        public IGameComponent? TargetComponent => _target;

        public GlobalRarity Rarity
        {
            get => _rarity;
            set => _rarity = value;
        }

        public EffectType EffectType
        {
            get =>_effectType;
            set => _effectType = value;
        }

        public void AfterBuffEnds() => AfterBuffEnds(_target);
        public void ActivateAbility() => ActivateAbility(_target);
        public void EffectAfterAttack() => EffectAfterAttack(_target);

        public abstract void AfterBuffEnds(T? component);

        public abstract void ActivateAbility(T? component);

        public abstract void EffectAfterAttack(T? component);
    }
}
