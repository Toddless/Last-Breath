namespace Core.Data
{
    using Godot;

    public interface IUIResourcesProvider
    {
        Resource? GetResource(string name);
    }
}
