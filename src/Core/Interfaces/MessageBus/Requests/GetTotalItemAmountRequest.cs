namespace Core.Interfaces.MessageBus.Requests
{
    using System.Collections.Generic;

    public  record GetTotalItemAmountRequest(IEnumerable<string> ItemsId) : IRequest<Dictionary<string, int>>
    {
    }
}
