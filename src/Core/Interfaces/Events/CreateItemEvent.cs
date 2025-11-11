namespace Core.Interfaces.Events
{
    public record CreateItemEvent(string RecipeId) : IEvent{}
}
