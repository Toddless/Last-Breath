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
    using Core.Localization;
    using Newtonsoft.Json;

    /// <summary>
    /// The abilities as their data declares them: what each one is built from, and how a copy of an
    /// augment becomes the upgrade it installs. The records of the augments themselves are not read
    /// here — they are asked of <see cref="IAbilityAugmentCatalog"/>, which every composition holds,
    /// while the code that turns one into an upgrade is this module's own.
    ///
    /// An ability is created bare. What it wears is not a property of the ability but of the sockets
    /// its owner has filled, so the arrangement is put on by <see cref="IAbilityAugmentBinder"/> and
    /// never handed over at construction.
    /// </summary>
    public partial class AbilityProvider(IAbilityAugmentCatalog augments, Func<IEffectProvider?>? effects = null)
        : IAbilityProvider, IGameDataParticipant
    {
        private readonly Dictionary<string, AbilityBaseData> _abilityBaseData = [];

        /// <summary>Resolved lazily: a sandbox without an effect registry still builds every other
        /// augment, and a behaviour that needs one refuses out loud instead of throwing.</summary>
        private readonly Func<IEffectProvider?> _effects = effects ?? (static () => null);

        public IReadOnlyList<string> Catalogs => [DataCatalog.Abilities];

        public IReadOnlyCollection<string> KnownAbilityIds => _abilityBaseData.Keys;

        /// <summary>Every augment this build knows how to make an upgrade of — the augments a factory
        /// is written for and the augments the parameter table describes, which are two halves of one
        /// registry and never name the same id twice. A record the data declares and this collection
        /// does not name is a record that parses, is minted, is seated and then does nothing at all.</summary>
        public IReadOnlyCollection<string> BuildableAugmentIds =>
        [
            .. AbilityUpgrades.Keys,
            .. _parameterAugments.Keys,
            // Records that carry their own behaviour need no line of code naming them.
            .. augments.All.Where(record => !string.IsNullOrWhiteSpace(record.Behaviour)).Select(record => record.Id)
        ];

        /// <summary>
        /// The parameters a record moves, by the same rule the effect registry publishes the keys a
        /// factory reads: a ledger that has to GUESS what a record touches guesses wrong. Asked from the
        /// outside it answered only "which abilities does this fit", which is a different question —
        /// every ability a record fits declares parameters the record never moves.
        /// <para>Both halves of the registry answer: the numeric table says which key each of its rows
        /// stands on, and a factory registration declares its keys beside itself. A factory used to be
        /// silent here, which made every ledger of shared keys blind to that half and left its rows to
        /// be written by hand. Still empty for a record answered by a declared behaviour, and for the
        /// factories whose work is a rider or a flag rather than a key — there the silence is the
        /// honest answer rather than a gap.</para>
        /// </summary>
        public IReadOnlyCollection<string> ParametersMovedBy(string augmentId) =>
            [.. TableMoves(augmentId).Concat(FactoryMoves(augmentId)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

        /// <summary>The keys the numeric table has this record standing on.</summary>
        private IEnumerable<string> TableMoves(string augmentId) =>
            _parameterAugments.TryGetValue(augmentId, out AugmentParameterMove[]? moves)
                ? moves.Select(move => move.Parameter)
                : [];

        /// <summary>The keys the record's own factory declares it moves.</summary>
        private IEnumerable<string> FactoryMoves(string augmentId) =>
            AbilityUpgrades.TryGetValue(augmentId, out AugmentFactory factory) ? factory.MovedParameters : [];

        public void Apply(string catalog, GameDataFile file)
        {
            var root = JsonConvert.DeserializeObject<AbilityDataRoot>(file.Json)
                       ?? throw new InvalidOperationException("Failed to deserialize ability data");
            foreach (AbilityBaseData abilityData in root.Abilities) _abilityBaseData[abilityData.Id] = abilityData;
        }

        public IAbility CreateAbility(string abilityId) => GetFactory(abilityId).Invoke(GetBaseData(abilityId));

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

        /// <summary>
        /// The upgrade one COPY of an augment installs. The copy's numbers go in ahead of the record's
        /// own, so the behaviour installed and the description printed are built from the same
        /// dictionary: a player reading a tooltip of the augment in his slot reads what that copy
        /// rolled, and not the average the record declares.
        /// Null for a copy of a record the catalog does not hold — nothing says what to build.
        /// </summary>
        public IAugment? CreateUpgrade(AugmentInstance instance)
        {
            AbilityAugmentData? record = augments.Find(instance.AugmentId);
            if (record != null) return CreateUpgrade(instance.Applied(record));

            Tracker.TrackNotFound($"Augment record '{instance.AugmentId}'", this);
            return null;
        }

        /// <summary>The upgrade an augment record installs, built from that record alone — the augment
        /// as the data declares it, before any copy of it is minted. Null for an id neither half of the
        /// registry answers: the record parses and is mintable, and this is where that silence is
        /// reported.</summary>
        public IAugment? CreateUpgrade(AbilityAugmentData data)
        {
            IAugment? upgrade = Build(data);
            if (upgrade == null)
            {
                Tracker.TrackNotFound($"Upgrade factory '{data.Id}'", this);
                return null;
            }

            // Placeholder = json property name; live upgrade descriptions come for free
            Dictionary<string, object?> printed = data.UpgradeProperties.ToDictionary(entry => entry.Key, object? (entry) => entry.Value);

            // What a record lays, under a placeholder of its own — the line of a pool record cannot be
            // written without it, since which effect a copy lays is the copy's draw, and a record that
            // names its effect outright is free to use it or not.
            if (!string.IsNullOrWhiteSpace(data.LaidEffectId))
                printed[AbilityAugmentData.EffectPlaceholder] = new LocalizedId(data.LaidEffectId);

            upgrade.DescriptionValues = printed;
            return upgrade;
        }

        /// <summary>The three halves of the registry, asked in order: a record that DECLARES a behaviour
        /// is built from its own data, an augment reaching into the members of an ability has a factory
        /// written for it, and one that only moves numbers is a row of the parameter table.</summary>
        private IAugment? Build(AbilityAugmentData data)
        {
            // Not a priority: a record with both a behaviour and a factory is a duplicate id, which the
            // uniqueness of BuildableAugmentIds already refuses loudly. The sets do not overlap.
            if (!string.IsNullOrWhiteSpace(data.Behaviour)) return CreateBehaviour(data);

            if (AbilityUpgrades.TryGetValue(data.Id, out AugmentFactory factory)) return factory.Build(data);

            return _parameterAugments.TryGetValue(data.Id, out AugmentParameterMove[]? moves)
                ? new AugmentParameterSet(data.Id, data.Tags, data.Tier,
                    [.. moves.Select(move => (move.Parameter, move.Operation, move.AmountIn(data.UpgradeProperties)))])
                : null;
        }
    }
}
