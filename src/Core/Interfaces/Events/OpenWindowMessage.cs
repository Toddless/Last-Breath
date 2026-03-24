namespace Core.Interfaces.Events
{
    using System;

    public record OpenWindowMessage(Type WindowType) : IMessage;
}
