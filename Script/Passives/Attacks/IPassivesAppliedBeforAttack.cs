namespace Playground.Script.Passives.Attacks
{

    public interface IPassivesAppliedBeforAttack
    {
        public int Cooldown
        {
            get;
        }

        void ApplyBeforeAttack(AttackComponent? attack);
    }
}
