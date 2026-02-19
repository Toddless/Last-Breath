namespace Battle.Services
{
    using Core.Data;
    using Godot;
    using Core.Interfaces;
    using Core.Interfaces.UI;

    public class EntityProvider(IGameServiceProvider gameServiceProvider) : IEntityProvider
    {
        public T CreateEntity<T>()
            where T : CharacterBody2D, IInitializable, IRequireServices
        {
            var character = T.Initialize().Instantiate<T>();
            character.InjectServices(gameServiceProvider);

            return character;
        }
    }
}
