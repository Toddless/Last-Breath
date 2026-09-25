namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Battle;
    using Core.Data;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Godot;

    public class BattleExperienceProcessor : IDisposable
    {
        private const float BaseExp = 50;

        /// <summary>A fled enemy is a partial victory: half the experience of a kill.</summary>
        private const float FledExperienceFactor = 0.5f;

        /// <summary>Experience bonus per NPC type. The ladder mirrors the loot budget one
        /// (Regular → Archon): a more dangerous type is never worth less than a lesser one.</summary>
        private static readonly Dictionary<EntityType, float> s_typeMultipliers = new()
        {
            [EntityType.Regular] = 0.05f,
            [EntityType.Special] = 0.1f,
            [EntityType.Elit] = 0.15f,
            [EntityType.Unique] = 0.25f,
            [EntityType.Boss] = 0.4f,
            [EntityType.Archon] = 0.9f
        };

        /// <summary>Experience bonus per NPC rarity. The scale of Rarity is inverted (Legendary = 0 …
        /// Common = 4), so the ladder is written by quality, not by the numeric value of the member.</summary>
        private static readonly Dictionary<Rarity, float> s_rarityMultipliers = new()
        {
            [Rarity.Common] = 0.05f,
            [Rarity.Uncommon] = 0.06f,
            [Rarity.Rare] = 0.07f,
            [Rarity.Epic] = 0.15f,
            [Rarity.Legendary] = 0.3f,
            [Rarity.Unique] = 0.5f,
            [Rarity.Mythic] = 0.9f
        };

        private readonly IGameServiceProvider _gameServiceProvider;
        private readonly IBattleEventBus _battleEventBus;
        private readonly IFightable _player;
        private readonly List<string> _diedEntities = [];
        private int _totalExp;

        public BattleExperienceProcessor(IBattleEventBus eventBus, IGameServiceProvider provider, IFightable player)
        {
            _gameServiceProvider = provider;
            _battleEventBus = eventBus;
            _player = player;
            _battleEventBus.Subscribe<EntityDiedEvent>(OnEntityDiedEvent);
            _battleEventBus.Subscribe<EntityFledBattleEvent>(OnEntityFledEvent);
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
                BattleResults.PlayerFled => 0, // fled — earned nothing
                _ => _totalExp
            };

            // TODO:
            // Необходимо сменить на GameMessageBus.PublishAsync<BattleExperienceGainedMessage>
            // и создать обработчик данного сообщения. Внутри обработчика начисляем опыт мастерству. Там же позднее будем записывать статистику (для ачивок/глобальная статистика)

            var martialArtMastery = _gameServiceProvider.GetService<IMartialArtMastery>();
            martialArtMastery.AddExperience(awarded);
            _totalExp = 0;
        }

        public void Dispose()
        {
            _battleEventBus.Unsubscribe<EntityDiedEvent>(OnEntityDiedEvent);
            _battleEventBus.Unsubscribe<EntityFledBattleEvent>(OnEntityFledEvent);
            _diedEntities.Clear();
        }

        private void OnEntityDiedEvent(EntityDiedEvent obj) => AccrueFor(obj.Entity, 1f);

        private void OnEntityFledEvent(EntityFledBattleEvent obj) => AccrueFor(obj.Entity, FledExperienceFactor);

        private void AccrueFor(IFightable entity, float factor)
        {
            if (!CanGetExperience(entity)) return;
            var npc = entity as IFightableNpc;
            int experienceAmount = Mathf.RoundToInt((BaseExp + npc!.Level) * CalculateTotalMultiplier(npc.EntityType, npc.Rarity) * factor);
            _totalExp += experienceAmount;
            _diedEntities.Add(entity.InstanceId); // counted once: a fled enemy killed later doesn't double-dip
        }

        /// <summary>Only the player's ENEMIES are worth experience: a fallen companion is a loss,
        /// not a reward, and a summon is a spell, not a kill.</summary>
        private bool CanGetExperience(IFightable entity) =>
            entity is not IPlayer && entity is IFightableNpc { IsSummon: false } && !_diedEntities.Contains(entity.InstanceId) && !IsPlayerAlly(entity);

        private bool IsPlayerAlly(IFightable entity) =>
            entity.Group != null && ReferenceEquals(entity.Group, _player.Group);

        private float CalculateTotalMultiplier(EntityType type, Rarity rarity) => 1f + (NpcTypeToMultiplier(type) + RarityToMultiplier(rarity));

        private float NpcTypeToMultiplier(EntityType npcEntityType) => LookupMultiplier(s_typeMultipliers, npcEntityType);

        private float RarityToMultiplier(Rarity npcRarity) => LookupMultiplier(s_rarityMultipliers, npcRarity);

        /// <summary>A member missing from the ladder is a gap in the catalog, not a reward of zero:
        /// the kill is paid at the weakest known rate and the gap is reported.</summary>
        private float LookupMultiplier<TKey>(IReadOnlyDictionary<TKey, float> multipliers, TKey member) where TKey : notnull
        {
            if (multipliers.TryGetValue(member, out float multiplier)) return multiplier;

            float weakestKnown = multipliers.Values.Min();
            Tracker.TrackError($"No experience multiplier for {typeof(TKey).Name}.{member}; paid at the weakest known rate {weakestKnown}.", this);
            return weakestKnown;
        }
    }
}
