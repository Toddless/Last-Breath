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
    using Crafting.TestResources;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Crafting;
    using Crafting.TestResources.DI;
    using System.Collections.Generic;

    public class ItemCreator : IItemCreator
    {
        private readonly RandomNumberGenerator _rnd;
        private readonly ItemDataProvider _dataProvider;

        public ItemCreator()
        {
            _rnd = new RandomNumberGenerator();
            _rnd.Randomize();
            _dataProvider = ServiceProvider.Instance.GetService<ItemDataProvider>();
        }

        public IEquipItem? CreateEquipItem(string resultItemId, IEnumerable<IMaterialModifier> resources, ICharacter? player = default)
        {
            var (Mods, TotalWeight) = WeightedRandomPicker.CalculateWeights(resources);
            return Create(resultItemId, Mods, TotalWeight, player);
        }

        public IEquipItem? CreateGenericItem(string resultItemId, IEnumerable<IMaterialModifier> resouces, ICharacter? player = default)
        {
            var rarity = GetRarity(player);
            var attribute = GetAttribute(player);
            var itemId = $"{resultItemId}_{attribute}_{rarity}";
            var (Mods, TotalWeight) = WeightedRandomPicker.CalculateWeights(resouces);
            return Create(itemId, Mods, TotalWeight, player);
        }

        public IItem? CreateItem(string resultItemId)
        {
            return null;
        }

        private IEquipItem? Create(string itemId, List<WeightedObject<IMaterialModifier>> modifiers, float totalWeight, ICharacter? player = default)
        {
            var item = (IEquipItem?)_dataProvider.CopyBaseItem(itemId);

            if (item == null)
            {
                Tracker.TrackNotFound($"Item with id: {itemId}", this);
                return null;
            }

            var baseStats = _dataProvider.GetItemBaseStats(itemId);
            var statModifiers = ModifiersCreator.CreateModifierInstances(baseStats, item).ToHashSet();
            item.SetBaseModifiers(statModifiers);

            var amountModifiers = GetAmountModifiers(item.Rarity);

            HashSet<IMaterialModifier> takenMods = WeightedRandomPicker.PickRandomMultipleWithoutDublicate(modifiers, totalWeight, amountModifiers, _rnd);

            List<IModifierInstance> mods = [];
            foreach (var mod in takenMods)
                mods.Add(ModifiersCreator.CreateModifierInstance(mod.Parameter, mod.ModifierType, ApplyPlayerMultiplier(mod.BaseValue, player), item));

            item.SetAdditionalModifiers(mods);
            item.SaveModifiersPool(modifiers.Select(x => x.Obj));

            // TODO : Change to get random effect/ability
            var skill = GetRandomSkill();
            if (skill != null) item.SetSkill(skill);
            EventBus.RaiseItemCreated(item);
            return item;
        }

        private float ApplyPlayerMultiplier(float baseValue, ICharacter? player = default)
        {
            if (player != null)
            {

            }

            return baseValue * _rnd.RandfRange(0.95f, 1.2f);
        }

        // TODO: An item can have skill or effect? Or both?
        private ISkill? GetRandomSkill(ICharacter? player = default)
        {
            // TODO: Later i need to find way to inject SkillProvider to get some skill
            // TODO: Random skill not guaranteed.
            // Again: we take probabillity from player
            var skillProvider = new PassiveSkillProvider();

            var rNumb = _rnd.RandiRange(1, 2);

            return skillProvider.CreateSkill(rNumb == 1 ? "Passive_Skill_Precise_Technique" : "Passive_Skill_Touch_Of_God");
        }

        private int GetAmountModifiers(Rarity rarity) => (int)rarity + 1;

        private Rarity GetRarity(ICharacter? player = default)
        {
            if (player == null) return GetRandomValueFallBack<Rarity>();
            // TODO: Later add call to players skill to get Rarity
            return Rarity.Rare;
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
