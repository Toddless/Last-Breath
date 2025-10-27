namespace Core.Interfaces.Mediator.Requests
{
    using System;

    public record OpenWindowEvent(Type WindowType, string? Parameter = default) : IEvent { }
}
