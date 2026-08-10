namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    public class LightStep(
        int duration,
        int maxStacks,
        EffectValue value,
        string id = "Effect_Light_Step")
        : ParameterChangeEffect(
            id,
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.Evade,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None)
    {
        public override IEffect Copy() => new LightStep(Duration, MaxStacks, Authored, Id);
    }
}
