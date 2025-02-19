using System.Collections.Generic;
using Playground.Script.Scenes;

namespace Playground.Components.Interfaces
{
    public interface IConditionsFactory
    {
        List<ICondition> SetNewConditions(IBattleContext context);
    }
}
