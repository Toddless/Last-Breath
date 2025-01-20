namespace Playground.Script.Passives.Attacks
{
    public partial class OneShotHeal : Ability<HealthComponent>
    {
        private readonly float _healAmount = 50;

        public OneShotHeal(HealthComponent component) : base(component)
        {
        }

        public override void ActivateAbility(HealthComponent? component)
        {
            component?.Heal(_healAmount);
        }

        public override void AfterBuffEnds(HealthComponent? component)
        {

        }

        public override void EffectAfterAttack(HealthComponent? component)
        {

        }
    }
}
