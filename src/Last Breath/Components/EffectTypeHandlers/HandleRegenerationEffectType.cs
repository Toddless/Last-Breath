namespace Playground.Components.EffectTypeHandlers
{
    using System.Collections.Generic;
    using System.Linq;
    using Playground.Script.Effects.Interfaces;

    public class HandleRegenerationEffectType : IHandleEffectTypeStrategy
    {
        public float HandleEffectType(IEnumerable<IEffect> effects)
        {
            return effects.Sum(effect => effect.Modifier);
        }
    }
}
