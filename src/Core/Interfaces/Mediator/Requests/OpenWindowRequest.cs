namespace Core.Interfaces.Mediator.Requests
{
    using System;

    public record OpenWindowRequest(Type WindowType, string? Parameter = default) : IRequest { }
}
