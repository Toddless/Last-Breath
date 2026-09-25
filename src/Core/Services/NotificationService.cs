namespace Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Events;
    using Godot;
    using MessageBus;
    using MessageBus.Messages;
    using Views.UI;

    /// <summary>
    /// The single consumer of <see cref="SendNotificationMessageMessage"/>: creates a fresh popup
    /// via the registered factory and shows it on the Overlay layer, in the region mapped from
    /// the message category (category = domain semantics, region = presentation — the mapping
    /// lives here, in one place). At most <see cref="MaxVisiblePerRegion"/> notifications are
    /// visible per region; the rest queue up and appear as visible ones die. Esc clearing the
    /// overlay layer drops the queued backlog too. The project bootstrap must call
    /// <see cref="Setup"/> once the layer manager and the popup scene exist; until then
    /// messages are silently dropped.
    /// </summary>
    public class NotificationService : IMessageHandler<SendNotificationMessageMessage>
    {
        private const int MaxVisiblePerRegion = 3;

        private static readonly Dictionary<NotificationCategory, OverlayRegion> s_regions = new()
        {
            [NotificationCategory.System] = OverlayRegion.BottomRight, [NotificationCategory.Location] = OverlayRegion.TopCenter,
        };

        private readonly NotificationQueue _queue = new(MaxVisiblePerRegion);
        private readonly List<SendNotificationMessageMessage> _heldDuringBattle = [];
        private ILayerManager? _layers;
        private Func<Control?>? _popupFactory;
        private bool _battleRunning;

        /// <summary>Toasts wait out the battle: level-ups and reputation resolve instantly at
        /// logic time and used to pop while the fight was still animating. Held messages flush
        /// on BattleEndEvent — published after the final presentation gate.</summary>
        public NotificationService(IGameEventBus gameEventBus)
        {
            gameEventBus.Subscribe<BattleInitializedEvent>(OnBattleStarted);
            gameEventBus.Subscribe<BattleEndEvent>(OnBattleEnded);
        }

        private void OnBattleStarted(BattleInitializedEvent evnt) => _battleRunning = true;

        private void OnBattleEnded(BattleEndEvent evnt)
        {
            _battleRunning = false;
            var held = _heldDuringBattle.ToArray();
            _heldDuringBattle.Clear();
            foreach (var message in held)
                _ = HandleMessageAsync(message);
        }

        /// <summary>
        /// The factory returns the popup Control implementing <see cref="INotificationPopup"/>.
        /// It may return null (e.g. the popup scene isn't built yet) — the message is then dropped.
        /// Called again after a scene reload: the old layer subscription is released.
        /// </summary>
        public void Setup(ILayerManager? layers, Func<Control?> popupFactory)
        {
            if (_layers != null) _layers.OverlaysCleared -= _queue.Clear;
            _layers = layers;
            _popupFactory = popupFactory;
            _queue.Clear();
            layers?.OverlaysCleared += _queue.Clear;
        }

        public Task HandleMessageAsync(SendNotificationMessageMessage message)
        {
            if (_layers == null || _popupFactory == null) return Task.CompletedTask;
            if (_battleRunning)
            {
                _heldDuringBattle.Add(message);
                return Task.CompletedTask;
            }

            var region = s_regions.GetValueOrDefault(message.Category, OverlayRegion.BottomRight);
            var notification = new NotificationContent(message.Id, message.Values);
            if (_queue.TryTake(notification, region)) Show(notification, region);
            return Task.CompletedTask;
        }

        private void Show(NotificationContent content, OverlayRegion region)
        {
            // The layer node dies with the scene; a queued follow-up must not talk to a corpse
            if (_layers is GodotObject layerNode && !GodotObject.IsInstanceValid(layerNode)) return;

            var popup = _popupFactory?.Invoke();
            if (popup is not INotificationPopup notification)
            {
                ShowNext(region); // the slot must not leak when the factory bails out
                return;
            }

            notification.SetNotification(content, region);
            popup.TreeExiting += () => ShowNext(region);
            _layers?.ShowOverlay(notification);
            _ = notification.PlayLifecycleAsync(); // Timed lifetime dispatch: the shower drives it
        }

        private void ShowNext(OverlayRegion region)
        {
            NotificationContent? next = _queue.Release(region);
            if (next != null) Show(next, region);
        }
    }
}
