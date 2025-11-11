namespace Core.Interfaces
{
    using Core.Interfaces.Data;
    using Core.Interfaces.UI;
    using Godot;

    public interface IEntityProvider
    {
        T CreateEntity<T>()
            where T : CharacterBody2D, IInitializable, IRequireServices;
    }
}
