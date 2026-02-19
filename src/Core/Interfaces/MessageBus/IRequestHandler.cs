namespace Core.Interfaces.MessageBus
{
    using System.Threading.Tasks;

    public interface IRequestHandler<in TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        Task<TResponse> HandleRequest(TRequest request);
    }
}
