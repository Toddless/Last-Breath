namespace Battle
{
    using Core.Enums;
    using Core.Interfaces.Entity;

    public class DexterityStance : StanceBase
    {
        public DexterityStance(IEntity owner) : base(owner, new ComboPoints(), effect: new StanceActivationEffect(), Stance.Dexterity)
        {
           // StanceSkillComponent = new StanceSkillComponent(this);
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
