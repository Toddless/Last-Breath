namespace Core.Items.Grants
{
    using System.Collections.Generic;
    using Enums;
    using Modifiers;

    /// <summary>The single birth point of item grants: data load, crafting rolls and save restore all
    /// mint through this service, so provider wiring (skills, effects) can never drift between
    /// projects. Unknown kinds return null — the caller reports and skips.</summary>
    public interface IGrantFactory
    {
        /// <summary>Data path (blueprints, effect catalogs): the record's id doubles as the skill/effect id.</summary>
        IItemGrant? Create(GrantKind kind, string id, List<IModifier> modifiers, IReadOnlyDictionary<string, float> properties);

        /// <summary>Save path: ready instances (roll provenance included) go in as-is.</summary>
        IItemGrant CreateModifierGrant(string id, IReadOnlyList<IModifierInstance> modifiers);

        IItemGrant CreatePassiveGrant(string id, string skillId, IReadOnlyDictionary<string, float> properties);
        IItemGrant CreateEffectGrant(string id, string effectId, IReadOnlyDictionary<string, float> properties);
    }
}
