namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    public class DarkShroudEffect(int duration, EffectValue evadeValue)
        : CompositeParameterChangeEffect(id: "Effect_Dark_Shroud", duration, maxStacks: 1, changes:
        [
            new ParameterChange(EntityParameter.Evade, evadeValue, OperationType.Multiply, Priority.Weak)
        ], statusEffect: StatusEffects.Regeneration)
    {

        public override IEffect Copy() => new DarkShroudEffect(Duration, evadeValue);
    }
}
