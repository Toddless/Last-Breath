namespace Crafting.Source.RequestHandlers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.MessageBus.Requests;

    public class GetTotalItemAmountRequestHandler : IRequestHandler<GetTotalItemAmountRequest, Dictionary<string, int>>
    {
        private readonly IInventory _inventory;

        public GetTotalItemAmountRequestHandler(IInventory inventory)
        {
            _inventory = inventory;
        }


        public Task<Dictionary<string, int>> HandleRequest(GetTotalItemAmountRequest request)
        {
            Dictionary<string, int> totalAmount = [];
            foreach (var item in request.ItemsId)
                totalAmount.Add(item, _inventory.GetTotalItemAmount(item));

            return Task.FromResult(totalAmount);
        }
    }
}
