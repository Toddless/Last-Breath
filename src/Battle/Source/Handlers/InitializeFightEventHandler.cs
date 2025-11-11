namespace Battle.Source.Handlers
{
    using System;
    using Utilities;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Interfaces.Data;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events;

    public class InitializeFightEventHandler(
        IUIElementProvider uiElementProvider,
        IGameServiceProvider gameServiceProvider)
        : IEventHandler<InitializeFightEvent<IEntity>>
    {
        public Task HandleAsync(InitializeFightEvent<IEntity> evnt)
        {
            try
            {
                // TODO: What if we already have some main element?
                var battle = uiElementProvider.CreateAndShowMainElement<BattleHud>();
                var fighters = evnt.Fighters.Where(x => x is not Player).ToList();
                if (fighters.Count > 1)
                    battle.SetFighterQueue(fighters);

                var turnScheduler = gameServiceProvider.GetService<QueueScheduler>();
                turnScheduler.AddMembers(fighters);
            }
            catch (Exception e)
            {
                Tracker.TrackException("Failed to initialize fight.", e, this);
            }

            return Task.CompletedTask;
        }
    }
}
