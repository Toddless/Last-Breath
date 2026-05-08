namespace Core.Interfaces.Components.Module
{
    using Enums;

    public interface IActionModule<in T>
    {
        ActionModule Parameter { get; }
        Priority Priority { get; }

        void PerformModuleAction(T parameter);
    }
}
