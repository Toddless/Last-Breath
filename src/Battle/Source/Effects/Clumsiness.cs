namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    // for now its kinda empty, later animations, sound effect, description will be added
    public class Clumsiness(
        int duration,
        int maxStacks,
        float value) :
        ParameterChangeEffect(id: "Effect_Clumsiness",
            duration,
            maxStacks,
            value: 1 - value,
            parameter: EntityParameter.Evade,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None)
    {
        public override bool IsHarmful => true;

        // Copy takes the primary-ctor value, not the transformed base Value — re-inverting would flip it.
        public override IEffect Copy() => new Clumsiness(Duration, MaxStacks, value);
    }
}
