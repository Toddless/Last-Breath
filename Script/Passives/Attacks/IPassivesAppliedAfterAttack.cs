namespace Playground.Script.Passives.Attacks
{
    internal interface IPassivesAppliedAfterAttack
    {
        public int Cooldown
        {
            get; set;
        }

        void ApplyAfterAttack(AttackComponent? attack = default, HealthComponent? health = default);
    }
}
