namespace Core.Interfaces.Data
{
    using Godot;

    public interface IUIResourcesProvider
    {
        Resource? GetResource(string name);
    }
}
