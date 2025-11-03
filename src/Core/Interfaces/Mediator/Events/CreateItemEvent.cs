namespace Core.Interfaces.Mediator.Events
{
    public record CreateItemEvent(string RecipeId) : IEvent{}
}
