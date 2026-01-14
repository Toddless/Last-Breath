namespace Core.Interfaces
{
    using Data;
    using UI;
    using Godot;

    public interface IEntityProvider
    {
        T CreateEntity<T>()
            where T : CharacterBody2D, IInitializable, IRequireServices;
    }
}
