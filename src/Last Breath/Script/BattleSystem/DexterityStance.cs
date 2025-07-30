namespace Playground.Script.BattleSystem
{
    using Playground.Components;
    using Playground.Script;
    using Playground.Script.Enums;

    public class DexterityStance : StanceBase
    {
        public DexterityStance(ICharacter owner) : base(owner, new ComboPoints(), effect: new StanceActivationEffect(), Stance.Dexterity)
        {
            StanceSkillManager = new(this);
        }

        public override void OnActivate()
        {
            base.OnActivate();
            ActivationEffect.OnActivate(Owner);
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            ActivationEffect.OnDeactivate(Owner);
        }
    }
}
