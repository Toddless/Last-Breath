namespace Core.Interfaces.Data
{
    using System.Collections.Generic;

    public interface IGameServiceProvider
    {
        T GetService<T>();
        IEnumerable<T> GetServices<T>();
    }
}
