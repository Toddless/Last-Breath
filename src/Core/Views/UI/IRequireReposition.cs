namespace Core.Views.UI
{
    using System;
    using Godot;

    public interface IRequireReposition
    {
        event Action<Control>? Reposition;
    }
}
