namespace Core.Views.UI
{
    using System;
    using Godot;

    /// <summary>
    /// Shared bodies of the project layer managers: Main and Battle carry identical thin nodes
    /// (a Godot-derived base class in Core double-registers in the engine's script map on
    /// assembly reload — see <see cref="HoverTooltipMotion"/>), so the logic lives here and the
    /// nodes delegate from their exports.
    /// </summary>
    public static class LayerBehavior
    {
        /// <summary>Deferred add of a UI element (any of the three layers); non-Control elements are ignored.</summary>
        public static void Show(Node? layer, object element)
        {
            if (element is Control control)
                layer?.CallDeferred(Node.MethodName.AddChild, control);
        }

        /// <summary>Clears the overlay layer. Region containers are scene furniture: their CONTENT
        /// is freed, the containers stay. True when anything was closed.</summary>
        public static bool CloseOverlays(Node? overlayLayer, params Node?[] regionContainers)
        {
            bool closedAny = false;
            foreach (var child in overlayLayer?.GetChildren() ?? [])
            {
                if (Array.IndexOf(regionContainers, child) >= 0)
                {
                    foreach (var popup in child.GetChildren())
                    {
                        popup.QueueFree();
                        closedAny = true;
                    }

                    continue;
                }

                child.QueueFree();
                closedAny = true;
            }

            return closedAny;
        }
    }
}
