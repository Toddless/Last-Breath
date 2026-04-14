namespace Core.Interfaces.UI
{
    using System;

    public interface IRequirementUiConfiguration : IDisposable
    {
        void Configure(IRequirementUi ui);
    }
}
