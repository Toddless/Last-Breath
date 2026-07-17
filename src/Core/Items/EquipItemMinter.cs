namespace Core.Items
{
    using System;
    using System.Collections.Generic;
    using Data;
    using Entity.Components;
    using Enums;
    using Modifiers;

    public interface IEquipItemMinter
    {
        /// <summary>Mints a fresh equip item from its blueprint: rolls every ranged value (level and
        /// lines) and materializes the authored descriptors. Throws when the id has no blueprint.</summary>
        IEquipItem Mint(string id);
    }

    /// <summary>The single birth point of equip items (save restore aside — that path replays stored
    /// results and never rolls). One copy in Core: every project mints through this class, so the roll
    /// rules can never drift between loot, crafting and quest rewards.</summary>
    public sealed class EquipItemMinter(
        IEquipBlueprintProvider blueprints,
        IItemGameDataFactory factory,
        IModifierMaterializer materializer,
        IRandomNumberGenerator rnd) : IEquipItemMinter
    {
        public IEquipItem Mint(string id)
        {
            var blueprint = blueprints.GetBlueprint(id);
            if (blueprint == null)
            {
                Tracker.TrackNotFound($"Equip blueprint with id: {id}", this);
                throw new ArgumentNullException($"Equip blueprint not found: {id}");
            }

            var item = CreateItem(blueprint);
            item.Rarity = blueprint.Rarity;
            ApplyUpdateLevel(blueprint, item);

            var implicits = Materialize(blueprint.Implicits, item.InstanceId);
            item.SetImplicits(implicits.Entities);
            item.SetContextImplicits(implicits.Contexts);

            var modifiers = Materialize(blueprint.Modifiers, item.InstanceId);
            item.SetModifiers(modifiers.Entities);
            item.SetContextModifiers(modifiers.Contexts);

            AddGrants(blueprint, item);
            return item;
        }

        private IEquipItem CreateItem(EquipItemBlueprint blueprint) =>
            blueprint.Weapon is { } weapon
                ? factory.CreateWeaponItem(weapon.WeaponType, weapon.Handedness, weapon.Damage,
                    weapon.CriticalChance, weapon.CriticalDamage, blueprint.Id, blueprint.Tags)
                : factory.CreateEquipItem(blueprint.Piece, blueprint.Id, blueprint.Tags);

        /// <summary>A ranged level on a Mythic is its whole progression: the roll IS the item's final
        /// level (UpdateLevel == MaxUpdateLevel, further sharpening impossible by cap — owner's call).
        /// Everything else starts at the fixed/rolled level and sharpens up to the data cap as usual.
        /// The level rolls BEFORE any line value, so seeded sequences are stable.</summary>
        private void ApplyUpdateLevel(EquipItemBlueprint blueprint, IEquipItem item)
        {
            var spec = blueprint.UpdateLevel;
            if (!spec.IsFixed && blueprint.Rarity == Rarity.Mythic)
            {
                int rolled = rnd.RandIntRange(spec.Min, spec.Max);
                item.MaxUpdateLevel = rolled;
                if (rolled > 0) item.Upgrade(rolled);
                return;
            }

            item.MaxUpdateLevel = blueprint.MaxUpdateLevel;
            int level = spec.IsFixed ? spec.Min : rnd.RandIntRange(spec.Min, spec.Max);
            if (level > 0) item.Upgrade(level);
        }

        private CollectingSink Materialize(IReadOnlyList<IModifierDescriptor> descriptors, string source)
        {
            var sink = new CollectingSink();
            foreach (var descriptor in descriptors)
                materializer.Materialize(descriptor, sink, source);

            return sink;
        }

        private void AddGrants(EquipItemBlueprint blueprint, IEquipItem item)
        {
            foreach (var grant in blueprint.Grants)
            {
                var created = factory.CreateGrant(grant.Kind, grant.Id, grant.Modifiers, grant.Properties);
                if (created != null) item.AddGrant(created);
            }
        }
    }
}
