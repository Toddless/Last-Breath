namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    // for now its kinda empty, later animations, sound effect, description will be added
    public class Clumsiness(
        int duration,
        int maxStacks,
        EffectValue value) :
        ParameterChangeEffect(id: "Effect_Clumsiness",
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.Evade,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None,
            shape: EffectValueShape.ShareLost)
    {
        public override bool IsHarmful => true;

        // Copy takes the authored share of evade lost, not the share left standing after it: the base
        // scales before it inverts, and a copy fed the inverted figure would invert it a second time.
        public override IEffect Copy() => new Clumsiness(Duration, MaxStacks, value);
    }
}
