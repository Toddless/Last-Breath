namespace Playground.Script.Scenes
{
    public class BattleContext(ICharacter opponent, ICharacter self) : IBattleContext
    {
        public ICharacter Opponent { get; private set; } = opponent;

        public ICharacter Self { get; private set; } = self;
    }
}
