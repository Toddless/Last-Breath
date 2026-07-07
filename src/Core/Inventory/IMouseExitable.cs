namespace Core.Inventory
{
    using System;

    public interface IMouseExitable
    {
        event Action? MouseExited;
    }
}
