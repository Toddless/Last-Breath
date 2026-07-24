namespace Battle.Source.UIElements
{
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// The expanded NPC card behind the over-head mini bar (hover to open, leaves with the cursor):
    /// the same CharacterBar scene the player's HUD card uses, filled from the snapshot the bar
    /// accumulated — identity, exact vitals, difficulty modifiers and the full effect list.
    /// Alt pins the card in place for study: the mouse filters open up and the effect icons become
    /// hoverable (their tooltips carry the description and remaining duration).
    /// Doubles as the future "inspect NPC" item's window (refinement #50).
    /// </summary>
    public partial class NpcInspectPopup : Control, IHoverTooltipPopup
    {
        private CharacterBar? _card;

        public PopupLifetime Lifetime => PopupLifetime.WhileHovered;

        public OverlayRegion Region => OverlayRegion.Cursor;

        public bool IsPinned { get; private set; }

        public void Close() => QueueFree();

        // ShowFor runs before the layer's deferred AddChild puts the popup in the tree,
        // so the card is built there and the cursor-follow wiring waits for _Ready.
        public override void _Ready()
        {
            SetAnchorsPreset(LayoutPreset.FullRect);
            HoverTooltipMotion.Setup(this, _card);
        }

        public override void _Process(double delta)
        {
            if (!IsPinned) HoverTooltipMotion.Follow(this, _card);
        }

        public override void _UnhandledKeyInput(InputEvent @event)
        {
            bool pinned = HoverTooltipMotion.TogglePin(@event, IsPinned);
            // Un-pinning dismisses the card: once the pointer has left the mini bar a pinned popup
            // is orphaned (HoverTooltip.Attach dropped its handle on MouseExited), so resuming
            // cursor-follow would make it chase the mouse forever with nothing left to close it.
            if (IsPinned && !pinned) { Close(); return; }
            IsPinned = pinned;
            if (IsPinned) OpenMouseFilters();
        }

        /// <summary>Pinned popups close on a click outside the card.</summary>
        public override void _UnhandledInput(InputEvent @event)
        {
            if (!IsPinned) return;
            if (@event is not InputEventMouseButton { Pressed: true }) return;
            if (_card != null && _card.GetGlobalRect().HasPoint(_card.GetGlobalMousePosition())) return;
            Close();
        }

        public void ShowFor(NpcBattleBar source)
        {
            if (_card == null)
            {
                _card = CharacterBar.Initialize().Instantiate<CharacterBar>();
                AddChild(_card);
            }

            _card.SetIdentity(source.DisplayNameText, source.FractionText);
            if (source.Level > 0) _card.SetLevel(source.Level);
            _card.SetInitialValues(source.MaxMana, source.Mana, source.MaxHealth, source.Health,
                source.MaxBarrier, source.Barrier);
            _card.SetModifiers(source.ModifierNames);
            _card.SetEffects(source.Effects);
            if (source.IsDead) _card.SetDead();
            _card.ResetSize(); // shrink to the freshly filled content before the first clamped placement
        }

        /// <summary>The card ignores the mouse while it follows the cursor; the pinned state opens
        /// the filters so the effect icons receive hover and their tooltips fire.</summary>
        private void OpenMouseFilters()
        {
            MouseFilter = MouseFilterEnum.Pass;
            _card?.MouseFilter = MouseFilterEnum.Stop;
        }
    }
}
