namespace Playground.Script.Passives.Attacks
{
    public partial class OneShotHeal : Ability<HealthComponent>
    {
        private float _healAmoint = 50;

        public OneShotHeal(HealthComponent component) : base(component)
        {
        }

        public override void ActivateAbility(HealthComponent? component)
        {
            component?.Heal(_healAmoint);
        }

        public override void AfterBuffEnds(HealthComponent? component)
        {

        }

        public override void EffectAfterAttack(HealthComponent? component)
        {

        }
    }
}
