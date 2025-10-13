namespace Core.Interfaces.Mediator
{
    using System;

    public interface IUiMediator : IMediator
    {
        event Action? UpdateUi;
        void RaiseUpdateUi();
    }
}
