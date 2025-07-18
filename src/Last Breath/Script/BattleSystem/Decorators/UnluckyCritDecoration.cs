namespace Playground.Script.BattleSystem.Decorators
{
    using Godot;
    using Playground.Script.Enums;

    public class UnluckyCritDecoration(DecoratorPriority priority) : StatModuleDecorator(statModule: StatModule.CritChance, priority)
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
