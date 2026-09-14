namespace LastBreath.World.Containers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
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
    using Godot.Collections;
    using Interactions;
    using Locations;
    using Newtonsoft.Json.Linq;

    [GlobalClass]
    public partial class ChestComponent : Node2D, ILocationStateParticipant, IInteractionSource
    {
        [Export] public string ObjectId { get; set; } = "";
        [Export] public string DefinitionId { get; set; } = "";
        [Export] private Sprite2D? Visual { get;  set; }
        [Export] private Array<Texture2D>? Visuals { get; set; } = [];

        private ChestDefinition _definition = null!;
        private IGameServiceProvider _provider = null!;
        public ChestContents Contents { get; } = new();
        public InteractionTarget Target => GetNode<InteractionTarget>("InteractionTarget");
        public string NameKey => _definition.NameKey;
        private IWorldClock Clock => _provider.GetService<IWorldClock>();
        private InventoryItemSaveConverter Converter => new(_provider.GetService<EquipItemSaveConverter>(),
            _provider.GetService<IItemDataProvider>(), _provider.GetService<IAugmentItemMinter>());

        public override void _EnterTree()
        {
            _provider = Services.GameServiceProvider.Instance;
            _definition = _provider.GetService<ChestCatalog>().Get(DefinitionId);
            if (string.IsNullOrWhiteSpace(ObjectId)) throw new InvalidOperationException("Chest requires a stable ObjectId.");
            Target.ObjectId = "chest/" + ObjectId;
        }

        public override void _Ready() => RefreshVisuals();
        public IEnumerable<InteractionAction> Actions() => Contents.Empty
            ? [] : [new(InteractionActions.Open, "UI_Interaction_Open")];

        public Task<InteractionResult> Execute(string actionId) => Task.FromResult(actionId == InteractionActions.Open
            ? _provider.GetService<InteractionService>().OpenChest(this) : InteractionResult.Unavailable);

        public void Open()
        {
            var creation = _provider.GetService<IItemCreationService>();
            Contents.Initialize(_definition, entry => creation.CreateItem(entry.ItemId, [], entry.Rarity, 0, 1, entry.Rarity));
            RefreshVisuals();
        }

        public ChestTransfer Transfer(string? slotId, Func<bool> accessible)
        {
            var result = Contents.Transfer(_provider.GetService<IInventoryTransfer>(), slotId, Clock.TotalMinutes,
                _definition.EmptyRemovalDelayMinutes, accessible);
            RefreshVisuals();
            return result;
        }

        public override void _Process(double delta)
        {
            if (LocationRoot.Find(this) is { IsPreparing: true }) return;
            ReconcileElapsed(0);
        }
        public void ReconcileElapsed(double gameMinutes)
        {
            if (Contents.RemovalDue(Clock.TotalMinutes) && !IsQueuedForDeletion()) QueueFree();
        }

        private void RefreshVisuals()
        {
            Visual?.Texture = Contents.Empty ? Visuals?[0] : Visuals?[1];
        }

        public JToken CaptureLocationState()
        {
            var converter = Converter;
            return JObject.FromObject(new SavedChest
            {
                DefinitionId = DefinitionId,
                Initialized = Contents.Initialized,
                RemoveAtMinutes = Contents.RemoveAtMinutes,
                Slots = Contents.Slots.Select(x => new SavedSlot
                {
                    Id = x.Id, Amount = x.Amount,
                    Item = x.Amount > 0 && x.Item != null ? converter.ToData(x.Item, x.Amount) : null
                }).ToList()
            });
        }

        public void RestoreLocationState(JToken state)
        {
            var saved = state.ToObject<SavedChest>() ?? throw new InvalidOperationException("Missing chest state.");
            if (saved.Version != 1 || saved.DefinitionId != DefinitionId) throw new InvalidOperationException("Incompatible chest state.");
            var converter = Converter;
            var slots = saved.Slots.Select(x => new ChestSlot(x.Id, x.Item == null ? null : converter.FromData(x.Item), x.Amount)).ToList();
            Contents.Restore(saved.Initialized, slots, saved.RemoveAtMinutes);
            RefreshVisuals();
        }

        private sealed class SavedChest
        {
            public int Version { get; set; } = 1;
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
