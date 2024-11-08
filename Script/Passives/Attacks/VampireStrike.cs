namespace Playground.Script.Passives.Attacks
{
    using Godot;

    public partial class VampireStrike : Node, IPassivesAppliedAfterAttack
    {
        private float _leachPercentage = 0.1f;
        private int _cooldown = 4;

        public int Cooldown
        {
            get => _cooldown;
            set => _cooldown = value;
        }

        public float LeechPercentage
        {
            get => _leachPercentage;
            set => _leachPercentage = value;
        }

        public void ApplyAfterAttack(AttackComponent? attack = default, HealthComponent? health = default)
        {

            if (health != null)
            {

            }
        }
    }
}
