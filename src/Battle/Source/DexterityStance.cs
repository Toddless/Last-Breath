namespace Battle.Source
{
    using Abilities;
    using Abilities.DarkShroud;
    using Abilities.Effects;
    using Abilities.IncreasingPressure;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Modifiers;
    using PassiveSkills;

    public class DexterityStance(IEntity owner)
        : StanceBase(owner, effect: new StanceActivationEffect([new ChainAttackPassiveSkill()],
            [new Modifier(ModifierValueType.Flat, EntityParameter.Dexterity, 15)]), Stance.Dexterity, [
        ])
    {
    }
}
