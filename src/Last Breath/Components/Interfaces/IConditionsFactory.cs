namespace Playground.Components.Interfaces
{
    using System.Collections.Generic;
    using Playground.Script.ScenesHandlers;

    public interface IConditionsFactory
    {
        List<ICondition> SetNewConditions(IBattleContext context);
    }
}
