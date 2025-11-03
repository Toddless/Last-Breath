namespace Core.Interfaces.UI
{
    using System;
    using Godot;

    public interface IRequireReposition
    {
        event Action<Control>? Reposition;
    }
}
