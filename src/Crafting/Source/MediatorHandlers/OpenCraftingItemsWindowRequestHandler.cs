namespace Crafting.Source.MediatorHandlers
{
    using Crafting.Source;
    using System.Threading.Tasks;
    using Core.Interfaces.Mediator;
    using System.Collections.Generic;
    using Crafting.Source.UIElements;
    using Core.Interfaces.Mediator.Requests;

    public class OpenCraftingItemsWindowRequestHandler : IRequestHandler<OpenCraftingItemsWindowRequest, IEnumerable<string>>
    {
        private readonly UIElementProvider _uIElementProvider;

        public OpenCraftingItemsWindowRequestHandler(UIElementProvider uIElementProvider)
        {
            _uIElementProvider = uIElementProvider;
        }

        public async Task<IEnumerable<string>> Handle(OpenCraftingItemsWindowRequest request)
        {
            var craftinItems = _uIElementProvider.CreateSingleClosable<CraftingItems>();
            craftinItems.Setup(request.TakenResources);
            await craftinItems.ToSignal(craftinItems, Godot.Node.SignalName.Ready);
            var selected = await craftinItems.WaitForSelectionAsync();
            return selected;
        }
    }
}
