namespace Playground.Script.ScenesHandlers
{
    public class BattleContext(ICharacter opponent, ICharacter player) : IBattleContext
    {
        public ICharacter Opponent { get; private set; } = opponent;

        public ICharacter Player { get; private set; } = player;
    }
}
