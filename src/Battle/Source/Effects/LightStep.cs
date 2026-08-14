namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>"Лёгкий шаг": raises the bearer's evasion by <c>value</c> (0.15 = +15%) per stack.
    /// Counterpart of <see cref="Clumsiness"/>.</summary>
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
            statusEffect: StatusEffects.None,
            shape: EffectValueShape.ShareGained)
    {
        // Copy takes the authored share GAINED, not the multiplier the base derives from it: the base
        // scales before it adds the one, and a copy fed the derived figure would add it a second time.
        public override IEffect Copy() => new LightStep(Duration, MaxStacks, Authored, Id);
    }
}
