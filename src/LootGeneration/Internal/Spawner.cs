namespace LootGeneration.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Data;
    using Core.Enums;
    using Core.Components.NpcModifiers;
    using Core.Entity;
    using Core.Events;
    using Godot;

    public class Spawner
    {
        private const int AmountNpc = 5;
        private readonly RandomNumberGenerator _rnd = new();

        private readonly Dictionary<Rarity, float> _rarityChances = new()
        {
            [Rarity.Uncommon] = 0.3f,
            [Rarity.Rare] = 0.2f,
            [Rarity.Epic] = 0.15f,
            [Rarity.Legendary] = 0.15f,
            [Rarity.Unique] = 0.1f,
            [Rarity.Mythic] = 0.1f,
        };

        private IGameEventBus? _gameEventBus;
        private Node2D? _mainWorld;
        private INpcCreationStrategy? CurrentNpcCreationStrategy { get; set; }
        private INpcModifierProvider? _npcModifierProvider;

        private int CurrentNpcAmount
        {
            get;
            set
            {
                if (value == field) return;
                field = value;
                CheckNeedNewSpawn();
            }
        }


        public void SetRandomCreation() =>
            CurrentNpcCreationStrategy =
                new CreateRandomNpcStrategy(GetRandomRarity, GetRandomFraction, GetRandomLevel, GetRandomEntityType, GetAmountNpcModifiers, GetRandomNpcModifiers);

        public void SetAsDefault(Rarity rarity, Fractions fractions, int level, EntityType type, List<string> modifierIds)
        {
            if (_npcModifierProvider == null) return;
            var modifiers = modifierIds.Select(modifierId => _npcModifierProvider.GetModifier(modifierId)).ToList();
            CurrentNpcCreationStrategy = new CreateNpcStrategy(rarity, fractions, level, type, modifiers);
        }

        public void CreateSingle(Rarity rarity, Fractions fractions, int level, EntityType type, List<string> modifiers)
        {
            if (_npcModifierProvider == null) return;
            var npc = ExampleNpc.Initialize().Instantiate<ExampleNpc>();
            npc.Rarity = rarity;
            npc.Fraction = fractions;
            npc.Level = level;
            npc.EntityType = type;
            npc.GameEventBus = _gameEventBus;
            npc.Position = GetRandomPosition();
            npc.NpcModifiers = new NpcModifiersComponent(npc);
            npc.TreeExiting += () => npc.Dead -= OnNpcDead;
            npc.Dead += OnNpcDead;
            npc.NpcModifiers.AddModifiers(modifiers.Select(modifierId => _npcModifierProvider.GetModifier(modifierId).Copy()).ToList());
            _mainWorld?.CallDeferred(Node.MethodName.AddChild, npc);
        }

        public void InitialSpawn()
        {
            _rnd.Randomize();
            CurrentNpcCreationStrategy =
                new CreateRandomNpcStrategy(GetRandomRarity, GetRandomFraction, GetRandomLevel, GetRandomEntityType, GetAmountNpcModifiers, GetRandomNpcModifiers);
            SpawnNewNpc();
        }


        public void SetNpcModifierProvider(INpcModifierProvider npcModifierProvider)
        {
            _npcModifierProvider = npcModifierProvider;
        }

        public void SetGameEventBus(IGameEventBus gameEventBus) => _gameEventBus = gameEventBus;
        public void SetWorld(Node2D world) => _mainWorld = world;

        public void SpawnNewNpc()
        {
            var entity = CurrentNpcCreationStrategy?.CreateNpc();
            if (entity is not ExampleNpc npc) return;
            npc.Position = GetRandomPosition();
            npc.TreeExiting += () => npc.Dead -= OnNpcDead;
            npc.Dead += OnNpcDead;
            npc.GameEventBus = _gameEventBus;
            npc.NpcModifiers.AddModifiers(GetRandomNpcModifiers(GetAmountNpcModifiers(npc.EntityType, npc.Rarity)));
            _mainWorld?.CallDeferred(Node.MethodName.AddChild, npc);
            CurrentNpcAmount++;
        }


        private List<INpcModifier> GetRandomNpcModifiers(int amount)
        {
            var modifiers = new List<INpcModifier>();
            var weighted = WeightedRandomPicker.CalculateWeights(_npcModifierProvider?.GetAllModifiers() ?? []);
            for (int i = 0; i < amount; i++)
                modifiers.Add(WeightedRandomPicker.PickRandom(weighted.WeightedObjects, weighted.TotalWeight, _rnd).Copy());
            return modifiers;
        }

        private void CheckNeedNewSpawn()
        {
            if (CurrentNpcAmount < AmountNpc) SpawnNewNpc();
        }

        private void OnNpcDead(IFightable obj)
        {
            if (obj is not ExampleNpc npc) return;
            npc.Dead -= OnNpcDead;
            npc.Hide();
            npc.QueueFree();
            CurrentNpcAmount--;
        }


        private Vector2 GetRandomPosition()
        {
            float x = GD.RandRange(0, 1500);
            float y = GD.RandRange(0, 800);
            return new Vector2(x, y);
        }

        private Rarity GetRandomRarity()
        {
            float roll = _rnd.Randf();
            float cumulative = 0f;

            foreach (KeyValuePair<Rarity, float> rarityChance in _rarityChances)
            {
                cumulative += rarityChance.Value;
                if (roll < cumulative)
                    return rarityChance.Key;
            }

            return Rarity.Uncommon;
        }

        private Fractions GetRandomFraction() => (Fractions)_rnd.RandiRange(0, 6);

        private int GetRandomLevel(Rarity rarity) => rarity switch
        {
            Rarity.Uncommon => _rnd.RandiRange(1, 15),
            Rarity.Rare => _rnd.RandiRange(1, 25),
            Rarity.Epic => _rnd.RandiRange(1, 35),
            Rarity.Legendary => _rnd.RandiRange(1, 55),
            Rarity.Unique => _rnd.RandiRange(1, 85),
            Rarity.Mythic => _rnd.RandiRange(1, 150),
            _ => _rnd.RandiRange(0, 10)
        };

        private EntityType GetRandomEntityType() => (EntityType)_rnd.RandiRange(0, 5);

        private int GetAmountNpcModifiers(EntityType type, Rarity rarity)
        {
            int min = MinAmountNpcModifiers(type);
            return _rnd.RandiRange(min, AmountNpcModifiersForRarity(rarity));
        }

        private int MinAmountNpcModifiers(EntityType type) => type switch
        {
            EntityType.Regular or EntityType.Special or EntityType.Elit => 0,
            EntityType.Unique => 1,
            EntityType.Boss => 2,
            EntityType.Archon => 3,
            _ => 0
        };

        private int AmountNpcModifiersForRarity(Rarity rarity) => rarity switch
        {
            Rarity.Uncommon => 1,
            Rarity.Rare => 2,
            Rarity.Epic => 3,
            Rarity.Legendary => 5,
            Rarity.Unique => 7,
            Rarity.Mythic => 11,
            _ => 0,
        };

        private class CreateNpcStrategy(
            Rarity rarity,
            Fractions fractions,
            int level,
            EntityType type,
            List<INpcModifier> modifiers) : INpcCreationStrategy
        {
            public IFightable CreateNpc()
            {
                var npc = ExampleNpc.Initialize().Instantiate<ExampleNpc>();
                npc.Rarity = rarity;
                npc.Fraction = fractions;
                npc.Level = level;
                npc.EntityType = type;
                npc.NpcModifiers = new NpcModifiersComponent(npc);
                npc.NpcModifiers.AddModifiers(modifiers);
                return npc;
            }
        }

        private class CreateRandomNpcStrategy(
            Func<Rarity> getRandomRarity,
            Func<Fractions> getRandomFraction,
            Func<Rarity, int> getRandomLevel,
            Func<EntityType> getRandomEntityType,
            Func<EntityType, Rarity, int> getModifiersAmount,
            Func<int, List<INpcModifier>> getRandomAmountModifiers) : INpcCreationStrategy
        {
            public IFightable CreateNpc()
            {
                var npc = ExampleNpc.Initialize().Instantiate<ExampleNpc>();
                npc.Rarity = getRandomRarity();
                npc.Fraction = getRandomFraction();
                npc.Level = getRandomLevel(npc.Rarity);
                npc.EntityType = getRandomEntityType();
                npc.NpcModifiers = new NpcModifiersComponent(npc);
                npc.NpcModifiers.AddModifiers(getRandomAmountModifiers(getModifiersAmount(npc.EntityType, npc.Rarity)));
                return npc;
            }
        }

        private interface INpcCreationStrategy
        {
            IFightable CreateNpc();
        }
    }
}
