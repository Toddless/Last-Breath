namespace LastBreath.World.Interactions.UI
{
    using System.Linq;
    using Core.Localization;
    using Godot;

    internal static class InteractionPresentation
    {
        /// <summary>Localization key shown in place of a key when an action has no usable input bound.</summary>
        private const string UnboundKey = "UI_Interaction_Unbound";

        /// <summary>Stands between the bound key and the label of a prompt.</summary>
        private const string PromptSeparator = " · ";

        /// <summary>Localization key of the Interact label: the prompt of several enabled actions and the menu's title.</summary>
        public const string InteractKey = "UI_Interaction_Interact";

        /// <summary>Whether the display server exposes keyboard layouts; physical keys resolve to layout labels only where it does.</summary>
        private static bool HasKeyboardLayouts => DisplayServer.KeyboardGetLayoutCount() > 0;

        public static Vector2 ScreenPoint(Node2D anchor)
        {
            var point = anchor.GetGlobalTransformWithCanvas().Origin;
            if (anchor.GetViewport().GetParent() is not SubViewportContainer container) return point;
            var viewportSize = anchor.GetViewport().GetVisibleRect().Size;
            var scale = new Vector2(container.Size.X / viewportSize.X, container.Size.Y / viewportSize.Y);
            return container.GetGlobalTransformWithCanvas() * (point * scale);
        }

        /// <summary>What the player reads to run an action: its bound input, then the localized label.</summary>
        public static string Prompt(string action, string labelKey) => $"{Binding(action)}{PromptSeparator}{Localization.Localize(labelKey)}";

        /// <summary>The first input bound to the action as the player reads it; an action with nothing to show reads as unbound.</summary>
        private static string Binding(string action)
        {
            string text = InputMap.ActionGetEvents(action).FirstOrDefault() switch
            {
                InputEventKey key => OS.GetKeycodeString(BoundKey(key)),
                { } input => input.AsText(),
                null => string.Empty,
            };
            return string.IsNullOrEmpty(text) ? Localization.Localize(UnboundKey) : text;
        }

        /// <summary>The key an event matches by, read in the engine's matching order: Latin keycode, physical position, key label.</summary>
        private static Key BoundKey(InputEventKey key)
        {
            if (key.Keycode != Key.None) return key.GetKeycodeWithModifiers();
            if (key.PhysicalKeycode != Key.None) return PhysicalKeyLabel(key);
            return key.GetKeyLabelWithModifiers();
        }

        /// <summary>The label printed on a physical key in the active layout, modifiers kept; the US QWERTY name where no layouts are exposed.</summary>
        private static Key PhysicalKeyLabel(InputEventKey key) => HasKeyboardLayouts
            ? DisplayServer.KeyboardGetLabelFromPhysical(key.PhysicalKeycode) | (Key)key.GetModifiersMask()
            : key.GetPhysicalKeycodeWithModifiers();
    }
}
