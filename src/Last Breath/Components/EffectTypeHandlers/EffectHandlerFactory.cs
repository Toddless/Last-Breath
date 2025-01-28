namespace Playground.Components.EffectTypeHandlers
{
    using System.Collections.Generic;
    using Playground.Script.Enums;

    public class EffectHandlerFactory : IEffectHandlerFactory
    {
        private readonly Dictionary<EffectType, IHandleEffectTypeStrategy> _handlers;

        public EffectHandlerFactory()
        {
            _handlers = new()
                {
                     {EffectType.Poison, new HandlePoisonEffectType() },
                     {EffectType.Bleeding, new HandleBleedEffectType() },
                     {EffectType.Regeneration, new HandleRegenerationEffectType() },
                };
        }

        public IHandleEffectTypeStrategy GetHandler(EffectType type)
        {
            return _handlers[type];
        }
    }
}
