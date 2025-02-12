namespace Playground.Script.Scenes
{
    public interface IBattleContext
    {
        ICharacter Opponent { get; }
        ICharacter Self { get; }
    }
}