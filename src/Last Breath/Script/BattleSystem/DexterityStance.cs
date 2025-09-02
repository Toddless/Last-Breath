namespace LastBreath.Script.BattleSystem
{
    using Core.Enums;
    using Core.Interfaces;
    using LastBreath.Components;

    public class DexterityStance : StanceBase
    {
        public DexterityStance(ICharacter owner) : base(owner, new ComboPoints(), effect: new StanceActivationEffect(), Stance.Dexterity)
        {
            StanceSkillComponent = new StanceSkillComponent(this);
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
