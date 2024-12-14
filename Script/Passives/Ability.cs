namespace Playground.Script.Passives.Attacks
{
    using Godot;

    public abstract partial class Ability : Node
    {

        public bool HaveISomethinToApplyAfterAttack { get; set; } = false;

        public int Cooldown { get; set; } = 4;

      //  public abstract void ApplyAfterAttack(AttackComponent? attack = default, HealthComponent? health = default);

        public abstract void AfterBuffEnds(AttackComponent? attack = default, HealthComponent? health = default);

        public abstract void BuffAttacks(AttackComponent? attack = default);

    }
}
