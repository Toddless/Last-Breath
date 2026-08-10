namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>"Благословение гигантов": increases the target's maximum health by <c>value</c> (0.15 = +15%)
    /// per stack. Mirror of <see cref="WitheringCurseEffect"/>.</summary>
    public class GiantsBlessingEffect(int duration, int maxStacks, float value)
        : ParameterChangeEffect(id: "Effect_Giants_Blessing",
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.Health,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None,
            shape: EffectValueShape.ShareGained)
    {
        // Copy takes the primary-ctor value, not the transformed base Value — re-transforming would double it.
        public override IEffect Copy() => new GiantsBlessingEffect(Duration, MaxStacks, value);
    }
}
