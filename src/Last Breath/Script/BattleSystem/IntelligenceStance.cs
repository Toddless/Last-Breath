namespace LastBreath.Script.BattleSystem
{
    using Core.Enums;
    using Core.Interfaces.Entity;

    public class IntelligenceStance : StanceBase
    {
        public IntelligenceStance(IEntity owner) : base(owner, resource: new Mana(), effect: new StanceActivationEffect(), Stance.Intelligence)
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
