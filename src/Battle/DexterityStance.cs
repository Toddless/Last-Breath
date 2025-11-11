namespace Battle
{
    using Core.Enums;
    using Core.Interfaces.Entity;

    public class DexterityStance(IEntity owner)
        : StanceBase(owner, effect: new StanceActivationEffect(), Stance.Dexterity)
    {
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
