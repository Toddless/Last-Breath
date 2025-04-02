namespace Playground.Script.Effects
{
    using Playground.Script.Effects.Interfaces;

    public class AbilityHandler
    {
        public static void ApplyAbility(ICharacter character, IAbility ability) => character.AppliedAbilities?.Add(ability);

        public static void ReflectOnApplyAbility(ICharacter character, IAbility ability) => character.EnemyHealth?.TakeDamage(50);
    }
}
