namespace Battle.Source.Abilities.SeriesOfAttacks
{
    public class SoAsAugmentUnevadable(string id, string[] tags, int tier)
        : DelegateAugment<SeriesOfAttacks>(
            id, tags, tier, ability => ability.IsEvadable = false, ability => ability.IsEvadable = true);
}
