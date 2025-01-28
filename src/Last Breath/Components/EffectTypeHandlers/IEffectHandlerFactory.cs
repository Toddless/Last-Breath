using Playground.Script.Enums;

namespace Playground.Components.EffectTypeHandlers
{
    public interface IEffectHandlerFactory
    {
        IHandleEffectTypeStrategy GetHandler(EffectType type);
    }
}