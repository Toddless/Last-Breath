namespace LootGeneration.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Data;
    using Core.Enums;
    using Core.Events;
    using Core.Inventory;
    using Core.Items;
    using Core.Save;
    using Core.Services;
    using Godot;

    /// <summary>
    /// Battle spoils: deaths DURING a player battle generate items, a won battle spills them on
    /// the floor, a click within reach picks them up (resources/recipes come as a whole category,
    /// equipment piece by piece). Deaths outside a battle (skirmishes, lifecycle) and during a
    /// save load produce nothing. IInventory/ILoadScope resolve lazily — the standalone
    /// LootGeneration sandbox doesn't register them.
    /// </summary>
    public class LootOrchestrator : ILootOrchestrator, IGroundItemStore
    {
        private const float PickupRange = 350f;

        private readonly ILootGenerationService _lootGenerationService;
        private readonly IGameEventBus _gameEventBus;
        private readonly IGameServiceProvider _provider;
        private readonly List<ItemOnGround> _itemOnGroundsCache = [];
        private readonly List<ItemOnGround> _itemsOnGround = [];
        private readonly RandomNumberGenerator _rnd;
        private Vector2 _startPosition = new(850f, 450f);
        private float _animationDurationScale = 1f;
        private Node2D? _floor;
        private Node2D? _battleFloor;
        private bool _battleActive;

        public LootOrchestrator(ILootGenerationService lootGenerationService, IGameEventBus gameEventBus, RandomNumberGenerator rnd, IGameServiceProvider provider)
        {
            _rnd = rnd;
            _lootGenerationService = lootGenerationService;
            _gameEventBus = gameEventBus;
            _provider = provider;
            gameEventBus.Subscribe<EntityDiedEvent>(OnEntityDied);
            gameEventBus.Subscribe<BattleInitializedEvent>(OnBattleStart);
            gameEventBus.Subscribe<BattleEndEvent>(OnBattleEnd);
        }

        public IReadOnlyList<ItemOnGround> ItemsOnGround => _itemsOnGround;

        public void SetFloorToSpawnItems(Node2D? floor) => _floor = floor;

        /// <summary>What lies on the floor right now. A drop the player already walked over left the list
        /// at pickup and is not written down as still lying there.</summary>
        public IReadOnlyList<GroundItemPlacement> CaptureGroundItems() =>
        [
            .. _itemsOnGround
                .Where(item => GodotObject.IsInstanceValid(item) && item.Item != null)
                .Select(item => new GroundItemPlacement(item.Item!, item.Quantity, item.Position.X, item.Position.Y))
        ];

        public void RestoreGroundItems(IReadOnlyList<GroundItemPlacement> items)
        {
            ClearGround();
            foreach (var placement in items)
                SpawnRestored(placement);
        }

        public bool TryPickup(ItemOnGround item, IInventory inventory)
        {
            if (item.Item == null) return false;

            // Currency never enters the bag: the pile credits the wallet (owner decision 2026-07-24).
            // A sandbox without an economy simply leaves the pile lying.
            if (item.Item is Core.Items.ICurrencyItem)
            {
                if (_provider.GetService<Core.Trade.IWalletService>() is not { } wallet) return false;
                wallet.Add(item.Quantity);
            }
            else if (!inventory.TryAddItem(item.Item, item.Quantity))
            {
                return false;
            }

            _itemsOnGround.Remove(item);
            _gameEventBus.Publish(new ItemPickedUpEvent(item.Item, item.Quantity));
            _ = ConfirmPickupSafeAsync(item);
            return true;
        }

        private static async System.Threading.Tasks.Task ConfirmPickupSafeAsync(ItemOnGround item)
        {
            try
            {
                await item.ConfirmPickupAsync();
            }
            catch (Exception exception)
            {
                GD.Print($"{exception.Message}. {exception.StackTrace}");
            }
        }

        private async void OnBattleEnd(BattleEndEvent obj)
        {
            _battleActive = false;
            // Snapshot-and-clear BEFORE the async drop loop: a battle that starts during the
            // staggered animations writes its drops into the cache, and iterating the live list
            // threw mid-loop, skipping Clear() — the poisoned cache (nodes already in the tree,
            // later freed) then failed every subsequent drop and loot stopped falling entirely.
            var pending = new List<ItemOnGround>(_itemOnGroundsCache);
            var floor = _battleFloor ?? _floor;
            _itemOnGroundsCache.Clear();
            try
            {
                if (obj.Results is not BattleResults.PlayerWon)
                {
                    FreeItems(pending);
                    return;
                }

                foreach (var item in pending)
                {
                    if (!GodotObject.IsInstanceValid(item)) continue;
                    AddToFloor(item, floor);
                    _gameEventBus.Publish(new ItemDroppedEvent(item));
                    await item.AnimateAsync();
                }
            }
            catch (Exception exception)
            {
                GD.Print($"{exception.Message}. {exception.StackTrace}");
            }
        }

        private void OnBattleStart(BattleInitializedEvent obj)
        {
            _battleActive = true;
            if (obj.Player is CharacterBody2D player)
            {
                _battleFloor = player.GetParent() as Node2D;
                _startPosition = player.Position;
            }
            _animationDurationScale = 1f;
        }

        private async void OnEntityDied(EntityDiedEvent obj)
        {
            // Only a live player battle drops spoils: skirmish deaths across the world and
            // restored bodies during a save load are not the player's kills to loot.
            if (!_battleActive || IsLoading()) return;

            try
            {
                var items = await _lootGenerationService.GenerateItemsAsync(obj.Entity);

                var initialPosition = _startPosition;

                foreach (var itemStack in items)
                {
                    var existingStack = _itemOnGroundsCache.FirstOrDefault(itemOnGround => itemOnGround.Item is not IEquipItem && itemOnGround.Item?.Id == itemStack.Item.Id);
                    if (existingStack != null)
                    {
                        existingStack.Quantity += itemStack.Stack;
                        continue;
                    }

                    CreateItemOnGround(itemStack.Item, initialPosition, GetSpiralPoint(initialPosition, 15f, 80f), _animationDurationScale, itemStack.Stack);
                    _animationDurationScale *= 0.95f;
                }
            }
            catch (Exception exception)
            {
                GD.Print($"{exception.Message}. {exception.StackTrace}");
                Tracker.TrackException($"Failed to generate items", exception, this);
            }
        }

        private void CreateItemOnGround(IItem item, Vector2 initialPosition, Vector2 targetPosition, float animationDurationScale, int amount = 1)
        {
            var onGround = BuildItemOnGround(item, amount);
            onGround.SetPositionToTravelTo(initialPosition, targetPosition);
            onGround.SetAnimationDurationScale(animationDurationScale);
            _itemOnGroundsCache.Add(onGround);
        }

        private ItemOnGround BuildItemOnGround(IItem item, int amount)
        {
            var onGround = ItemOnGround.Initialize().Instantiate<ItemOnGround>();
            onGround.SetItem(item, amount);
            onGround.PickedUp += OnItemPickedUp;
            return onGround;
        }

        private void AddToFloor(ItemOnGround item, Node2D? floor = null)
        {
            (floor ?? _floor)?.AddChild(item);
            _itemsOnGround.Add(item);
            // A drop can leave the tree without going through TryPickup (scene change,
            // debug frees) — TreeExited keeps the pickable list honest either way.
            item.TreeExited += () => _itemsOnGround.Remove(item);
        }

        /// <summary>A restored drop lands where it was left instead of being tossed there: the toss is the
        /// presentation of a kill, and nothing was just killed. The spot is written BEFORE the node enters
        /// the tree — a body placed after it joins teleports whatever it overlaps.</summary>
        public void RestoreLocationItems(Node2D floor, IReadOnlyList<GroundItemPlacement> items)
        {
            FreeItems(_itemsOnGround.Where(x => GodotObject.IsInstanceValid(x) && Core.World.Spaces.SpatialAccess.SharesSpace(floor, x)).ToList());
            foreach (var placement in items) SpawnRestored(placement, floor);
        }

        private void SpawnRestored(GroundItemPlacement placement, Node2D? floor = null)
        {
            var onGround = BuildItemOnGround(placement.Item, placement.Quantity);
            onGround.Position = new Vector2(placement.X, placement.Y);
            AddToFloor(onGround, floor);
        }

        /// <summary>Empties the floor, spilled drops and the ones a battle had ready alike: the orchestrator
        /// outlives the scene and has no session reset of its own, so nothing else would take them away.</summary>
        private void ClearGround()
        {
            FreeItems([.. _itemsOnGround]);
            FreeItems([.. _itemOnGroundsCache]);
            _itemsOnGround.Clear();
            _itemOnGroundsCache.Clear();
        }

        private void OnItemPickedUp(ItemOnGround itemOnGround)
        {
            if (itemOnGround.Item == null || !PlayerInReach(itemOnGround)) return;

            var inventory = _provider.GetServices<IInventory>().FirstOrDefault();
            if (inventory == null)
            {
                Tracker.TrackNotFound("Item pickup ignored: no IInventory registered in this project");
                GD.Print("Item pickup ignored: no IInventory registered in this project");
                return;
            }

            foreach (var target in GetPickupBatch(itemOnGround))
            {
                if (TryPickup(target, inventory)) continue;
                target.NotifyInventoryFull();
                break;
            }
        }

        /// <summary>Resources and recipes come off the floor as a whole category; equipment only the clicked piece.</summary>
        private List<ItemOnGround> GetPickupBatch(ItemOnGround clicked)
        {
            if (clicked.Category is not (LootCategory.Resource or LootCategory.Recipe)) return [clicked];

            var batch = new List<ItemOnGround> { clicked };
            batch.AddRange(_itemsOnGround.Where(item => item != clicked && item.Category == clicked.Category && Core.World.Spaces.SpatialAccess.SharesSpace(clicked, item)));
            return batch;
        }

        private bool PlayerInReach(Node2D item) =>
            _provider.GetServices<IPlayerAccessor>().FirstOrDefault()?.Player is Node2D player
            && Core.World.Spaces.SpatialAccess.SharesSpace(player, item)
            && player.GlobalPosition.DistanceTo(item.GlobalPosition) <= PickupRange;

        /// <summary>Takes drops out of the game for good: a lost/fled battle leaves nothing behind, a load
        /// sweeps the floor of the playthrough being left. Leaving the tree comes BEFORE the free — a
        /// QueueFree lands at the end of the frame, and until then the node still catches a click and hands
        /// its item over a second time. Cached nodes never reached the tree and simply have no parent.</summary>
        private static void FreeItems(List<ItemOnGround> items)
        {
            foreach (var item in items)
            {
                if (!GodotObject.IsInstanceValid(item)) continue;
                item.GetParent()?.RemoveChild(item);
                item.QueueFree();
            }
        }

        private bool IsLoading() => _provider.GetServices<ILoadScope>().FirstOrDefault()?.IsLoading == true;

        private Vector2 GetSpiralPoint(Vector2 origin, float minRadius, float maxRadius)
        {
            float angle = GD.Randf() * Mathf.Tau;
            float radius = _rnd.RandfRange(minRadius, maxRadius);

            return new Vector2(origin.X + radius * Mathf.Cos(angle), origin.Y + radius * Mathf.Sin(angle));
        }
    }
}
