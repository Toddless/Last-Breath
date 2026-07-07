namespace Core.Events
{
    using System;

    public record OpenWindowMessage(Type WindowType) : IMessage;
}
