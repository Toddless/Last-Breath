namespace Core.Interfaces.Mediator
{
    using Godot;

    public interface IUiEventHandler<TEvent>
    {
        void Handle<T>(TEvent evnt, T? ui)
            where T : CanvasLayer;
    }
}
