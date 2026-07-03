namespace Core.Data
{
    using System.Collections.Generic;

    public interface IGameServiceProvider
    {
        T GetService<T>()
            where T : notnull;

        IEnumerable<T> GetServices<T>();

        T GetKeyedService<T>(string key)
            where T : notnull;
    }
}
