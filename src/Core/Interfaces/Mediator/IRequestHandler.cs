namespace Core.Interfaces.Mediator
{
    public interface IRequestHandler<TRequest>
    {
        void Handle(TRequest request);
    }
}
