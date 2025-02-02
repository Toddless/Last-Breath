namespace PlaygroundTest
{
    using Playground.Script.Passives;
    using Playground.Script.Passives.Attacks;
    using Playground.Script.Passives.Buffs;

    public class TestBuffAbility : Ability
    {
        public TestBuffAbility()
        {
            Effects = [new CriticalStrikeChanceBuff(string.Empty, string.Empty, 0.1f, 3), new CriticalStrikeDamageBuff(string.Empty, string.Empty, 0.1f, 3)];
            OnApplyAbilityHandler = AbilityHandler.ApplyAbility;
        }
    }
}
