namespace LastBreath.World.Containers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Core.Ai.World.Time;
    using Core.Data;
    using Core.Data.SaveData;
    using Core.Inventory;
    using Core.Items;
    using Core.Save;
    using Core.Services;
    using Core.World.Containers;
    using Core.World.Interactions;
    using Core.World.Locations;
    using Godot;
    using Interactions;
    using Interactions.UI;
    using Locations;
    using Newtonsoft.Json.Linq;

    [GlobalClass]
    public partial class ChestComponent : Node2D, ILocationStateParticipant, IInteractionSource, IInteractionOwner
    {
        private const string ReportKind = "Chest";
        private const string MissingObjectIdFormat = "Chest '{0}' is disabled: it needs a stable ObjectId";
        private const string UnknownDefinitionFormat = "Chest '{0}' ('{1}') is disabled: the chest catalog holds no definition '{2}'";
        private const string MintRefusedFormat = "Chest '{0}' ('{1}') is disabled: definition '{2}' cannot mint item '{3}' into position '{4}': {5}";
        private const string MissingSpriteFormat = "Chest '{0}' ('{1}') shows no state: it needs its sprite set in the scene";
        private const string MissingTextureFormat = "Chest '{0}' ('{1}') leaves its sprite unchanged: no texture is set for the {2} state";
        private const string StateRepairedFormat = "Chest '{0}' ('{1}') repaired its saved state: {2}";
        private const string UnreadStateKeptFormat = "Chest '{0}' ('{1}') is disabled and keeps its saved state for the next save: {2}";
        private const string FailureFormat = "{0}: {1}";
        private const string IncompleteRecordProblem = "the record is empty, or its slot list is missing or holds an empty entry";
        private const string ItemFailedFormat = "the item in slot '{0}' failed to load with {1}";
        private static readonly ChestTransfer NothingTransferred = new(0, false);

        [Export] public string ObjectId { get; set; } = "";
        [Export] public string DefinitionId { get; set; } = "";

        /// <summary>Interaction target of this chest, set in the scene as its direct child, so the chest both owns it and offers the
        /// actions through it; any other setup is reported and leaves the chest disabled.</summary>
        [Export] public InteractionTarget Target { get; private set; } = null!;

        /// <summary>Sprite that shows the chest; the state textures share one canvas size with the chest standing on its bottom
        /// edge, so a state change never moves the chest.</summary>
        [Export] private Sprite2D? Visual { get;  set; }

        /// <summary>Look of a chest never opened.</summary>
        [Export] private Texture2D? ClosedTexture { get; set; }

        /// <summary>Look of an opened chest with something left in it.</summary>
        [Export] private Texture2D? OpenTexture { get; set; }

        /// <summary>Look of an opened chest with nothing left; optional, the open look stands in when it is not set.</summary>
        [Export] private Texture2D? EmptyTexture { get; set; }

        private ChestDefinition? _definition;
        private IGameServiceProvider _provider = null!;
        private bool _configurationReported;
        private bool _visualsReported;

        /// <summary>The clock this chest listens to while its removal deadline is set; null while it does not listen.</summary>
        private IWorldClock? _watchedClock;

        /// <summary>A saved state this build could not read; every capture writes it back unchanged.</summary>
        private JToken? _unreadState;

        public ChestContents Contents { get; } = new();
        public string NameKey => _definition?.NameKey ?? string.Empty;

        /// <summary>Cells the chest's definition gives it; zero while the chest has no definition.</summary>
        public int Capacity => _definition?.Capacity ?? 0;

        /// <summary>ID of the chest's interaction target, composed from <see cref="ObjectId"/>; empty while ObjectId is blank.</summary>
        public string InteractionId => InteractionIds.Chest(ObjectId);

        /// <summary>Always true: a disabled or emptied chest already offers no actions.</summary>
        public bool IsInteractable => true;

        private IWorldClock Clock => _provider.GetService<IWorldClock>();
        private InventoryItemSaveConverter Converter => new(_provider.GetService<EquipItemSaveConverter>(),
            _provider.GetService<IItemDataProvider>(), _provider.GetService<IAugmentItemMinter>());

        /// <summary>Checks the ObjectId and the interaction target setup, resolves its definition and resumes the removal watch of a
        /// set deadline; a setup error disables the chest.</summary>
        public override void _EnterTree()
        {
            _provider = Services.GameServiceProvider.Instance;
            _definition = CheckTarget() ? FindDefinition() : null;
            RefreshRemovalWatch();
        }

        public override void _Ready() => RefreshVisuals();

        /// <summary>Stops the removal watch: a chest outside the tree does not listen to the clock.</summary>
        public override void _ExitTree() => StopRemovalWatch();

        /// <summary>Open while the chest has a definition and something left in it.</summary>
        public IEnumerable<InteractionAction> Actions() => _definition == null || Contents.Empty
            ? [] : [new(InteractionActions.Open, "UI_Interaction_Open")];

        /// <summary>Opens the chest only while <see cref="Actions"/> offers Open enabled.</summary>
        public Task<InteractionResult> Execute(string actionId) => Task.FromResult(actionId == InteractionActions.Open && Offers(actionId)
            ? Open() : InteractionResult.Unavailable);

        /// <summary>Mints the contents of the reachable chest, then shows them in its window as the open session; Unavailable when the
        /// player cannot reach it, the contents refuse to mint or the window does not open.</summary>
        private InteractionResult Open()
        {
            var interactions = _provider.GetService<InteractionService>();
            if (!interactions.CanReach(Target) || !TryOpen()) return InteractionResult.Unavailable;
            return interactions.OpenWindow<ChestContentsWindow>(Target);
        }

        /// <summary>Mints the authored contents on the first open; false while the chest is disabled or empty, and when its contents refuse to mint.</summary>
        public bool TryOpen()
        {
            if (_definition is not { } definition || Contents.Empty || !TryInitialize(definition)) return false;
            RefreshFromContents();
            return true;
        }

        /// <summary>Moves one slot, or every slot, into the bag as far as it fits; a chest without a definition moves nothing.</summary>
        public ChestTransfer Transfer(string? slotId, Func<bool> accessible)
        {
            if (_definition == null) return NothingTransferred;
            var result = Contents.Transfer(_provider.GetService<IInventoryTransfer>(), slotId, Clock.TotalMinutes,
                _definition.EmptyRemovalDelayMinutes, accessible);
            RefreshFromContents();
            return result;
        }

        /// <summary>Removes the chest at once when its deadline passed while the location was away or loading.</summary>
        public void ReconcileElapsed(double gameMinutes) => RemoveIfDue(Clock.TotalMinutes);

        /// <summary>Brings the look and the removal watch in line with the contents after they change.</summary>
        private void RefreshFromContents()
        {
            RefreshVisuals();
            RefreshRemovalWatch();
        }

        /// <summary>Listens to the game minutes only while a removal deadline is set and the chest is in the tree and not queued
        /// for deletion; repeated calls change nothing.</summary>
        private void RefreshRemovalWatch()
        {
            if (Contents.RemoveAtMinutes == null || !IsInsideTree() || IsQueuedForDeletion()) StopRemovalWatch();
            else StartRemovalWatch();
        }

        /// <summary>Subscribes to the current clock unless a watch is already running.</summary>
        private void StartRemovalWatch()
        {
            if (_watchedClock != null) return;
            _watchedClock = Clock;
            _watchedClock.MinutePassed += OnMinutePassed;
        }

        /// <summary>Unsubscribes from the clock the watch subscribed to, if any.</summary>
        private void StopRemovalWatch()
        {
            if (_watchedClock == null) return;
            _watchedClock.MinutePassed -= OnMinutePassed;
            _watchedClock = null;
        }

        /// <summary>Checks the deadline every game minute; a location still preparing waits for the next one.</summary>
        private void OnMinutePassed(double totalMinutes)
        {
            if (LocationRoot.Find(this) is { IsPreparing: true }) return;
            RemoveIfDue(totalMinutes);
        }

        /// <summary>Queues the chest for removal once its deadline is due, which also ends the watch.</summary>
        private void RemoveIfDue(double now)
        {
            if (!Contents.RemovalDue(now) || IsQueuedForDeletion()) return;
            QueueFree();
            RefreshRemovalWatch();
        }

        /// <summary>Shows the texture of the current look; without a sprite or that texture the sprite stays as it is and the
        /// problem is reported once.</summary>
        private void RefreshVisuals()
        {
            var look = CurrentLook();
            if (Visual == null) ReportVisuals(string.Format(MissingSpriteFormat, GetPath(), ObjectId));
            else if (TextureFor(look) is { } texture) Visual.Texture = texture;
            else ReportVisuals(string.Format(MissingTextureFormat, GetPath(), ObjectId, look));
        }

        /// <summary>The look of the current contents.</summary>
        private ChestLook CurrentLook() => !Contents.Initialized ? ChestLook.Closed : Contents.Empty ? ChestLook.Empty : ChestLook.Open;

        /// <summary>The texture set for a look; an empty chest without its own texture shows the open one.</summary>
        private Texture2D? TextureFor(ChestLook look) => look switch
        {
            ChestLook.Closed => ClosedTexture,
            ChestLook.Open => OpenTexture,
            ChestLook.Empty => EmptyTexture ?? OpenTexture,
            _ => null
        };

        /// <summary>True when <see cref="Actions"/> offers the action with this ID enabled.</summary>
        private bool Offers(string actionId) => Actions().Any(action => action.Enabled && action.Id == actionId);

        /// <summary>Mints the authored contents once; a refused mint leaves the chest uninitialized and disables it.</summary>
        private bool TryInitialize(ChestDefinition definition)
        {
            if (Contents.Initialized) return true;
            if (!new AuthoredChestMinter(_provider.GetService<IItemCreationService>()).TryMint(definition, out var slots, out var refusal))
            {
                DisableAfterRefusal(refusal);
                return false;
            }

            Contents.Initialize(slots);
            return true;
        }

        /// <summary>Reports the refused mint and drops the definition: the chest stays shut, without actions, until its location loads again.</summary>
        private void DisableAfterRefusal(ChestMintRefusal refusal)
        {
            _definition = null;
            ReportConfiguration(string.Format(MintRefusedFormat, GetPath(), ObjectId, DefinitionId,
                refusal.ItemId, refusal.SlotId, refusal.Problem));
        }

        /// <summary>True when the chest has a stable ObjectId, owns its interaction target and offers its actions through it; any
        /// other setup is reported and disables the chest.</summary>
        private bool CheckTarget()
        {
            if (FindTargetProblem() is not { } problem) return true;
            ReportConfiguration(problem);
            return false;
        }

        /// <summary>Why the target cannot serve this chest, described for the report: no stable ObjectId, no target, a target that
        /// another node owns or no node owns, or a target the chest does not offer its actions through; null when it serves.</summary>
        private string? FindTargetProblem() => string.IsNullOrWhiteSpace(ObjectId)
            ? string.Format(MissingObjectIdFormat, GetPath())
            : InteractionTargetSetup.FindProblem(this, Target, ReportKind, ObjectId);

        /// <summary>The catalog definition this chest is placed with; null, and reported, when the catalog holds none.</summary>
        private ChestDefinition? FindDefinition()
        {
            if (_provider.GetService<ChestCatalog>().Find(DefinitionId) is { } definition) return definition;
            ReportConfiguration(string.Format(UnknownDefinitionFormat, GetPath(), ObjectId, DefinitionId));
            return null;
        }

        /// <summary>Writes an error that disables the chest to the log and the Godot console, once per node.</summary>
        private void ReportConfiguration(string message) => ReportOnce(ref _configurationReported, message);

        /// <summary>Writes a look the chest cannot show to the log and the Godot console, once per node.</summary>
        private void ReportVisuals(string message) => ReportOnce(ref _visualsReported, message);

        /// <summary>Writes a chest problem to the log and the Godot console unless its flag says one of its kind was written.</summary>
        private void ReportOnce(ref bool reported, string message)
        {
            if (reported) return;
            reported = true;
            Report(message);
        }

        /// <summary>Writes a chest problem to the log and the Godot console.</summary>
        private void Report(string message)
        {
            Tracker.TrackError(message, this);
            GD.PrintErr(message);
        }

        /// <summary>The contents as the save writes them; a saved state this chest could not read goes back exactly as it came.</summary>
        public JToken CaptureLocationState()
        {
            if (_unreadState != null) return _unreadState.DeepClone();
            var converter = Converter;
            return JObject.FromObject(new SavedChest
            {
                DefinitionId = DefinitionId,
                Initialized = Contents.Initialized,
                RemoveAtMinutes = Contents.RemoveAtMinutes,
                Slots = Contents.Slots.Select(x => new SavedSlot
                {
                    Id = x.Id, Amount = x.Amount,
                    Item = x is { Amount: > 0, Item: not null } ? converter.ToData(x.Item, x.Amount) : null
                }).ToList()
            });
        }

        /// <summary>Restores the saved contents, repaired and reported where content changed since the save; a state that
        /// cannot be read shuts the chest and is kept for the next save.</summary>
        public void RestoreLocationState(JToken state)
        {
            var repair = ReadSaved(state);
            ReportFindings(repair);
            if (repair.State is not { } restored)
            {
                DisableUnread(state);
                return;
            }

            _unreadState = null;
            Contents.Restore(restored.Initialized, restored.Slots, restored.RemoveAtMinutes);
            RefreshFromContents();
        }

        /// <summary>The saved state made into one this chest can hold, or refused when it cannot be read.</summary>
        private ChestStateRepairResult ReadSaved(JToken state)
        {
            if (!TryParse(state, out var saved, out string? problem)) return ChestStateRepair.Unreadable(problem);
            if (!ChestStateRepair.Reads(saved.Version)) return ChestStateRepair.VersionNotRead(saved.Version);
            if (!TryReadSlots(saved.Slots, out var slots, out problem)) return ChestStateRepair.Unreadable(problem);
            return ChestStateRepair.Repair(new SavedChestState(saved.DefinitionId, saved.Initialized, slots, saved.RemoveAtMinutes),
                DefinitionId, Clock.TotalMinutes, _definition?.EmptyRemovalDelayMinutes);
        }

        /// <summary>Reads the state into its record; false with the reason when it does not read as one.</summary>
        private static bool TryParse(JToken state, [NotNullWhen(true)] out SavedChest? saved, [NotNullWhen(false)] out string? problem)
        {
            try
            {
                saved = state.ToObject<SavedChest>();
            }
            catch (Exception exception)
            {
                saved = null;
                problem = Describe(exception);
                return false;
            }

            if (saved is not { Slots: { } slots } || slots.Any(slot => slot == null))
            {
                problem = IncompleteRecordProblem;
                return false;
            }

            problem = null;
            return true;
        }

        /// <summary>The saved slots with their items read back, null where the game no longer holds the item; false with
        /// the reason when an item's saved data fails to load.</summary>
        private bool TryReadSlots(List<SavedSlot> saved, out List<ChestSlot> slots, [NotNullWhen(false)] out string? problem)
        {
            var converter = Converter;
            slots = [];
            foreach (var slot in saved)
            {
                try
                {
                    slots.Add(new ChestSlot(slot.Id, slot.Item == null ? null : converter.FromData(slot.Item), slot.Amount));
                }
                catch (Exception exception)
                {
                    problem = string.Format(ItemFailedFormat, slot.Id, Describe(exception));
                    return false;
                }
            }

            problem = null;
            return true;
        }

        /// <summary>Names a failure by its exception type and message.</summary>
        private static string Describe(Exception exception) => string.Format(FailureFormat, exception.GetType().Name, exception.Message);

        /// <summary>Writes every finding of a restore to the log and the Godot console, one entry each.</summary>
        private void ReportFindings(ChestStateRepairResult repair)
        {
            string format = repair.State == null ? UnreadStateKeptFormat : StateRepairedFormat;
            foreach (var finding in repair.Findings) Report(string.Format(format, GetPath(), ObjectId, finding.Description));
        }

        /// <summary>Keeps the unread state for every later capture and shuts the chest: no contents, no actions, and nothing
        /// minted in place of what the state holds.</summary>
        private void DisableUnread(JToken state)
        {
            _unreadState = state.DeepClone();
            _definition = null;
            Contents.Restore(initialized: false, slots: [], removeAt: null);
            RefreshFromContents();
        }

        /// <summary>What the chest looks like: shut until first opened, lid up while anything is left, then empty.</summary>
        private enum ChestLook
        {
            Closed,
            Open,
            Empty
        }

        private sealed class SavedChest
        {
            public int Version { get; set; } = ChestStateRepair.StateVersion;
            public string DefinitionId { get; set; } = "";
            public bool Initialized { get; set; }
            public double? RemoveAtMinutes { get; set; }
            public List<SavedSlot> Slots { get; set; } = [];
        }
        private sealed class SavedSlot
        {
            public string Id { get; set; } = "";
            public int Amount { get; set; }
            public InventoryItemSaveData? Item { get; set; }
        }
    }
}
