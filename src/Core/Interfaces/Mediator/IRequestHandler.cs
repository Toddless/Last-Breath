namespace Core.Interfaces.Mediator
{
    using System.Threading.Tasks;

    public interface IRequestHandler<in TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request);
    }
}
