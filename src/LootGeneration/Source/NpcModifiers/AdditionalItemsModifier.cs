namespace LootGeneration.Source.NpcModifiers
{
    using System.Linq;
    using Core.Interfaces;
    using Core.Data.LootTable;
    using Core.Interfaces.Entity;
    using System.Collections.Generic;

    public class AdditionalItemsModifier(
        string id,
        float weight,
        float difficultyMultiplier,
        Dictionary<int, List<TableRecord>> items,
        bool isUnique,
        string npcBuffId) : NpcModifier(id, weight, difficultyMultiplier, isUnique, npcBuffId), IAdditionalItemsModifier
    {
        public Dictionary<int, List<TableRecord>> Items { get; } = items;

        public override void ApplyModifier(IModifierApplyingContext context)
        {
            foreach (KeyValuePair<int, List<TableRecord>> tiers in Items)
            {
                var records = tiers.Value;
                if (!context.AdditionalItems.TryGetValue(tiers.Key, out List<TableRecord>? additionalItems))
                {
                    context.AdditionalItems.Add(tiers.Key, records);
                    continue;
                }

                foreach (var tableRecord in tiers.Value.Where(tableRecord => !additionalItems.Contains(tableRecord)))
                    additionalItems.Add(tableRecord);
            }
        }

        public override INpcModifier Copy() => new AdditionalItemsModifier(Id, Weight, BaseDifficultyMultiplier, Items, IsUnique, NpcBuffId);
    }
}
