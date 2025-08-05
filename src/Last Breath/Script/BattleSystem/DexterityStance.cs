namespace LastBreath.Script.BattleSystem
{
    using Core.Enums;
    using LastBreath.Components;
    using LastBreath.Script;

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
