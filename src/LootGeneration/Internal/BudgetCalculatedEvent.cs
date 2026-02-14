namespace LootGeneration.Internal
{
    using Core.Interfaces.Events;

    internal record BudgetCalculatedEvent(float Budget) : IGameEvent;
}
