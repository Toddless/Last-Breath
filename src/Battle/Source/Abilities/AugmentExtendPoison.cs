namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;
    using Riders;

    /// <summary>
    /// A landing hit extends the poison already on what it touched. The sentence belongs to no ability:
    /// whoever swings, the poison on the target lasts longer, and the rider that does it
    /// (<see cref="ExtendPoisonOnHitRider"/>) reads nothing but the impact it is handed.
    ///
    /// It used to be said twice, once bolted onto Increasing Pressure and once onto Poison Coating, and
    /// both of those classes were unreachable in their own way — the first was written for an ability its
    /// record's tags never led to, the second had lost its body entirely and installed nothing at all.
    /// One record and one class now, sitting beside the other upgrades that belong to the contract rather
    /// than to a class.
    ///
    /// Its tags seat it on Poison Coating, which does not call <c>ApplyImpactRiders</c> at all — the
    /// augment is seated and inert there until the riders move into the common delivery path (A-2 of
    /// <c>Docs/PLAN-Augments.md</c>). A tag promises a fit, not a result.
    /// </summary>
    public class AugmentExtendPoison(string id, string[] tags, int tier, int extension)
        : AbilityAugmentImpactRider(id, tags, tier, () => new ExtendPoisonOnHitRider(extension))
    {
        public override IAbilityAugment Copy() => new AugmentExtendPoison(Id, Tags, Tier, extension);
    }
}
