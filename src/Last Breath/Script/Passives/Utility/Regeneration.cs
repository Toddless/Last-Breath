namespace Playground.Script.Passives.Attacks
{
    using Playground.Script.Passives.Interfaces;

    public partial class Regeneration : Ability<HealthComponent, BaseEnemy>, ICanHeal
    {
        private float _regenerationAmount = 15;


        public override void ActivateAbility(HealthComponent? component)
        {
            if (component == null)
                return;
            component.CurrentHealth += _regenerationAmount;
        }

        public override void SetTargetCharacter(BaseEnemy? target) => throw new System.NotImplementedException();
    }
}
