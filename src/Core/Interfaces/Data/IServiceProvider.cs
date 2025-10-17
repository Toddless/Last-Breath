namespace Core.Interfaces.Data
{
    using System.Collections.Generic;

    public interface IServiceProvider
    {
        T GetService<T>();
        IEnumerable<T> GetServices<T>();
    }
}
