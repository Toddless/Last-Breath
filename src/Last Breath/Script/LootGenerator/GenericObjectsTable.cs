namespace Playground.Script.LootGenerator
{
    using Godot;
    using Playground.Script.Items;
    using Playground.Script.Items.Factories;
    using System;
    using System.Collections.Generic;

    public abstract class GenericObjectsTable<T>
        where T : GenericObject
    {
        public List<T>? LootDropItems;

        public List<ItemCreator>? Factories;

        private RandomNumberGenerator? _random = new();

        private float _probabilityTotalWeight;

        public virtual void ValidateTable()
        {
            SetFactories();
            if (LootDropItems != null && LootDropItems.Count > 0)
            {
                float currentProbabilityWeithMaximum = 0f;

                foreach (T lootDropItem in LootDropItems)
                {
                    if (lootDropItem.ProbabilityWeight < 0f)
                    {
                        lootDropItem.ProbabilityWeight = 0f;
                    }
                    else
                    {
                        lootDropItem.ProbabilityRangeFrom = currentProbabilityWeithMaximum;
                        currentProbabilityWeithMaximum += lootDropItem.ProbabilityWeight;
                        lootDropItem.ProbabilityRangeTo = currentProbabilityWeithMaximum;
                    }
                }
                _probabilityTotalWeight = currentProbabilityWeithMaximum;

                foreach (T lootDropItem in LootDropItems)
                {
                    lootDropItem.ProbabilityPercent = MathF.Round(lootDropItem.ProbabilityWeight / _probabilityTotalWeight * 100, 1);
                }
            }
        }

        public virtual Item? GetRandomItem()
        {
            if (LootDropItems == null || Factories == null)
            {
                ArgumentNullException.ThrowIfNull(LootDropItems);
                ArgumentNullException.ThrowIfNull(Factories);
            }
            var randomFactory = _random!.RandiRange(0, Factories.Count - 1);

            var factory = Factories[randomFactory];

            float pickedNumber = _random.RandfRange(0, _probabilityTotalWeight);
            foreach (T lootDropItem in LootDropItems)
            {
                if (pickedNumber >= lootDropItem.ProbabilityRangeFrom && pickedNumber <= lootDropItem.ProbabilityRangeTo)
                {
                    return factory?.GenerateItem(lootDropItem.Rarity);
                }
            }
            return null;
        }

        public virtual T? GetRarity()
        {
            float pickedNumber = _random!.RandfRange(0, _probabilityTotalWeight);
            foreach (T lootDropItem in LootDropItems!)
            {
                if (pickedNumber >= lootDropItem.ProbabilityRangeFrom && pickedNumber <= lootDropItem.ProbabilityRangeTo)
                {
                    return lootDropItem;
                }
            }
            return null;
        }

        public virtual Item? GetItemWithSelectedRarity(int index)
        {
            if (LootDropItems == null || Factories == null)
            {
                ArgumentNullException.ThrowIfNull(Factories);
                ArgumentNullException.ThrowIfNull(LootDropItems);
            }
            var randomFactory = _random!.RandiRange(0, Factories.Count - 1);

            var factory = Factories[randomFactory];

            return factory?.GenerateItem(LootDropItems[index].Rarity);
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
