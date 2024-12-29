namespace Playground.Script.Passives.Attacks
{
    using Playground.Script.Passives.Interfaces;

    public partial class Regeneration : Ability, ICanHeal
    {
        private float _regenerationAmount = 15;
        public Regeneration()
        {
            BuffLasts = 3;
        }

        public override void ActivateAbility(AttackComponent? attack = null, HealthComponent? health = null)
        {
            if(health == null)
            {
                return;
            }
            health.CurrentHealth += _regenerationAmount;
        }

        public override void AfterBuffEnds(AttackComponent? attack = null, HealthComponent? health = null)
        {
            BuffLasts = 3;
        }

        public override void EffectAfterAttack(AttackComponent? attack = null, HealthComponent? health = null)
        {
            return;
        }
    }
}
