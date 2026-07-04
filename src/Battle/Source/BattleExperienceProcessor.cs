namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events;
    using Core.Interfaces.Events.GameEvents;
    using Godot;

    public class BattleExperienceProcessor : IDisposable
    {
        private const float BaseExp = 50;
        private readonly IGameServiceProvider _gameServiceProvider;
        private readonly IBattleEventBus _battleEventBus;
        private readonly List<string> _diedEntities = [];
        private int _totalExp;

        public BattleExperienceProcessor(IBattleEventBus eventBus, IGameServiceProvider provider)
        {
            _gameServiceProvider = provider;
            _battleEventBus = eventBus;
            _battleEventBus.Subscribe<EntityDiedEvent>(OnEntityDiedEvent);
        }

        /// <summary>
        /// Awards the accumulated experience. Called explicitly by BattleContext with the battle
        /// results — a BattleEndEvent subscription would be dead: that event goes to the game bus
        /// (published by Main AFTER the context is disposed), not to the battle bus.
        /// </summary>
        public void CompleteBattle(BattleResults results)
        {
            int awarded = results switch
            {
                BattleResults.PlayerLost => Mathf.RoundToInt(_totalExp * 0.3f),
                BattleResults.BattleAbandoned => 0,
                _ => _totalExp
            };

            var martialArtMastery = _gameServiceProvider.GetService<IMartialArtMastery>();
            martialArtMastery.AddExperience(awarded);
            _totalExp = 0;
        }

        public void Dispose()
        {
            _battleEventBus.Unsubscribe<EntityDiedEvent>(OnEntityDiedEvent);
            _diedEntities.Clear();
        }

        private void OnEntityDiedEvent(EntityDiedEvent obj)
        {
            if (!CanGetExperience(obj.Entity)) return;
            var npc = obj.Entity as IFightableNpc;
            int experienceAmount = Mathf.RoundToInt((BaseExp + npc!.Level) * CalculateTotalMultiplier(npc.EntityType, npc.Rarity));
            _totalExp += experienceAmount;
            _diedEntities.Add(obj.Entity.InstanceId);
        }

        private bool CanGetExperience(IFightable entity) => entity is not IPlayer && entity is IFightableNpc && !_diedEntities.Contains(entity.InstanceId);

        private float CalculateTotalMultiplier(EntityType type, Rarity rarity) => 1f + (NpcTypeToMultiplier(type) + RarityToMultiplier(rarity));

        private float NpcTypeToMultiplier(EntityType npcEntityType) => npcEntityType switch
        {
            EntityType.Regular => 0.05f,
            EntityType.Special => 0.7f,
            EntityType.Elit => 0.15f,
            EntityType.Unique => 0.25f,
            EntityType.Boss => 0.4f,
            EntityType.Archon => 0.9f,
            _ => 0f
        };

        private float RarityToMultiplier(Rarity npcRarity) => npcRarity switch
        {
            Rarity.Common => 0.05f,
            Rarity.Rare => 0.07f,
            Rarity.Epic => 0.15f,
            Rarity.Legendary => 0.3f,
            Rarity.Unique => 0.5f,
            Rarity.Mythic => 0.9f,
            _ => 0f
        };
    }
}
