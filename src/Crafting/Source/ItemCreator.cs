namespace Crafting.Source
{
    using Godot;
    using System;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Core.Modifiers;
    using Core.Interfaces;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;

    public class ItemCreator : IItemCreator
    {
        private readonly RandomNumberGenerator _rnd;
        private readonly IItemDataProvider _dataProvider;
        private readonly ICraftingMastery _craftingMastery;

        public ItemCreator(ICraftingMastery craftingMastery, RandomNumberGenerator rnd, IItemDataProvider itemDataProvider)
        {
            _rnd = rnd;
            _dataProvider = itemDataProvider;
            _craftingMastery = craftingMastery;
        }

        public IEquipItem CreateEquipItem(string recipeId, IEnumerable<IMaterialModifier> resources, ICharacter? player = default)
        {
            var recipe = _dataProvider.GetRecipe(recipeId);
            var (Mods, TotalWeight) = WeightedRandomPicker.CalculateWeights(resources);
            if (recipe.HasTag("Generic"))
            {
                var resultItemId = GetGenericItemId(recipe);
                return Create(resultItemId, Mods, TotalWeight, player);
            }
            else
                return Create(recipe.ResultItemId, Mods, TotalWeight, player);
        }

        public IItem? CreateItem(string recipeId)
        {
            return null;
        }

        private IEquipItem Create(string itemId, List<WeightedObject<IMaterialModifier>> modifiers, float totalWeight, ICharacter? player = default)
        {
            try
            {
                var item = (IEquipItem)_dataProvider.CopyBaseItem(itemId);
                var itemRarity = GetRarity(player);
                item.Rarity = itemRarity;
                var baseStats = item.BaseModifiers;
                var amountModifiers = GetAmountModifiers(item.Rarity);

                var statModifiers = ModifiersCreator.CreateModifierInstances([.. baseStats.OrderBy(_ => Guid.NewGuid()).Take(amountModifiers)], item).ToHashSet();
                item.SetBaseModifiers(statModifiers);

                HashSet<IMaterialModifier> takenMods = WeightedRandomPicker.PickRandomMultipleWithoutDublicate(modifiers, totalWeight, amountModifiers, _rnd);

                List<IModifierInstance> mods = [];
                foreach (var mod in takenMods)
                    mods.Add(ModifiersCreator.CreateModifierInstance(mod.Parameter, mod.ModifierType, ApplyPlayerMultiplier(mod.BaseValue, mod.ModifierType, player), item));

                item.SetAdditionalModifiers(mods);
                item.SaveModifiersPool(modifiers.Select(x => x.Obj));

                // TODO : Change to get random effect/ability
                var skill = GetRandomSkill();
                if (skill != null) item.SetSkill(skill);
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
                var _ when recipe.HasTag("Belt") || recipe.HasTag("Cloak") || recipe.HasTag("Amulet") || recipe.HasTag("Weapong") => recipe.ResultItemId,
                _ => $"{recipe.ResultItemId}_{attribute}_Generic"
            };
        }


        private float ApplyPlayerMultiplier(float baseValue, ModifierType type, ICharacter? player = default)
        {
            float multiplier = _craftingMastery.GetFinalValueMultiplier();
            if (type == ModifierType.Multiplicative)
                return 1f + (baseValue - 1f) * multiplier;
            else
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

        private int GetAmountModifiers(Rarity rarity) => (int)rarity + 1;

        private Rarity GetRarity(ICharacter? player = default)
        {
            // if (player == null) return GetRandomValueFallBack<Rarity>();
            // TODO: Later add call to players skill to get Rarity
            return _craftingMastery.RollRarity();
        }

        private AttributeType GetAttribute(ICharacter? player = default)
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
            _rnd.Randomize();
            var values = Enum.GetValues<T>();
            var idx = (byte)_rnd.RandiRange(0, values.Length - 1);
            return values[idx];
        }
    }
}
