namespace Playground.Script.Passives.Attacks
{
    using Playground.Script.Passives.Interfaces;

    public partial class Regeneration : Ability<HealthComponent>, ICanHeal
    {
        private float _regenerationAmount = 15;

        public Regeneration(HealthComponent component) : base(component)
        {
            BuffLasts = 3;
        }

        public override void ActivateAbility(HealthComponent? component)
        {
            if (component == null)
                return;
            component.CurrentHealth += _regenerationAmount;
        }
        public override void AfterBuffEnds(HealthComponent? component)
        {
            BuffLasts = 3;
        }

        public override void EffectAfterAttack(HealthComponent? component)
        {

        }
    }
}
