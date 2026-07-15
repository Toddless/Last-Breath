namespace Core.Entity.Components.Module
{
    using Enums;

    public interface IActionModule<in T>
    {
        ActionModule Parameter { get; }
        Priority Priority { get; }

        void PerformModuleAction(T parameter);
    }
}
