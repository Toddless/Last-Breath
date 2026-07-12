namespace Core.Views.UI
{
    using System;
    using Godot;

    /// <summary>
    /// Wires a hover tooltip to a source control: the popup appears after a short delay, lives
    /// while the pointer stays on the source and dies when it leaves. An Alt-pinned tooltip
    /// survives leaving the source (ShowPopup's same-type replacement and Esc still clear it).
    /// </summary>
    public static class HoverTooltip
    {
        private const float ShowDelaySeconds = 0.4f;

        /// <summary>
        /// <paramref name="show"/> creates and fills the popup (usually via IUiElementsManager.ShowPopup)
        /// and returns it; null means there is nothing to show (an empty slot).
        /// </summary>
        public static void Attach(Control source, Func<IPopup?> show)
        {
            bool inside;
            IPopup? current = null;

            source.MouseEntered += async () =>
            {
                inside = true;
                await source.ToSignal(source.GetTree().CreateTimer(ShowDelaySeconds), SceneTreeTimer.SignalName.Timeout);
                if (!inside || !GodotObject.IsInstanceValid(source) || !source.IsInsideTree()) return;
                current = show();
            };

            source.MouseExited += CloseCurrent;
            source.TreeExiting += CloseCurrent;
            return;

            void CloseCurrent()
            {
                inside = false;
                var popup = current;
                current = null;
                if (popup is not Node node || !GodotObject.IsInstanceValid(node)) return;
                if (popup is IHoverTooltipPopup { IsPinned: true }) return;
                popup.Close();
            }
        }
    }
}
