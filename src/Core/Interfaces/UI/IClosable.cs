namespace Core.Interfaces.UI
{
    using System;

    /// <summary>
    /// If a control uses this interface, you should not call QueueFree yourself, call Close.Invoke() within control instead.
    /// </summary>
    public interface IClosable
    {
        event Action? Close;
    }
}
