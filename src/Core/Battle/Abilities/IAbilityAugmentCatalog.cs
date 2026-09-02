namespace Core.Battle.Abilities
{
    using System.Collections.Generic;
    using Data.AbilityData;

    /// <summary>
    /// What the ids of an install mean. Seating an augment names two things and holds neither: the
    /// augment (an id) and the ability whose slot it goes into (an id). Judging the fit needs the
    /// augment's declaration and the ability's tags, and both are catalog data — the same for every
    /// instance of either, and answerable for an augment nobody has built yet, which is what a drop,
    /// a conversion or a window listing candidates asks about.
    /// </summary>
    public interface IAbilityAugmentCatalog
    {
        /// <summary>Every augment record the data declares. Asked by whoever offers augments instead
        /// of judging one already named — an augment says which sockets take it and no index is kept
        /// the other way round, so "what may go into this slot" is answered by putting the whole
        /// section to the rule.</summary>
        IReadOnlyCollection<AbilityAugmentData> All { get; }

        /// <summary>The augment's own record; null when the catalog holds no augment of that id.</summary>
        AbilityAugmentData? Find(string augmentId);

        /// <summary>Every ability record the data declares, hidden ones included — what an ability is
        /// before anything is built out of it. Asked by whoever lists or edits the abilities instead of
        /// naming one, which is a question the module that BUILDS them cannot be the only answer to.</summary>
        IReadOnlyCollection<AbilityBaseData> Abilities { get; }

        /// <summary>The ids of those records. Membership is its own question — a passive node and an NPC
        /// entry both ask "is this id written anywhere" per node and per entry — and answering it by
        /// walking the records would make a lookup out of every one of them.</summary>
        IReadOnlyCollection<string> AbilityIds { get; }

        /// <summary>The ability's own record; null when the catalog holds no ability of that id.</summary>
        AbilityBaseData? FindAbility(string abilityId);

        /// <summary>The combat tags of an ability; empty when the catalog does not hold the ability.</summary>
        IReadOnlyCollection<string> TagsOf(string abilityId);
    }
}
