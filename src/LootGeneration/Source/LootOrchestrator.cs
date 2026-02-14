namespace LootGeneration.Source
{
    using Godot;
    using System;
    using System.Linq;
    using Core.Interfaces.Items;
    using Core.Interfaces.Events;
    using System.Collections.Generic;
    using Core.Interfaces.Events.GameEvents;

    public class LootOrchestrator : ILootOrchestrator
    {
        private readonly ILootGenerationService _lootGenerationService;
        private readonly List<ItemOnGround> _itemOnGroundsCache = [];
        private readonly IGameEventBus _gameEventBus;
        private readonly RandomNumberGenerator _rnd;
        private Vector2 _startPosition = new(850f, 450f);
        private float _animationDurationScale = 1f;
        private int _index;
        private Node2D? _floor;

        public LootOrchestrator(ILootGenerationService lootGenerationService, IGameEventBus gameEventBus, RandomNumberGenerator rnd)
        {
            _rnd = rnd;
            _gameEventBus = gameEventBus;
            _lootGenerationService = lootGenerationService;
            _gameEventBus.Subscribe<EntityDiedEvent>(OnEntityDied);
            _gameEventBus.Subscribe<BattleStartEvent>(OnBattleStart);
            _gameEventBus.Subscribe<BattleEndEvent>(OnBattleEnd);
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
        }

        private async void OnEntityDied(EntityDiedEvent obj)
        {
            try
            {
                if (obj.Entity is not CharacterBody2D body) return;
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

                    CreateItemOnGround(itemStack.Item, initialPosition, GetSpiralPoint(initialPosition, _rnd.RandfRange(5f, 50f)), _animationDurationScale, itemStack.Stack);
                    _animationDurationScale *= 0.95f;
                }
            }
            catch (Exception exception)
            {
                GD.Print($"{exception.Message}. {exception.StackTrace}");
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

        private Vector2 GetSpiralPoint(Vector2 origin, float spacing)
        {
            _index++;
            float gAngle = Mathf.Pi * (3 - Mathf.Sqrt(5));
            float radius = spacing * Mathf.Sqrt(_index);
            float angle = _index * gAngle;

            Vector2 offset = new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)) * radius;

            if (_index > 20) _index = 0;
            return origin + offset;
        }
    }
}
