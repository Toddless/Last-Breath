namespace Core.Interfaces.Data
{
   public interface IServiceProvider
    {
        T GetService<T>();
    }
}
