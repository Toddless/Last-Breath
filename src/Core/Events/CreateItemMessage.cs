namespace Core.Events
{
    public record CreateItemMessage(string RecipeId) : IMessage{}
}
