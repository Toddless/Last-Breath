namespace LootGeneration.Internal
{
    using Core.Interfaces.Events;

    public record ExampleBattleStart(float X, float Y) : IGameEvent;
}
