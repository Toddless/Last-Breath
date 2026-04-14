namespace Crafting.Source.RequestHandlers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.MessageBus.Requests;
    using Core.Interfaces.UI;
    using UIElements;

    public class OpenCraftingItemsWindowRequestHandler(IUiElementsManager uIElementManager) : IRequestHandler<OpenCraftingItemsWindowRequest, IEnumerable<string>>
    {
        public async Task<IEnumerable<string>> HandleRequest(OpenCraftingItemsWindowRequest request)
        {
            var craftingItems = (CraftingItems)uIElementManager.OpenWindow(typeof(CraftingItems));
            craftingItems.Setup(request.TakenResources, request.Tags, request.IsSingleChose);
            var selected = await craftingItems.WaitForSelectionAsync();
            craftingItems.Close();
            return selected;
        }
    }
}
