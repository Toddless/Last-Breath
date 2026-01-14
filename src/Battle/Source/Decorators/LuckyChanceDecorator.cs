namespace Battle.Source.Decorators
{
    using Godot;
    using Core.Enums;
    using Core.Interfaces.Components.Decorator;

    public class LuckyChanceDecorator(DecoratorPriority priority, EntityParameter parameter) : EntityParameterModuleDecorator(parameter, priority, "Lucky_Chance_Decorator")
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
