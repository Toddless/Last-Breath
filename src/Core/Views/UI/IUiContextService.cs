namespace Core.Views.UI
{
    using System;

    /// <summary>Single owner of the current <see cref="UiContext"/>; switches on existing bus
    /// events (battle start/end, dialogue, death) — consumers never derive the state themselves.</summary>
    public interface IUiContextService
    {
        UiContext Current { get; }

        event Action<UiContext>? ContextChanged;
    }
}
