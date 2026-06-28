namespace Battle.Source.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class CriticalChanceBuffEffect(
        int duration,
        int maxStacks,
        float value)
        : ParameterChangeEffect(
            id:"Effect_Critical_Chance_Buff",
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.CriticalChance,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None)
    {
        public override IEffect Copy() => throw new System.NotImplementedException();
    }
}
