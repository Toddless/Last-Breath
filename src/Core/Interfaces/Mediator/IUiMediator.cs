namespace Core.Interfaces.Mediator
{
    using Godot;

    public interface IUiMediator : IMediator
    {
        void Subscribe<T>(T? layer)
            where T: CanvasLayer;
    }
}
