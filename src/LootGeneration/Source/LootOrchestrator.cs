namespace LootGeneration.Source
{
    using Godot;
    using System;
    using Utilities;
    using System.Linq;
    using Core.Interfaces.Items;
    using Core.Interfaces.Events;
    using System.Collections.Generic;
    using Core.Interfaces.Events.GameEvents;

    public class LootOrchestrator : ILootOrchestrator
    {
        private readonly ILootGenerationService _lootGenerationService;
        private readonly List<ItemOnGround> _itemOnGroundsCache = [];
        private readonly RandomNumberGenerator _rnd;
        private Vector2 _startPosition = new(850f, 450f);
        private float _animationDurationScale = 1f;
        private Node2D? _floor;

        public LootOrchestrator(ILootGenerationService lootGenerationService, IGameEventBus gameEventBus, RandomNumberGenerator rnd)
        {
            _rnd = rnd;
            _lootGenerationService = lootGenerationService;
            gameEventBus.Subscribe<EntityDiedEvent>(OnEntityDied);
            gameEventBus.Subscribe<BattleStartEvent>(OnBattleStart);
            gameEventBus.Subscribe<BattleEndEvent>(OnBattleEnd);
        }

        public void SetFloorToSpawnItems(Node2D? floor) => _floor = floor;

        private async void OnBattleEnd(BattleEndEvent obj)
        {
            try
            {
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

        private void OnBattleStart(BattleStartEvent obj)
        {
            if (obj.Player is CharacterBody2D player)
                _startPosition = player.Position;
            _animationDurationScale = 1f;
        }

        private async void OnEntityDied(EntityDiedEvent obj)
        {
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
            _itemOnGroundsCache.Add(onGround);
        }

        private Vector2 GetSpiralPoint(Vector2 origin, float minRadius, float maxRadius)
        {
            float angle = GD.Randf() * Mathf.Tau;
            float radius = _rnd.RandfRange(minRadius, maxRadius);

            return new Vector2(origin.X + radius * Mathf.Cos(angle), origin.Y + radius * Mathf.Sin(angle));
        }
    }
}
