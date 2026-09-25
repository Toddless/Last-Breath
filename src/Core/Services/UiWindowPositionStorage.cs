namespace Core.Services
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Views.UI;

    /// <summary>
    /// Window positions survive the fresh-instance UI policy here: draggable windows save
    /// their position on close/drag and restore it on the next open.
    /// </summary>
    public class UiWindowPositionStorage : IUIWindowPositionStorage
    {
        private readonly Dictionary<Type, Vector2> _windowPositions = [];

        public void SavePosition<T>(Vector2 position)
            where T : Control => _windowPositions[typeof(T)] = position;

        public Vector2? GetPosition<T>()
            where T : Control
        {
            if (_windowPositions.TryGetValue(typeof(T), out var position))
                return position;
            return default;
        }
    }
}
