namespace Core.Save
{
    using System;
    using Ai.World.Raids;
    using Core;
    using Battle;
    using MessageBus;
    using Services;
    using Godot;
    using MessageBus.Messages;

    public class SaveGameService : ISaveGameService, Session.ISessionResettable
    {
        private const int Slots = 10;
        private const string SavedNotificationId = "Notification_Game_Saved";

        private readonly ISaveManager _manager;
        private readonly ISaveStorage _storage;
        private readonly IPlayerAccessor _playerAccessor;
        private readonly IMartialArtMastery _mastery;
        private readonly INpcPopulationService _population;
        private readonly IGameMessageBus _messageBus;
        private readonly IRaidService? _raids;
        private SaveFile? _pendingLoad;

        public SaveGameService(
            ISaveManager manager,
            ISaveStorage storage,
            IPlayerAccessor playerAccessor,
            IMartialArtMastery mastery,
            INpcPopulationService population,
            IGameMessageBus messageBus,
            IRaidService? raids = null)
        {
            _manager = manager;
            _storage = storage;
            _playerAccessor = playerAccessor;
            _mastery = mastery;
            _population = population;
            _messageBus = messageBus;
            _raids = raids;
            _manager.SectionRestoreFailed += OnSectionRestoreFailed;
        }

        public int SlotCount => Slots;

        /// <summary>No saving in battle, while lying dead, or under an active raid (checkpoints are unreachable in all — this is the backstop).</summary>
        public bool CanSave => _playerAccessor.Player is { IsFighting: false, IsAlive: true } && _raids?.IsRaidActive != true;
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

            _storage.Write(slot, _manager.Capture(metadata));
            _messageBus.PublishMessageAsync(new SendNotificationMessageMessage(SavedNotificationId));
        }

        public void DeleteSlot(int slot) => _storage.Delete(slot);

        public bool RequestLoad(int slot)
        {
            if (_playerAccessor.Player is not Node playerNode || !StageLoad(slot)) return false;

            playerNode.GetTree().CallDeferred(SceneTree.MethodName.ReloadCurrentScene);
            return true;
        }

        public bool StageLoad(int slot)
        {
            var file = _storage.Load(slot);
            if (file == null) return false;

            _pendingLoad = file;
            _population.Reset(); // the scene change frees NPC nodes without final-death events
            Engine.TimeScale = 1; // loading from the game-over screen: the death fast-forward must not leak
            return true;
        }

        /// <summary>A pending load must not leak into a freshly started game.</summary>
        public void ResetSession() => _pendingLoad = null;

        public void ApplyPendingLoad()
        {
            if (_pendingLoad == null) return;
            var file = _pendingLoad;
            // Cleared first: a throwing section must not re-apply forever, and the restore opens with a
            // session reset that reaches this service — the file has to be out of the field by then.
            _pendingLoad = null;
            _manager.Restore(file);
        }

        private string CurrentSceneName() =>
            _playerAccessor.Player is Node playerNode ? playerNode.GetTree().CurrentScene.Name : string.Empty;

        private static void OnSectionRestoreFailed(string sectionId, Exception e) =>
            Tracker.TrackException($"Save section '{sectionId}' failed to restore", e);
    }
}
