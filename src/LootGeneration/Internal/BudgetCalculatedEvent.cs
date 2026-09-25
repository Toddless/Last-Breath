namespace LootGeneration.Internal
{
    using Core.Events;

    internal record BudgetCalculatedEvent(float Budget) : IGameEvent;
}
