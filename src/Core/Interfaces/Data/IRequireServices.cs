namespace Core.Interfaces.Data
{
    public interface IRequireServices
    {
        void InjectServices(IGameServiceProvider provider);
    }
}
