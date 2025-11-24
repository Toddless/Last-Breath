namespace Battle.Source.Decorators
{
    using Godot;
    using Core.Enums;
    using Core.Interfaces.Battle.Decorator;

    public class LuckyCritDecoration(DecoratorPriority priority) : EntityParameterModuleDecorator(parameter: EntityParameter.CriticalChance, priority, "Lucky_Crit_Decorator")
    {
        public override float GetValue()
        {
            // call base twice, because we need to get both values modified by another decorators
            float firstRoll = base.GetValue();
            float secondRoll = base.GetValue();

            return Mathf.Min(firstRoll, secondRoll);
        }
    }
}
