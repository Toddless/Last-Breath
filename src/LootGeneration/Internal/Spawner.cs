namespace LootGeneration.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Data;
    using Core.Enums;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Entity.NpcModifiers;
    using Core.Events;
    using Godot;

    /// <summary>
    /// Fills the sandbox world with example NPCs and rolls what each of them is — rarity, faction, level,
    /// type, how many modifiers it carries and which, and where it lands.
    /// </summary>
    public class Spawner
    {
        private const int AmountNpc = 5;

        /// <summary>The rectangle a spawn lands in, in world units.</summary>
        private const int WorldWidth = 1500;
        private const int WorldHeight = 800;

        /// <summary>
        /// The stream every spawn decision rolls on. It starts on <see cref="DefaultRandomNumberGenerator"/>,
        /// which touches nothing native: the engine generator is a native object, and constructing one where
        /// Godot is not running kills the whole process (0xC0000005) past the reach of any catch — a spawner
        /// that built its own took every host that merely CREATED it down with it. Which implementation the
        /// running sandbox rolls on is a composition decision, made by the scene that wires this spawner.
        /// </summary>
        private IRandomNumberGenerator _rnd = new DefaultRandomNumberGenerator();

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

        /// <summary>Seats the stream spawn decisions roll on. The scene that boots inside Godot hands over
        /// the engine-backed generator here; a host without the engine keeps the pure-C# default.</summary>
        public void SetRandomNumberGenerator(IRandomNumberGenerator rnd) => _rnd = rnd;

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
            float x = _rnd.RandIntRange(0, WorldWidth);
            float y = _rnd.RandIntRange(0, WorldHeight);
            return new Vector2(x, y);
        }

        private Rarity GetRandomRarity()
        {
            float roll = _rnd.RandFloat();
            float cumulative = 0f;

            foreach (KeyValuePair<Rarity, float> rarityChance in _rarityChances)
            {
                cumulative += rarityChance.Value;
                if (roll < cumulative)
                    return rarityChance.Key;
            }

            return Rarity.Uncommon;
        }

        private Fractions GetRandomFraction() => (Fractions)_rnd.RandIntRange(0, 6);

        private int GetRandomLevel(Rarity rarity) => rarity switch
        {
            Rarity.Uncommon => _rnd.RandIntRange(1, 15),
            Rarity.Rare => _rnd.RandIntRange(1, 25),
            Rarity.Epic => _rnd.RandIntRange(1, 35),
            Rarity.Legendary => _rnd.RandIntRange(1, 55),
            Rarity.Unique => _rnd.RandIntRange(1, 85),
            Rarity.Mythic => _rnd.RandIntRange(1, 150),
            _ => _rnd.RandIntRange(0, 10)
        };

        private EntityType GetRandomEntityType() => (EntityType)_rnd.RandIntRange(0, 5);

        private int GetAmountNpcModifiers(EntityType type, Rarity rarity)
        {
            int min = MinAmountNpcModifiers(type);
            return _rnd.RandIntRange(min, AmountNpcModifiersForRarity(rarity));
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
