namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source.Abilities.HeadButt;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Enums;

    /// <summary>
    /// The two things every walk about worn augments needs: an ability carrying the base numbers the
    /// case is measured on, and an arrangement whose slots are read in exactly the order the case names.
    /// Written once because both are the SETTING of a case and never its subject — two copies of either
    /// would let two walks quietly disagree about what "seated first" means.
    /// </summary>
    internal static class AugmentBench
    {
        /// <summary>A stand-in ability carrying the base numbers under test. Any ability would do — the
        /// augments walked here decorate keys every ability registers as part of the base contract.</summary>
        internal static HeadButt AbilityWith(int cost = 100, int cooldown = 5) => new(new AbilityBaseData
        {
            Id = "Ability_Head_Butt",
            Cooldown = cooldown,
            CostValue = cost,
            CostsType = Costs.Mana,
            AbilityProperties = new() { ["stunDuration"] = 1, ["attacks"] = 2 }
        });

        /// <summary>An arrangement whose slots are applied in exactly the order written. The socket ids
        /// are made up and sorted, because what these walks vary is the order the augments go on in and
        /// nothing about the slots themselves — the claim is that the order changes nothing.</summary>
        internal static IReadOnlyDictionary<string, IAugment> InThisOrder(params IAugment[] upgrades)
        {
            SortedDictionary<string, IAugment> seated = new(StringComparer.Ordinal);
            for (int slot = 0; slot < upgrades.Length; slot++) seated[$"socket_{slot}"] = upgrades[slot];
            return seated;
        }
    }
}
