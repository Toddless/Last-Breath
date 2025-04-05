namespace Playground.Script.ScenesHandlers
{
    public interface IBattleContext
    {
        ICharacter Opponent { get; }
        ICharacter Player { get; }
    }
}