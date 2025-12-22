namespace Battle
{
    using Core.Enums;
    using Core.Modifiers;
    using Source.PassiveSkills;
    using Core.Interfaces.Entity;

    public class DexterityStance(IEntity owner)
        : StanceBase(owner, effect: new StanceActivationEffect(
            [new ChainAttackPassiveSkill()],
            [new Modifier(ModifierType.Flat, EntityParameter.Dexterity, 15)]), Stance.Dexterity)
    {
    }
}
