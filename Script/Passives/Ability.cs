namespace Playground.Script.Passives.Attacks
{
    using Godot;

    public abstract partial class Ability : Node
    {

        public bool HaveISomethinToApplyAfterAttack { get; set; } = false;

        public int BuffLasts { get; set; } = 1;

        public int Cooldown { get; set; } = 4;

        public abstract void AfterBuffEnds(AttackComponent? attack = default, HealthComponent? health = default);

        public abstract void ActivateAbility(AttackComponent? attack = default, HealthComponent? health = default);

        public abstract void EffectAfterAttack(AttackComponent? attack = default, HealthComponent? health = default);
    }
}
