namespace Playground.Script.LootGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Playground.Script.Enums;
    using Playground.Script.Items;
    using Playground.Script.Items.Factories;

    public abstract class GenericObjectsTable<T>
        where T : GenericObject
    {
        public List<T>? LootDropItems;

        public List<ItemCreator>? Factories;

        private readonly RandomNumberGenerator _random = new();

        private float _probabilityTotalWeight;

        public virtual void ValidateTable()
        {
            SetFactories();
            if (LootDropItems != null && LootDropItems.Count > 0)
            {
                float currentProbabilityWeighMaximum = 0f;

                foreach (T lootDropItem in LootDropItems)
                {
                    if (lootDropItem.ProbabilityWeight < 0f)
                    {
                        lootDropItem.ProbabilityWeight = 0f;
                    }
                    else
                    {
                        lootDropItem.ProbabilityRangeFrom = currentProbabilityWeighMaximum;
                        currentProbabilityWeighMaximum += lootDropItem.ProbabilityWeight;
                        lootDropItem.ProbabilityRangeTo = currentProbabilityWeighMaximum;
                    }
                }
                _probabilityTotalWeight = currentProbabilityWeighMaximum;

                foreach (T lootDropItem in LootDropItems)
                {
                    lootDropItem.ProbabilityPercent = MathF.Round(lootDropItem.ProbabilityWeight / _probabilityTotalWeight * 100, 1);
                }
            }
        }

        public virtual Item GetRandomItem()
        {
            float pickedNumber = _random!.RandfRange(0, _probabilityTotalWeight);
            return Factories![_random!.RandiRange(0, Factories!.Count - 1)]
                .GenerateItem(LootDropItems!.FirstOrDefault(item => pickedNumber >= item.ProbabilityRangeFrom && pickedNumber <= item.ProbabilityRangeTo)?.Rarity ?? GlobalRarity.Uncommon);
        }

        public virtual T? GetRarity()
        {
            float pickedNumber = _random!.RandfRange(0, _probabilityTotalWeight);
            return LootDropItems!.FirstOrDefault(rarity => pickedNumber >= rarity.ProbabilityRangeFrom && pickedNumber <= rarity.ProbabilityRangeTo);
        }

        public virtual Item? GetItemWithSelectedRarity(int index)
        {
            return Factories![_random!.RandiRange(0, Factories.Count - 1)]?.GenerateItem(LootDropItems![index].Rarity);
        }

        private void SetFactories()
        {
            Factories =
            [
                new BowFactory(_random!),
                new SwordFactory(_random!),
                new BodyArmorFactory(_random!),
            ];
        }
    }
}
