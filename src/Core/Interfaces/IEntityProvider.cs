namespace Core.Interfaces
{
    using Data;
    using Godot;
    using UI;

    public interface IEntityProvider
    {
        T CreateEntity<T>()
            where T : CharacterBody2D, IInitializable, IRequireServices;
    }
}
