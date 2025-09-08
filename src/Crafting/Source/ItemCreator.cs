namespace Crafting.Source
{
    using Godot;
    using System;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Core.Modifiers;
    using Core.Interfaces.Items;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;
    using Core.Interfaces;

    public class ItemCreator
    {
        private record FilteredMaterialModifier(IMaterialModifier Modifier, float Weight);
        private record WeightedMaterialModifier(IMaterialModifier Modifier, float From, float To, float Weight);
        private float _totalWeight;

        public IEquipItem? CreateEquipItem(ICraftingRecipe recipe, IEnumerable<ICraftingResource> mainResources, IEnumerable<ICraftingResource> optionalResources, ICharacter? player = default)
        {
            var modifiers = CalculateWeights(CombineModifiers(mainResources, optionalResources));
            return Create(recipe.ResultItemId, modifiers);
        }

        public IEquipItem? CreateGenericItem(ICraftingRecipe recipe, IEnumerable<ICraftingResource> mainResources, IEnumerable<ICraftingResource> optionalResources, ICharacter? player = default)
        {
            var rarity = GetRarity(player);
            var attribute = GetAttribute(player);
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

        private Rarity GetRarity(ICharacter? player = default)
        {
            if (player == null) return GetRandomValueFallBack<Rarity>();
            // TODO: Later add call to players skill to get Rarity
            return Rarity.Rare;
        }

        private AttributeType GetAttribute(ICharacter? player = default)
        {
            // TODO: Sometime i can get from this call AttributeType.None.
            if (player == null) return GetRandomValueFallBack<AttributeType>();
            // TODO: Later add call to players skill to get attribute
            return AttributeType.Dexterity;
        }

        private T GetRandomValueFallBack<T>()
            where T : struct, Enum
        {
            using var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            var values = Enum.GetValues<T>();
            var idx = (byte)rnd.RandiRange(0, values.Length - 1);
            return values[idx];
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
                    var x = new FilteredMaterialModifier(group.First(), group.Sum(c => c.Weight));
                    return x;
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
                weights.Add(new(modifier.Modifier, from, currentMaxWeight, modifier.Weight));
                from = currentMaxWeight;
            }
            foreach (var item in weights)
            {
                DebugLogger.LogDebug($"Percent: {MathF.Round(item.Weight / currentMaxWeight * 100), 2}% Modifier: {item.Modifier.Parameter}, {item.Modifier.ModifierType}, {item.Modifier.Value}");
            }
            _totalWeight = currentMaxWeight;
            DebugLogger.LogDebug($"Total weight: {currentMaxWeight}",this);
            return weights;
        }

        private IEquipItem? Create(string itemId, List<WeightedMaterialModifier> modifiers)
        {
            var dataProvider = ItemDataProvider.Instance;
            if (dataProvider == null)
            {
                Logger.LogNull(nameof(ItemDataProvider), this);
                return null;
            }

            var item = (IEquipItem?)dataProvider.CopyBaseItem(itemId);

            if (item == null)
            {
                Logger.LogNotFound($"Item with id: {itemId}", this);
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
                    var rNumb = rnd.RandfRange(0, _totalWeight);
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
            var skill = GetRandomSkill();
            if (skill != null) item.SetSkill(skill);

            List<IModifier> mods = [];
            foreach (var mod in takenMods)
            {
                mods.Add(ModifiersCreator.CreateModifier(mod.Parameter, mod.ModifierType, mod.Value, item));
            }

            item.SetAdditionalModifiers(mods);

            _totalWeight = 0;
            return item;
        }

        // TODO: An item can have skill or effect? Or both?
        private ISkill? GetRandomSkill()
        {
            // TODO: Later i need to find way to inject SkillProvider to get some skill
            // TODO: Random skill not guaranteed.
            return null;
        }

        // TODO: GetRandomEffect()

        private void AddAnyNotTakenMod(HashSet<IMaterialModifier> takenMods, List<WeightedMaterialModifier> modifiers)
        {
            Logger.LogInfo("Attemp limit was reached. Added first not taken modifier.", this);
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
