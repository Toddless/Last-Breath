namespace Battle.Source.Abilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Core.Enums;
    using Newtonsoft.Json;

    /// <summary>
    /// The abilities as their data declares them: what each one is built from, and which augments it
    /// is offered. The records of the augments themselves are not read here — they are asked of
    /// <see cref="IAbilityAugmentCatalog"/>, which every composition holds, while the code that turns
    /// one into an upgrade is this module's own.
    /// </summary>
    public partial class AbilityProvider(IAbilityAugmentCatalog augments) : IAbilityProvider, IGameDataParticipant
    {
        private readonly Dictionary<string, AbilityBaseData> _abilityBaseData = [];

        public IReadOnlyList<string> Catalogs => [DataCatalog.Abilities];

        public IReadOnlyCollection<string> KnownAbilityIds => _abilityBaseData.Keys;

        /// <summary>Every augment this build knows how to make an upgrade of — the augments a factory
        /// is written for and the augments the parameter table describes, which are two halves of one
        /// registry and never name the same id twice. A record the data declares and this collection
        /// does not name is a record that parses, is offered and then silently produces nothing.</summary>
        public IReadOnlyCollection<string> BuildableAugmentIds => [.. _abilityUpgrades.Keys, .. _parameterAugments.Keys];

        public void Apply(string catalog, GameDataFile file)
        {
            var root = JsonConvert.DeserializeObject<AbilityDataRoot>(file.Json)
                       ?? throw new InvalidOperationException("Failed to deserialize ability data");
            foreach (AbilityBaseData abilityData in root.Abilities) _abilityBaseData[abilityData.Id] = abilityData;
        }

        public IAbility CreateAbility(string abilityId)
        {
            var data = GetBaseData(abilityId);
            var ability = GetFactory(abilityId).Invoke(data);
            ability.SetAbilityUpgrades(AugmentsOffered(data));
            return ability;
        }

        /// <summary>The stance an ability belongs to — the ability book partitions by it on Learn.</summary>
        public Stance GetAbilityStance(string abilityId) => GetBaseData(abilityId).Stance;

        /// <summary>Internal-cast-only ability (boss reactions): never learnable, never shown in trees.</summary>
        public bool IsHidden(string abilityId) => GetBaseData(abilityId).Hidden;

        private AbilityBaseData GetBaseData(string abilityId) =>
            _abilityBaseData.GetValueOrDefault(abilityId)
            ?? throw new KeyNotFoundException($"No base data loaded for ability '{abilityId}'");

        private Func<AbilityBaseData, IAbility> GetFactory(string abilityId) =>
            _abilityFactories.GetValueOrDefault(abilityId)
            ?? throw new KeyNotFoundException($"No factory registered for ability '{abilityId}'");

        /// <summary>The augments an ability may wear, grouped by the tier each of them is written at.
        /// The offering is no longer a list the ability declares: every record of the catalog is put
        /// to the fitting rule against a slot of this ability, so what answers is the augment's own
        /// declaration. Nothing is passed as already worn — the offering says what may ever go in,
        /// while an exclusion group speaks about what is sitting there right now.</summary>
        private Dictionary<int, List<IAbilityUpgrade>> AugmentsOffered(AbilityBaseData ability) =>
            augments.All
                .Where(augment => BelongsOn(ability, augment))
                .GroupBy(augment => augment.Tier)
                .ToDictionary(group => group.Key, group => group.Select(CreateUpgrade).OfType<IAbilityUpgrade>().ToList());

        /// <summary>Whether the augment belongs on that ability at all, asked of the one rule that
        /// answers it. The slot handed over is the ability's own, opened exactly deep enough for the
        /// record — the offering is about belonging, and a socket too shallow is a question for the
        /// board that holds the real slots. It carries no id because no slot is meant: nothing here
        /// is being seated.</summary>
        private static bool BelongsOn(AbilityBaseData ability, AbilityUpgradeData augment) =>
            AugmentFit.Check(
                new AbilitySocketPlacement(string.Empty, ability.Id, augment.Tier),
                ability.Tags,
                augment,
                []) == AugmentFitResult.Fits;

        /// <summary>The upgrade an augment record installs, built from that record alone. Null for an
        /// id neither half of the registry answers — the record parses and is offered, and this is
        /// where that silence is reported.</summary>
        public IAbilityUpgrade? CreateUpgrade(AbilityUpgradeData data)
        {
            IAbilityUpgrade? upgrade = Build(data);
            if (upgrade == null)
            {
                Tracker.TrackNotFound($"Upgrade factory '{data.Id}'", this);
                return null;
            }

            // Placeholder = json property name; live upgrade descriptions come for free
            upgrade.DescriptionValues = data.UpgradeProperties.ToDictionary(entry => entry.Key, object? (entry) => entry.Value);
            return upgrade;
        }

        /// <summary>The two halves of the registry, asked in order: an augment reaching into the members
        /// of an ability has a factory written for it, one that only moves numbers is a row of the
        /// parameter table and needs no code of its own.</summary>
        private IAbilityUpgrade? Build(AbilityUpgradeData data)
        {
            if (_abilityUpgrades.TryGetValue(data.Id, out var factory)) return factory(data);

            return _parameterAugments.TryGetValue(data.Id, out AugmentParameterMove[]? moves)
                ? new AbilityUpgradeParameterSet(data.Id, data.Tags, data.Tier,
                    [.. moves.Select(move => (move.Parameter, move.Operation, move.AmountIn(data.UpgradeProperties)))])
                : null;
        }
    }
}
