namespace LootGeneration.Internal
{
    using Core.Events;

    public record ExampleBattleStart(float X, float Y) : IGameEvent;
}
