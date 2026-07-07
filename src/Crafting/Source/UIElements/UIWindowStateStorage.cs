namespace Crafting.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using Core.Views.UI;
    using Godot;

    public class UIWindowStateStorage : IUIWindowPositionStorage
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
