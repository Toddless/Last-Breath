namespace Playground.Script.Passives
{
    using Playground.Script.Passives.Attacks;
    public static class AbilityHandler
    {
        public static void ApplyAbility(ICharacter character, IAbility ability) => character.AppliedAbilities?.Add(ability);
    }
}
