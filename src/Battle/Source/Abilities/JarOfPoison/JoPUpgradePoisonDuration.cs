namespace Battle.Source.Abilities.JarOfPoison
{
    using Core.Battle.Abilities;
    using Core.Enums;

    public class JoPUpgradePoisonDuration(string id, string[] tags, int tier, float duration) :
        SimpleUpgrade<JarOfPoison>(id, tags, tier,
            new SimpleAbilityParameterDecorator(
                JarOfPoison.Parameters.PoisonDuration,
                Priority.Weak,
                OperationType.Add,
                duration,
                "Ability_Parameter_Decorator_JoP_Poison_Duration",
                id))
    {
    }
}
