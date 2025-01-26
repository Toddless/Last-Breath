namespace Playground.Script.Passives
{
    using Playground.Script.Passives.Attacks;
    using Playground.Script.Passives.Debuffs;

    public class TestDebuffAbility : Ability
    {
        public TestDebuffAbility()
        {
            Effects = [new HealthDebuf(string.Empty, string.Empty, -0.1f, 3), new StrikeDamageDebuff(string.Empty, string.Empty, -0.1f, 3)];
            OnReceiveAbilityHandler = AbilityHandler.ApplyAbility;
        }
    }
}
