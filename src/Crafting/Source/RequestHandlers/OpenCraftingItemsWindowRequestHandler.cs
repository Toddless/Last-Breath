namespace Crafting.Source.RequestHandlers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Interfaces.Data;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Mediator.Requests;
    using Crafting.Source.UIElements;

    public class OpenCraftingItemsWindowRequestHandler : IRequestHandler<OpenCraftingItemsWindowRequest, IEnumerable<string>>
    {
        private readonly IUIElementProvider _uIElementProvider;

        public OpenCraftingItemsWindowRequestHandler(IUIElementProvider uIElementProvider)
        {
            _uIElementProvider = uIElementProvider;
        }

        public async Task<IEnumerable<string>> Handle(OpenCraftingItemsWindowRequest request)
        {
            var craftingItems = _uIElementProvider.CreateSingleClosable<CraftingItems>();
            craftingItems.Setup(request.TakenResources);
            var selected = await craftingItems.WaitForSelectionAsync();
            return selected;
        }
    }
}
