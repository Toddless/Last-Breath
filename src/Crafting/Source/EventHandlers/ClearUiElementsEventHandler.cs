namespace Crafting.Source.EventHandlers
{
    using System.Threading.Tasks;
    using Core.Interfaces.Data;
    using Core.Interfaces.Events;

    public class ClearUiElementsEventHandler(IUiElementProvider provider) : IEventHandler<ClearUiElementsEvent>
    {
        public Task HandleAsync(ClearUiElementsEvent evnt)
        {
            provider.ClearSource(evnt.Source);
            return Task.CompletedTask;
        }
    }
}
