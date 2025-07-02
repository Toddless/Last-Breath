namespace Playground.Components
{
    using System.Collections.Generic;
    using Godot;
    using Playground.Script;
    using Playground.Script.Abilities.Interfaces;

    public class DexterityStance : StanceBase
    {
        private StanceActivationEffect _effect;
        // private Ultimate _ultimate;
        private List<IAbility> _abilities = [];

        private int _points;
        private int _currentLevel;
        private int _maxLevel;

        public DexterityStance(ICharacter owner) : base(owner, new ComboPoints())
        {
            _effect = new StanceActivationEffect();
        }

        protected override void HandleAttackSucceed(ICharacter target)
        {
            var addAttack = Rnd.Randf() <= 0.7f;
            if (addAttack)
            {
                OnAttack(target);
            }
            GD.Print($"Is additional attack: {addAttack}");
            base.HandleAttackSucceed(target);
        }

        public override void OnActivate()
        {
            _effect.OnActivate(Owner);

        }
        public override void OnDeactivate()
        {
            _effect.OnDeactivate(Owner);
        }
    }
}
