namespace Core.Interfaces.Battle.Module
{
    using Enums;

    public interface IActionModule<in T>
    {
        ActionModule Parameter { get; }
        DecoratorPriority Priority { get; }

        void PerformModuleAction(T parameter);
    }
}
