namespace Core.Services
{
    using System;
    using Enums;
    using Events;
    using Events.GameEvents;
    using Narrative.Dialogues;
    using Views.UI;

    /// <inheritdoc cref="IUiContextService"/>
    public class UiContextService : IUiContextService, Session.ISessionResettable
    {
        public UiContextService(IGameEventBus events, IPlayerAccessor playerAccessor, IDialogueService? dialogues = null)
        {
            events.Subscribe<BattleInitializedEvent>(_ => Switch(UiContext.Battle));
            events.Subscribe<BattleEndEvent>(evnt =>
                Switch(evnt.Results == BattleResults.PlayerLost ? UiContext.Defeated : UiContext.World));
            events.Subscribe<PlayerRevivedEvent>(_ => Switch(UiContext.World));
            events.Subscribe<PlayerFinalDeathEvent>(_ => Switch(UiContext.GameOver));

            // A fresh scene (new game, save load) registers a fresh player and always settles in the world.
            playerAccessor.PlayerChanged += _ => Switch(UiContext.World);

            // Optional: only Main runs conversations. Changed fires on every node, including the first;
            // the World guard keeps a dialogue ripped by a battle from downgrading the context.
            if (dialogues == null) return;
            dialogues.Changed += () =>
            {
                if (dialogues.IsActive && Current == UiContext.World) Switch(UiContext.Dialogue);
            };
            dialogues.Ended += () =>
            {
                if (Current == UiContext.Dialogue) Switch(UiContext.World);
            };
        }

        public UiContext Current { get; private set; } = UiContext.World;

        public event Action<UiContext>? ContextChanged;

        public void ResetSession() => Switch(UiContext.World);

        private void Switch(UiContext context)
        {
            if (Current == context) return;
            Current = context;
            ContextChanged?.Invoke(context);
        }
    }
}
