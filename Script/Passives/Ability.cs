namespace Playground.Script.Passives.Attacks
{
    using Godot;

    public abstract partial class Ability : Node
    {

        public bool HaveISomethinToApplyAfterAttack { get; set; } = false;

        public int Cooldown { get; set; } = 4;

        public abstract void ApplyAfterAttack(AttackComponent? attack = default, HealthComponent? health = default);

        public abstract void ApplyBeforAttack(AttackComponent? attack, HealthComponent? health);

        public abstract void ApplyAfterBuffEnds(AttackComponent? attack = default, HealthComponent? health = default);
    }
}
