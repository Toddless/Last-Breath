namespace Core.Views.UI
{
    using System.Linq;
    using Data;

    /// <summary>
    /// Resolving a service a composition may simply not have. <see cref="IGameServiceProvider.GetService{T}"/>
    /// is required-by-contract and throws, which is right for a system that cannot work without its
    /// dependency — a window is not one of them: the battle sandbox composes no bag and no passive
    /// tree, and a screen that showed nothing there is better than one that takes the scene down.
    /// </summary>
    public static class OptionalServices
    {
        public static T? Optional<T>(this IGameServiceProvider provider)
            where T : class => provider.GetServices<T>().FirstOrDefault();
    }
}
