namespace Battle.Source.Decorators
{
    using Core.Enums;
    using Core.Interfaces.Battle.Decorator;

    public class HealthBonusVsUndead : StatModuleDecorator
    {
        public HealthBonusVsUndead() : base(Parameter.Health, DecoratorPriority.Strong, "Health_BonusVsUndead") { }

        public override float GetValue() => base.GetValue() * 1.1f;
    }
}
