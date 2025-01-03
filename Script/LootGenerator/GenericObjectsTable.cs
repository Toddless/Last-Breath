namespace Playground.Script.LootGenerator
{
    using Godot;
    using Playground.Script.Items;
    using Playground.Script.Items.Factories;
    using System;
    using System.Collections.Generic;

    public abstract class GenericObjectsTable<T, U>
        where T : GenericObject<U> where U : class
    {
        public List<T>? lootDropItems;

        public List<ItemCreator>? factories;

        private readonly RandomNumberGenerator random = new();

        private float probabilityTotalWeight;

        public virtual void ValidateTable()
        {
            SetFactories();
            if (lootDropItems != null && lootDropItems.Count > 0)
            {
                float currentProbabilityWeithMaximum = 0f;

                foreach (T lootDropItem in lootDropItems)
                {
                    if (lootDropItem.probabilityWeight < 0f)
                    {
                        lootDropItem.probabilityWeight = 0f;
                    }
                    else
                    {
                        lootDropItem.probabilityRangeFrom = currentProbabilityWeithMaximum;
                        currentProbabilityWeithMaximum += lootDropItem.probabilityWeight;
                        lootDropItem.probabilityRangeTo = currentProbabilityWeithMaximum;
                    }
                }
                probabilityTotalWeight = currentProbabilityWeithMaximum;

                foreach (T lootDropItem in lootDropItems)
                {
                    lootDropItem.probabilityPercent = lootDropItem.probabilityWeight / probabilityTotalWeight * 100;
                }
            }
        }

        public virtual Item? GetRandomItem()
        {
            if (lootDropItems == null || factories == null)
            {
                ArgumentNullException.ThrowIfNull(lootDropItems);
                ArgumentNullException.ThrowIfNull(factories);
            }
            var randomFactory = random.RandiRange(0, factories.Count - 1);

            var factory = GetFactory(randomFactory);

            float pickedNumber = random.RandfRange(0, probabilityTotalWeight);
            foreach (T lootDropItem in lootDropItems)
            {
                if (pickedNumber >= lootDropItem.probabilityRangeFrom && pickedNumber <= lootDropItem.probabilityRangeTo)
                {
                    return factory?.GenerateItem(lootDropItem.Rarity);
                }
            }
            return null;
        }

        public virtual T? GetRarity()
        {
            float pickedNumber = random.RandfRange(0, probabilityTotalWeight);
            foreach (T lootDropItem in lootDropItems!)
            {
                if (pickedNumber >= lootDropItem.probabilityRangeFrom && pickedNumber <= lootDropItem.probabilityRangeTo)
                {
                    return lootDropItem;
                }
            }
            return null;
        }

        public virtual Item? GetItemWithSelectedRarity(int index)
        {
            if (lootDropItems == null || factories == null)
            {
                ArgumentNullException.ThrowIfNull(factories);
                ArgumentNullException.ThrowIfNull(lootDropItems);
            }
            var randomFactory = random.RandiRange(0, factories.Count - 1);

            var factory = GetFactory(randomFactory);

            return factory?.GenerateItem(lootDropItems[index].Rarity);
        }

        private void SetFactories()
        {
            factories =
            [
                new BowFactory(),
                new SwordFactory(),
                new BodyArmorFactory(),
            ];
        }

        private ItemCreator? GetFactory(int factoryIndex)
        {
            return factoryIndex switch
            {
                0 => new BowFactory(),
                1 => new SwordFactory(),
                2 => new BodyArmorFactory(),
                _ => null,
            };
        }
    }
}
