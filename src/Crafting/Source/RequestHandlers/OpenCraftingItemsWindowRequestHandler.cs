namespace Crafting.Source.RequestHandlers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.MessageBus.Requests;
    using Core.Interfaces.UI;
    using UIElements;

    public class OpenCraftingItemsWindowRequestHandler : IRequestHandler<OpenCraftingItemsWindowRequest, IEnumerable<string>>
    {
        private readonly IUiElementsManager _uIElementManager;

        public OpenCraftingItemsWindowRequestHandler(IUiElementsManager uIElementManager)
        {
            _uIElementManager = uIElementManager;
        }

        public async Task<IEnumerable<string>> HandleRequest(OpenCraftingItemsWindowRequest request)
        {
            var craftingItems = (CraftingItems)_uIElementManager.OpenWindow(typeof(CraftingItems));
            craftingItems.Setup(request.TakenResources);
            var selected = await craftingItems.WaitForSelectionAsync();
            return selected;
        }
    }
}
