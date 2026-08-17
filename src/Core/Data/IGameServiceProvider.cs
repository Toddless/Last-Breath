namespace Core.Data
{
    using System.Collections.Generic;
    using System.Linq;

    public interface IGameServiceProvider
    {
        T GetService<T>()
            where T : notnull;

        IEnumerable<T> GetServices<T>();

        /// <summary>The service if this composition holds one, and nothing if it does not. For callers
        /// that carry a working default for what they would have read — a project composing the module
        /// without it still runs. Not a way around registering a dependency: a caller that cannot work
        /// without a service asks for it through <see cref="GetService{T}"/> and fails loudly.</summary>
        T? TryGet<T>()
            where T : class => GetServices<T>().FirstOrDefault();
    }
}
