namespace Battle.Source.Decorators
{
    using Godot;
    using Core.Enums;
    using Core.Interfaces.Battle.Decorator;

    public class UnluckyCritDecoration(DecoratorPriority priority) : StatModuleDecorator(abilityParameter: EntityParameter.CriticalChance, priority, "Unlucky_Crit_Decorator")
    {
        public override float GetValue()
        {
            // call base twice, because we need to get both values modified by another decorators
            var firstRoll = base.GetValue();
            var secondRoll = base.GetValue();

            return Mathf.Min(firstRoll, secondRoll);
        }
    }
}
