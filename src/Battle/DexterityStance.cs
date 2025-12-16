namespace Battle
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Modifiers;
    using Source.Abilities.PassiveSkills;

    public class DexterityStance(IEntity owner)
        : StanceBase(owner, effect: new StanceActivationEffect(
                [new ChainAttackPassiveSkill("Chain")],
                [new Modifier(ModifierType.Flat, EntityParameter.Dexterity, 15)]),
            Stance.Dexterity)
    {
    }
}
