namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>Ares' Blessing: one composite buff raising max health and health recovery by a percentage.</summary>
    public class AresBlessingEffect(int duration, float healthBonus, float recoveryBonus)
        : CompositeParameterChangeEffect(id: "Effect_Ares_Blessing", duration, maxStacks: 1,
            changes:
            [
                new ParameterChange(EntityParameter.Health, 1 + healthBonus, OperationType.Multiply, Priority.Weak),
                new ParameterChange(EntityParameter.HealthRecovery, 1 + recoveryBonus, OperationType.Multiply, Priority.Weak)
            ])
    {
        public override IEffect Copy() => new AresBlessingEffect(Duration, healthBonus, recoveryBonus);
    }
}
