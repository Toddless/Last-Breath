namespace Battle.Source.Abilities.SeriesOfAttacks
{
    public class SoAsUpgradeUnevadable(string id, string[] tags, int tier)
        : DelegateUpgrade<SeriesOfAttacks>(
            id, tags, tier, ability => ability.IsEvadable = false, ability => ability.IsEvadable = true);
}
