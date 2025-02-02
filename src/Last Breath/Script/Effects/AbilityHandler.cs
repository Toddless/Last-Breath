namespace Playground.Script.Passives
{
    using Playground;
    using Playground.Script.Effects.Interfaces;
    public static class AbilityHandler
    {
        public static void ApplyAbility(ICharacter character, IAbility ability) => character.AppliedAbilities?.Add(ability);
    }
}
