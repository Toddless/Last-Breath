namespace Battle.Source.EventHandlers
{
    using System;
    using Utilities;
    using Core.Interfaces.Data;
    using System.Threading.Tasks;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events;

    public class InitializeFightEventHandler(
        IUIElementProvider uiElementProvider,
        IGameServiceProvider gameServiceProvider,
        QueueScheduler queueScheduler)
        : IEventHandler<InitializeFightEvent<IEntity>>
    {
        public Task HandleAsync(InitializeFightEvent<IEntity> evnt)
        {
            try
            {
                // Что мы делаем:
                // 1. Подготавливаем худ
                // 2. Добавляем бойцов на сцену (определяем кто игрок, кто нпс)
                // 3. Определяем очередность ходов в раунде
                // 4. Начинаем бой
                var battle = uiElementProvider.CreateAndShowMainElement<BattleHud>();
                var fighters = evnt.Fighters;
                queueScheduler.AddMembers(fighters);
            }
            catch (Exception e)
            {
                Tracker.TrackException("Failed to initialize fight.", e, this);
            }

            return Task.CompletedTask;
        }
    }
}
