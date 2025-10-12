namespace Core.Interfaces.Inventory
{
    using System;

    public interface IMouseExitable
    {
        event Action? MouseExited;
    }
}
