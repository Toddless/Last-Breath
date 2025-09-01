namespace Crafting.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;
    using Core.Modifiers;
    using Godot;

    public class ItemCreator
    {
        private record FilteredMaterialModifier(IMaterialModifier Modifier, float Weight);
        private record WeightedMaterialModifier(IMaterialModifier Modifier, float From, float To);
        private float _maxWeight;

        public IEquipItem? CreateEquipItem(ICraftingRecipe recipe, IEnumerable<ICraftingResource> mainResources, IEnumerable<ICraftingResource> optionalResources)
        {
            var modifiers = CalculateWeights(CombineModifiers(mainResources, optionalResources));
            return Create(recipe.ResultItemId, modifiers);
        }

        public IEquipItem? CreateGenericItem(ICraftingRecipe recipe, IEnumerable<ICraftingResource> mainResources, IEnumerable<ICraftingResource> optionalResources)
        {
            using var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            //                          нужно как то влиять на эти значения, чтоб мы могли повышать шанс на более редкий предмет
            var rarity = GetRarity((byte)rnd.RandiRange(0, 3));
            //                          Должны ли мы как то влиять на данное значение??
            var attribute = GetAttribute((byte)rnd.RandiRange(1, 3));
            var itemId = $"{recipe.ResultItemId}_{attribute}_{rarity}";
            var modifiers = CalculateWeights(CombineModifiers(mainResources, optionalResources));
            var item = Create(itemId, modifiers);
            return item;
        }

        public IItem? CreateItem(ICraftingRecipe recipe)
        {
            // for creating basic items i need a new item provider
            return null;
        }

        private Rarity GetRarity(byte idx)
        {
            var rarity = Enum.GetValues<Rarity>().GetValue(idx);
            if (rarity == null) return Rarity.Uncommon;
            return (Rarity)rarity;
        }

        private AttributeType GetAttribute(byte idx)
        {
            var attribute = Enum.GetValues<AttributeType>().GetValue(idx);
            if (attribute == null) return AttributeType.Dexterity;

            return (AttributeType)attribute;
        }

        private int GetAmountModifiers(Rarity rarity) => (int)rarity + 1;

        private List<FilteredMaterialModifier> CombineModifiers(IEnumerable<ICraftingResource> main, IEnumerable<ICraftingResource> optional)
        {
            return main
                .Concat(optional)
                .SelectMany(res => res.MaterialType?.Modifiers ?? [])
                .GroupBy(mod => new { mod.ModifierType, mod.Parameter, mod.Value })
                .Select(group =>
                {
                    return new FilteredMaterialModifier(group.First(), group.Sum(c => c.Weight));
                }).ToList();
        }

        private List<WeightedMaterialModifier> CalculateWeights(List<FilteredMaterialModifier> modifiers)
        {
            List<WeightedMaterialModifier> weights = [];
            float currentMaxWeight = 0;
            float from = 0;
            foreach (var modifier in modifiers)
            {
                currentMaxWeight += modifier.Weight;
                weights.Add(new(modifier.Modifier, from, currentMaxWeight));
                from = currentMaxWeight;
            }
            _maxWeight = currentMaxWeight;
            return weights;
        }

        private IEquipItem? Create(string itemId, List<WeightedMaterialModifier> modifiers)
        {
            var dataProvider = EquipItemDataProvider.Instance;
            if (dataProvider == null)
            {
                // TODO: Log
                return null;
            }

            var item = dataProvider.GetItem(itemId);

            if (item == null)
            {
                // TODO: Log
                return null;
            }

            var baseStats = dataProvider.GetItemStats(itemId);
            var statModifiers = ModifiersCreator.ItemStatsToModifier(baseStats, item).ToHashSet();
            item.SetBaseModifiers(statModifiers);

            var amountModifiers = GetAmountModifiers(item.Rarity);
            HashSet<IMaterialModifier> takenMods = [];
            using var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            const int maxAttemps = 15;

            while (amountModifiers > 0)
            {
                TakeMode(maxAttemps);
                void TakeMode(int attemp)
                {
                    if (attemp <= 0)
                    {
                        AddAnyNotTakenMod(takenMods, modifiers);
                        return;
                    }
                    attemp--;
                    var rNumb = rnd.RandfRange(0, _maxWeight);
                    var mod = modifiers.FirstOrDefault(x => rNumb >= x.From && rNumb <= x.To)?.Modifier;
                    if (mod != null)
                    {
                        if (!takenMods.Add(mod))
                            TakeMode(attemp);
                    }
                }
                amountModifiers--;
            }
            // TODO : Change to get random effect/ability
          
            List<IModifier> mods = [];
            foreach (var mod in takenMods)
                mods.Add(ModifiersCreator.CreateModifier(mod.Parameter, mod.ModifierType, mod.Value, item));

            item.SetAdditionalModifiers(mods);

            _maxWeight = 0;
            return item;
        }

        private void AddAnyNotTakenMod(HashSet<IMaterialModifier> takenMods, List<WeightedMaterialModifier> modifiers)
        {
            // TODO: Log
            foreach (var mod in modifiers)
            {
                if (!takenMods.Contains(mod.Modifier))
                {
                    takenMods.Add(mod.Modifier);
                    break;
                }
            }
        }
    }
}
