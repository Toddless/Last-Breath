namespace LootGeneration.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Data;
    using Core.Enums;
    using Core.Events;
    using Core.Events.GameEvents;
    using Core.Inventory;
    using Core.Items;
    using Core.Save;
    using Core.Services;
    using Godot;

    /// <summary>
    /// Battle spoils: deaths DURING a player battle generate items, a won battle spills them on
    /// the floor, a click within reach picks them up. Deaths outside a battle (skirmishes,
    /// lifecycle) and during a save load produce nothing. IInventory/ILoadScope resolve lazily —
    /// the standalone LootGeneration sandbox doesn't register them.
    /// </summary>
    public class LootOrchestrator : ILootOrchestrator
    {
        private const float PickupRange = 350f;

        private readonly ILootGenerationService _lootGenerationService;
        private readonly IGameServiceProvider _provider;
        private readonly List<ItemOnGround> _itemOnGroundsCache = [];
        private readonly RandomNumberGenerator _rnd;
        private Vector2 _startPosition = new(850f, 450f);
        private float _animationDurationScale = 1f;
        private Node2D? _floor;
        private bool _battleActive;

        public LootOrchestrator(ILootGenerationService lootGenerationService, IGameEventBus gameEventBus, RandomNumberGenerator rnd, IGameServiceProvider provider)
        {
            _rnd = rnd;
            _lootGenerationService = lootGenerationService;
            _provider = provider;
            gameEventBus.Subscribe<EntityDiedEvent>(OnEntityDied);
            gameEventBus.Subscribe<BattleInitializedEvent>(OnBattleStart);
            gameEventBus.Subscribe<BattleEndEvent>(OnBattleEnd);
        }

        public void SetFloorToSpawnItems(Node2D? floor) => _floor = floor;

        private async void OnBattleEnd(BattleEndEvent obj)
        {
            _battleActive = false;
            try
            {
                if (obj.Results is not BattleResults.PlayerWon)
                {
                    DropCache();
                    return;
                }

                foreach (var item in _itemOnGroundsCache)
                {
                    _floor?.AddChild(item);
                    await item.AnimateAsync();
                }

                GD.Print($"Total items on floor: {_itemOnGroundsCache.Sum(x => x.Quantity)}");

                _itemOnGroundsCache.Clear();
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
                _startPosition = player.Position;
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
            var onGround = ItemOnGround.Initialize().Instantiate<ItemOnGround>();
            onGround.SetItem(item, amount);
            onGround.SetPositionToTravelTo(initialPosition, targetPosition);
            onGround.SetAnimationDurationScale(animationDurationScale);
            onGround.PickedUp += OnItemPickedUp;
            _itemOnGroundsCache.Add(onGround);
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

            if (!inventory.TryAddItem(itemOnGround.Item, itemOnGround.Quantity)) return;
            itemOnGround.QueueFree();
        }

        private bool PlayerInReach(Node2D item) =>
            _provider.GetServices<IPlayerAccessor>().FirstOrDefault()?.Player is Node2D player
            && player.GlobalPosition.DistanceTo(item.GlobalPosition) <= PickupRange;

        /// <summary>A lost/fled battle leaves nothing behind; the cached nodes never reached the tree.</summary>
        private void DropCache()
        {
            foreach (var item in _itemOnGroundsCache)
                item.QueueFree();
            _itemOnGroundsCache.Clear();
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
