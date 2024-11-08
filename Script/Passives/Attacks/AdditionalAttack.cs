namespace Playground.Script.Passives.Attacks
{
    using Godot;

    public partial class AdditionalAttack : Node, IPassivesAppliedAfterAttack
    {

        private int _cooldown = 4;

        public int Cooldown
        {
            get => _cooldown;
            set => _cooldown = value;
        }

        public void ApplyAfterAttack(AttackComponent? attack = null, HealthComponent? health = null)
        {

        }
    }
}
