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
        /// <summary>The augment's own record; null when the catalog holds no augment of that id.</summary>
        AbilityUpgradeData? Find(string augmentId);

        /// <summary>The combat tags of an ability; empty when the catalog does not hold the ability.</summary>
        IReadOnlyCollection<string> TagsOf(string abilityId);
    }
}
