namespace Crafting.Source.MediatorHandlers
{
    using System.Threading.Tasks;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Inventory;
    using System.Collections.Generic;
    using Core.Interfaces.Mediator.Requests;

    public class GetTotalItemAmountRequestHandler : IRequestHandler<GetTotalItemAmountRequest, Dictionary<string, int>>
    {
        private readonly IInventory _inventory;

        public GetTotalItemAmountRequestHandler(IInventory inventory)
        {
            _inventory = inventory;
        }


        public Task<Dictionary<string, int>> Handle(GetTotalItemAmountRequest request)
        {
            Dictionary<string, int> totalAmount = [];
            foreach (var item in request.ItemsId)
                totalAmount.Add(item, _inventory.GetTotalItemAmount(item));

            return Task.FromResult(totalAmount);
        }
    }
}
