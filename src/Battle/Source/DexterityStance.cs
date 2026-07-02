namespace Battle.Source
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Modifiers;
    using PassiveSkills;

    public class DexterityStance(IFightable owner)
        : StanceBase(owner, effect: new StanceActivationEffect([new ChainAttackPassiveSkill()],
            [new Modifier(ModifierValueType.Flat, EntityParameter.Dexterity, 15)]), Stance.Dexterity)
    {
    }
}
