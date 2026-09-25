namespace Core.Items.Grants
{
    using System;
    using System.Collections.Generic;
    using Battle.Abilities;
    using Battle.Skills;
    using Enums;
    using Events;
    using Modifiers;

    /// <summary>Providers resolve lazily (Func accessors): a sandbox project without a skill/effect
    /// registry still mints the grant object — display by id works, Attach is an honest no-op
    /// (the strict provider reports the refusal when one exists, null providers stay silent).</summary>
    public class GrantFactory(
        Func<ISkillProvider?> skillProviderAccessor,
        Func<IEffectProvider?> effectProviderAccessor,
        Func<IGameEventBus?> gameEventBusAccessor) : IGrantFactory
    {
        public IItemGrant? Create(GrantKind kind, string id, List<IModifier> modifiers, IReadOnlyDictionary<string, float> properties) => kind switch
        {
            GrantKind.Modifier => CreateModifierGrant(id, ModifiersCreator.CreateModifierInstances(modifiers, id)),
            GrantKind.Passive => CreatePassiveGrant(id, id, properties),
            GrantKind.Effect => CreateEffectGrant(id, id, properties),
            _ => null
        };

        public IItemGrant CreateModifierGrant(string id, IReadOnlyList<IModifierInstance> modifiers) =>
            new ModifierGrant(id, modifiers);

        public IItemGrant CreatePassiveGrant(string id, string skillId, IReadOnlyDictionary<string, float> properties) =>
            new PassiveSkillGrant(id, skillId, properties, skillProviderAccessor);

        public IItemGrant CreateEffectGrant(string id, string effectId, IReadOnlyDictionary<string, float> properties) =>
            new EffectGrant(id, effectId, properties, effectProviderAccessor, gameEventBusAccessor);
    }
}
