namespace Core.Views.UI
{
    using System;
    using Godot;

    /// <summary>
    /// Wires a hover tooltip to a source control: the popup appears after a short delay, lives
    /// while the pointer stays on the source and dies when it leaves. An Alt-pinned tooltip
    /// survives leaving the source (ShowPopup's same-type replacement and Esc still clear it).
    /// Nothing opens while a drag is in the air — a card standing over the thing being dragged, or over
    /// the target it is aimed at, is in the way of the gesture rather than an answer to it.
    /// </summary>
    public static class HoverTooltip
    {
        internal const float ShowDelaySeconds = 0.4f;

        /// <summary>
        /// <paramref name="show"/> creates and fills the popup (usually via IUiElementsManager.ShowPopup)
        /// and returns it; null means there is nothing to show (an empty slot).
        /// </summary>
        public static void Attach(Control source, Func<IPopup?> show)
        {
            HoverTooltipHandle handle = Follow(source, _ => show());

            source.MouseEntered += () => handle.Target(source);
            source.MouseExited += () => handle.Target(null);
        }

        /// <summary>
        /// The same delay and the same death, for a surface whose hover targets are not controls of
        /// their own — a canvas that decides what is under the cursor itself. The caller says WHAT is
        /// hovered (<see cref="HoverTooltipHandle.Target"/>) and the handle answers when and whether to
        /// show it, so the rule about how a tooltip lives stays written once for the whole game.
        /// </summary>
        /// <param name="host">The control the tooltip belongs to; its death takes the tooltip with it.</param>
        /// <param name="show">Builds the popup for the current target, or null when that target has
        /// nothing to say.</param>
        public static HoverTooltipHandle Follow(Control host, Func<object?, IPopup?> show) => new(host, show);
    }

    /// <summary>
    /// A live hover tooltip whose target can be changed. Not a second copy of the rule: the delay and
    /// the lifetime dispatch live here, and <see cref="HoverTooltip.Attach"/> is three lines on top of
    /// it.
    /// </summary>
    public sealed class HoverTooltipHandle
    {
        private readonly Control _host;
        private readonly Func<object?, IPopup?> _show;

        /// <summary>Bumped by every retarget. The timer that wakes up compares it and gives up when it
        /// no longer matches: on a canvas the key changes faster than the delay expires, and a single
        /// "still inside" flag would let a stale wake-up open the previous node's card.</summary>
        private int _generation;

        private object? _key;
        private IPopup? _current;

        internal HoverTooltipHandle(Control host, Func<object?, IPopup?> show)
        {
            _host = host;
            _show = show;
            // The popup lives in the overlay layer, not under the host, so the host's death has to say so.
            host.TreeExiting += Cancel;
        }

        /// <summary>What is hovered now; null means nothing is. Repeating the current target does
        /// nothing, so a stream of mouse moves over one node is one timer and not a hundred.</summary>
        public void Target(object? key)
        {
            if (Equals(key, _key)) return;

            _key = key;
            _generation++;
            CloseCurrent();

            if (key != null) OpenAfterDelay(key, _generation);
        }

        /// <summary>Drops the target and whatever it opened — the same thing leaving the surface does.</summary>
        public void Cancel() => Target(null);

        private async void OpenAfterDelay(object key, int generation)
        {
            try
            {
                SceneTree? tree = _host.GetTree();
                if (tree == null) return;

                await _host.ToSignal(tree.CreateTimer(HoverTooltip.ShowDelaySeconds), SceneTreeTimer.SignalName.Timeout);

                if (generation != _generation) return;
                if (!GodotObject.IsInstanceValid(_host) || !_host.IsInsideTree()) return;

                // A drag in the air outranks every card: the engine hides its own tooltips then, and these
                // are not the engine's. One reading of that rule for the whole game — a surface guarding
                // its own would be the one surface that forgot to.
                if (_host.GetViewport()?.GuiIsDragging() == true) return;

                _current = _show(key);
            }
            catch (Exception exception)
            {
                Tracker.TrackException("Failed to show a hover tooltip", exception, _host);
            }
        }

        /// <summary>Lifetime dispatch: only WhileHovered popups die with the pointer. A pinned one
        /// temporarily behaves as Pinned and survives until unpin, Esc or an overlay clear.</summary>
        private void CloseCurrent()
        {
            IPopup? popup = _current;
            _current = null;

            if (popup is not Node node || !GodotObject.IsInstanceValid(node)) return;
            if (popup.Lifetime != PopupLifetime.WhileHovered) return;
            if (popup is IHoverTooltipPopup { IsPinned: true }) return;

            popup.Close();
        }
    }
}
