namespace Playground.Script.Passives.Buffs
{
    using Playground.Components;
    using Playground.Script.Passives.Attacks;

    public partial class JustCreatingNewAbility : Ability<AttributeComponent>
    {
        private int _bonusAttribute = 5;

        public JustCreatingNewAbility(AttributeComponent component) : base(component)
        {
        }

        public override void ActivateAbility(AttributeComponent? component) => component!.Dexterity!.Total += _bonusAttribute;
        public override void AfterBuffEnds(AttributeComponent? component) => component.Dexterity.Total -= _bonusAttribute;
        public override void EffectAfterAttack(AttributeComponent? component) => throw new System.NotImplementedException();
    }
}
