namespace Playground.Components.EffectTypeHandlers
{
    using System.Collections.Generic;
    using Playground.Script.Effects.Interfaces;

    public interface IHandleEffectTypeStrategy
    {
        float HandleEffectType(IEnumerable<IEffect> effects);
    }
}
