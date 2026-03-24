namespace Core.Interfaces.Events
{
    public record CreateItemMessage(string RecipeId) : IMessage{}
}
