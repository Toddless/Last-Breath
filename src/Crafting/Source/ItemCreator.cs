namespace Crafting.Source
{
    using Godot;
    using System;
    using Utilities;
    using Core.Data;
    using Core.Enums;
    using System.Linq;
    using Core.Modifiers;
    using Core.Interfaces.Items;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;

    public class ItemCreator(ICraftingMastery craftingMastery, RandomNumberGenerator rnd, IItemDataProvider itemDataProvider)
        : IItemCreator
    {
        public IEquipItem CreateEquipItem(string recipeId, IEnumerable<IModifier> resources, IEntity? player = null)
        {
            var recipe = itemDataProvider.GetRecipe(recipeId);
            (List<WeightedObject<IModifier>> mods, float totalWeight) = WeightedRandomPicker.CalculateWeights(resources);
            if (!recipe.HasTag("Generic"))
            {
                return Create(recipe.ResultItemId, mods, totalWeight, player);
            }

            string resultItemId = GetGenericItemId(recipe);
            return Create(resultItemId, mods, totalWeight, player);
        }

        public IItem? CreateItem(string recipeId)
        {
            return null;
        }

        private IEquipItem Create(string itemId, List<WeightedObject<IModifier>> modifiers, float totalWeight, IEntity? player = null)
        {
            try
            {
                var item = (IEquipItem)itemDataProvider.CopyBaseItem(itemId);
                var itemRarity = GetRarity(player);
                item.Rarity = itemRarity;
                var baseStats = item.BaseModifiers;
                int amountModifiers = item.Rarity.ConvertRarityToItemModifierAmount();

                var statModifiers = ModifiersCreator.CreateModifierInstances([.. baseStats.OrderBy(_ => Guid.NewGuid())], item);
                item.SetBaseModifiers(statModifiers);

                HashSet<IModifier> takenMods = WeightedRandomPicker.PickRandomMultipleWithoutDuplicate(modifiers, totalWeight, amountModifiers, rnd);

                List<IModifierInstance> mods = [];
                foreach (var mod in takenMods)
                    mods.Add(ModifiersCreator.CreateModifierInstance(mod.EntityParameter, mod.ModifierType, ApplyPlayerMultiplier(mod.BaseValue, mod.ModifierType, player), item));

                item.SetAdditionalModifiers(mods);
                item.SaveModifiersPool(modifiers.Select(x => x.Obj));

                // TODO : Change to get random effect/ability
                return item;
            }
            catch (ArgumentNullException ex)
            {
                Tracker.TrackException($"Failed to copy base item: {itemId}", ex, this);
                throw new InvalidOperationException($"Cannot create item: base item {itemId} not found", ex);
            }
        }

        private string GetGenericItemId(ICraftingRecipe recipe)
        {
            var attribute = GetAttribute();
            return true switch
            {
                _ when recipe.HasTag("Belt") || recipe.HasTag("Cloak") || recipe.HasTag("Amulet") || recipe.HasTag("Weapon") => recipe.ResultItemId,
                _ => $"{recipe.ResultItemId}_{attribute}_Generic"
            };
        }


        private float ApplyPlayerMultiplier(float baseValue, ModifierType type, IEntity? player = default)
        {
            float multiplier = craftingMastery.GetValueMultiplier();
            if (type == ModifierType.Multiplicative)
                return 1f + (baseValue - 1f) * multiplier;

            return baseValue * multiplier;
        }

        private ISkill? GetRandomSkill()
        {
            //if (_rnd.Randf() <= _craftingMastery.GetFinalSkillChance())
            //    return new PassiveSkillProvider().CreateSkill(GetSkillId(_rnd.RandiRange(1, 4)));
            return null;
        }

        private string GetSkillId(int number) => number switch
        {
            1 => "Passive_Skill_Touch_Of_God",
            2 => "Passive_Skill_Precise_Technique",
            3 => "Passive_Skill_Enhanced_Mastery",
            _ => "Passive_Skill_Master_Apprentice"
        };

        private Rarity GetRarity(IEntity? player = default)
        {
            // if (player == null) return GetRandomValueFallBack<Rarity>();
            // TODO: Later add call to players skill to get Rarity
            return craftingMastery.RollRarity();
        }

        private AttributeType GetAttribute(IEntity? player = default)
        {
            // TODO: Sometime i can get from this call AttributeType.None.
            if (player == null)
            {
                var attribute = AttributeType.None;
                while (attribute == AttributeType.None)
                    attribute = GetRandomValueFallBack<AttributeType>();
                return attribute;
            }

            // TODO: Later add call to players skill to get attribute
            return AttributeType.Dexterity;
        }

        private T GetRandomValueFallBack<T>()
            where T : struct, Enum
        {
            rnd.Randomize();
            var values = Enum.GetValues<T>();
            byte idx = (byte)rnd.RandiRange(0, values.Length - 1);
            return values[idx];
        }
    }
}
