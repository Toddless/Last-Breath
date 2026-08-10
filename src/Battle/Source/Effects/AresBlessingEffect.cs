namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>Ares' Blessing: one composite buff raising max health and health recovery by a percentage.</summary>
    public class AresBlessingEffect(int duration, EffectValue healthBonus, EffectValue recoveryBonus)
        : CompositeParameterChangeEffect(id: "Effect_Ares_Blessing", duration, maxStacks: 1,
            changes:
            [
                new ParameterChange(EntityParameter.Health, healthBonus, OperationType.Multiply, Priority.Weak, EffectValueShape.ShareGained),
                new ParameterChange(EntityParameter.HealthRecovery, recoveryBonus, OperationType.Multiply, Priority.Weak, EffectValueShape.ShareGained)
            ])
    {
        public override IEffect Copy() => new AresBlessingEffect(Duration, healthBonus, recoveryBonus);
    }
}
