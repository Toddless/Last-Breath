namespace Battle.Source.EventHandlers
{
    using System;
    using Utilities;
    using Core.Interfaces.Data;
    using System.Threading.Tasks;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events;

    public class InitializeFightEventHandler(
        IGameServiceProvider gameServiceProvider)
        : IEventHandler<InitializeFightEvent<IEntity>>
    {
        public Task HandleAsync(InitializeFightEvent<IEntity> evnt)
        {
            try
            {
                var battleArena = BattleArena.Initialize().Instantiate<BattleArena>();

            }
            catch (Exception e)
            {
                Tracker.TrackException("Failed to initialize fight.", e, this);
            }

            return Task.CompletedTask;
        }
    }
}
