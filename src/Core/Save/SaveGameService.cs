namespace Core.Save
{
    using System;
    using Core;
    using Battle;
    using Events;
    using MessageBus;
    using Services;
    using Godot;

    public class SaveGameService : ISaveGameService
    {
        private const int Slots = 10;
        private const string SavedNotificationId = "Notification_Game_Saved";

        private readonly ISaveManager _manager;
        private readonly ISaveStorage _storage;
        private readonly IPlayerAccessor _playerAccessor;
        private readonly IMartialArtMastery _mastery;
        private readonly INpcPopulationService _population;
        private readonly IGameMessageBus _messageBus;
        private SaveFile? _pendingLoad;

        public SaveGameService(
            ISaveManager manager,
            ISaveStorage storage,
            IPlayerAccessor playerAccessor,
            IMartialArtMastery mastery,
            INpcPopulationService population,
            IGameMessageBus messageBus)
        {
            _manager = manager;
            _storage = storage;
            _playerAccessor = playerAccessor;
            _mastery = mastery;
            _population = population;
            _messageBus = messageBus;
            _manager.SectionRestoreFailed += OnSectionRestoreFailed;
        }

        public int SlotCount => Slots;

        /// <summary>No saving in battle or while lying dead (checkpoints are unreachable in both — this is the backstop).</summary>
        public bool CanSave => _playerAccessor.Player is { IsFighting: false, IsAlive: true };
        public bool HasPendingLoad => _pendingLoad != null;

        public bool HasSave(int slot) => _storage.Exists(slot);
        public SaveMetadata? PeekSlot(int slot) => _storage.ReadMetadata(slot);

        public void SaveToSlot(int slot)
        {
            if (!CanSave) return;

            var metadata = new SaveMetadata
            {
                Name = $"Slot {slot}",
                SavedAtUtc = DateTime.UtcNow,
                Location = CurrentSceneName(),
                MasteryLevel = _mastery.CurrentLevel
            };

            // Capture over the previous file: sections owned by other modules survive the rewrite.
            _storage.Write(slot, _manager.Capture(metadata, _storage.Load(slot)));
            _messageBus.PublishMessageAsync(new SendNotificationMessageMessage(SavedNotificationId));
        }

        public void DeleteSlot(int slot) => _storage.Delete(slot);

        public bool RequestLoad(int slot)
        {
            var file = _storage.Load(slot);
            if (file == null || _playerAccessor.Player is not Node playerNode) return false;

            _pendingLoad = file;
            _population.Reset(); // the reload frees NPC nodes without final-death events
            Engine.TimeScale = 1; // loading from the game-over screen: the death fast-forward must not leak
            playerNode.GetTree().CallDeferred(SceneTree.MethodName.ReloadCurrentScene);
            return true;
        }

        public void ApplyPendingLoad()
        {
            if (_pendingLoad == null) return;
            var file = _pendingLoad;
            _pendingLoad = null; // cleared first: a throwing section must not re-apply forever
            _manager.Restore(file);
        }

        private string CurrentSceneName() =>
            _playerAccessor.Player is Node playerNode ? playerNode.GetTree().CurrentScene.Name : string.Empty;

        private static void OnSectionRestoreFailed(string sectionId, Exception e) =>
            Tracker.TrackException($"Save section '{sectionId}' failed to restore", e);
    }
}
