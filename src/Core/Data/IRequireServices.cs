namespace Core.Data
{
    public interface IRequireServices
    {
        void InjectServices(IGameServiceProvider provider);
    }
}
