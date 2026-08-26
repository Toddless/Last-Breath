namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Core.Battle.Abilities;
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Moq;

    /// <summary>A fighter with the parts a keystone reaches: the roster it registers on, the modifier list
    /// its lines land in, the parameters they resolve through and the pipelines it hangs mutators on.
    /// Shared by the keystone walks — every one of them needs the same fighter and none of them needs a
    /// different one.</summary>
    internal sealed class KeystoneCarrier
    {
        public KeystoneCarrier()
        {
            var mock = new Mock<IPlayer>();
            Fighter = mock.Object;
            Skills = new PassiveSkillsComponent(Fighter);
            Parameters.Initialize(Modifiers.GetModifiers);
            Modifiers.ModifiersChanged += Parameters.OnParameterModifiersChange;

            mock.SetupGet(fighter => fighter.PassiveSkills).Returns(Skills);
            mock.SetupGet(fighter => fighter.ParameterModifiers).Returns(Modifiers);
            mock.SetupGet(fighter => fighter.Parameters).Returns(Parameters);
            mock.SetupGet(fighter => fighter.ModifierHandler).Returns(Handler);
            mock.SetupGet(fighter => fighter.CombatEvents).Returns(new CombatEventBus());
        }

        public IPlayer Fighter { get; }

        public IPassiveSkillsComponent Skills { get; }

        public IParameterModifiersComponent Modifiers { get; } = new ParameterModifiersComponent();

        public IEntityParametersComponent Parameters { get; } = new EntityParametersComponent();

        public IModifierHandlerComponent Handler { get; } = new ModifierHandlerComponent();

        public float Value(EntityParameter parameter) => Parameters.GetValueForParameter(parameter);

        public void Set(EntityParameter parameter, float value) => Parameters.SetBaseValueForParameter(parameter, value);

        public void Wear(ISkill skill) => Skills.AddSkill(skill);

        public void TakeOff(ISkill skill) => Skills.RemoveSkill(skill);

        /// <summary>One effect offered to the fighter's own incoming pipeline — the pass that decides whether
        /// what was laid on him lands at all.</summary>
        public IIncomingEffectContext Incoming(StatusEffects status)
        {
            var effect = new Mock<IEffect>();
            effect.SetupGet(landing => landing.Status).Returns(status);
            effect.SetupProperty(landing => landing.Duration, 3);

            var context = new IncomingEffectContext(Fighter, Fighter, effect.Object);
            Handler.Apply(context);

            return context;
        }
    }
}
