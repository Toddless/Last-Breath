namespace Playground.Script.Passives.Attacks
{
    public partial class OneShotHeal : Ability<HealthComponent, BaseEnemy>
    {
        private readonly float _healAmount = 50;

        public override void ActivateAbility(HealthComponent? component)
        {
            component?.Heal(_healAmount);
        }

        public override void SetTargetCharacter(BaseEnemy? target) => throw new System.NotImplementedException();
    }
}
