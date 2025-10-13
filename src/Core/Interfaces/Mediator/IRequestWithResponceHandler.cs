namespace Core.Interfaces.Mediator
{
    using System.Threading.Tasks;

    public interface IRequestWithResponceHandler<TRequest, TResponse>
        where TRequest : IRequestWithResponce<TResponse>
    {
        Task<TResponse> Handle(TRequest request);
    }
}
