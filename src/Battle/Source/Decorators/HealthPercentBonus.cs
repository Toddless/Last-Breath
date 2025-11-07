namespace Battle.Source.Decorators
{
    using Core.Enums;
    using Core.Interfaces.Battle.Decorator;

    public class HealthPercentBonus : StatModuleDecorator
    {
        public HealthPercentBonus() : base(parameter: Parameter.Health, priority: DecoratorPriority.Weak, "Health_Percent_Decorator") { }

        public override float GetValue() => base.GetValue() * 1.2f;
    }
}
