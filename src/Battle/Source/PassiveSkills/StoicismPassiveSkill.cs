namespace Battle.Source.PassiveSkills
{
    using System;
    using Core.Battle;
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Core.Modifiers.Context;
    using Core.Services;

    /// <summary>"Стоицизм": nothing takes the bearer's turn away from him, and in return nothing he does
    /// with his feet takes an attack away from him either.
    /// <para>The immunity is the categorical end of the control scale: an effect carrying hard control is
    /// refused on arrival instead of being shortened.</para>
    /// <para>The price is paid at the evasion roll and not on the parameter: evasion stays the number it
    /// was, so everything counted per point of it keeps paying, and only the verdict of the roll is lost.
    /// Block and resistances are not touched — the bearer still raises a shield and still resists.</para></summary>
    public class StoicismPassiveSkill : Skill
    {
        public const string PassiveId = "Passive_Skill_Stoicism";

        private readonly IIncomingEffectModifier _immunity;

        /// <summary>Which statuses count as hard control. Asked of the shipped rules by default, and named
        /// outright by a composition that carries none of them.</summary>
        private readonly Func<StatusEffects> _hardControl;

        public StoicismPassiveSkill(Func<StatusEffects>? hardControl = null) : base(PassiveId)
        {
            _hardControl = hardControl ?? ShippedHardControl;
            _immunity = new ControlImmunityModifier(_hardControl);
        }

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            owner.ModifierHandler.Add(_immunity);
            owner.Parameters.AddChanceDenial(EntityParameter.Evade);
        }

        /// <summary>Leaves nothing behind: control lands again and evasion is won again the moment the
        /// keystone is refunded or the passive is suppressed.</summary>
        public override void Detach(IFightable owner)
        {
            owner.ModifierHandler.Remove(_immunity);
            owner.Parameters.RemoveChanceDenial(EntityParameter.Evade);
            Owner = null;
        }

        public override ISkill Copy() => new StoicismPassiveSkill(_hardControl);

        /// <summary>Parameterless: the immunity is all-or-nothing and the price is the same for everyone,
        /// so no instance can be stronger than another.</summary>
        public override bool IsStronger(ISkill skill) => false;

        /// <summary>The mask the combat rules declare, or nothing at all where no rules are composed —
        /// what hard control IS belongs to the rules file, and inventing a list here would be a second
        /// answer to a question that already has one.</summary>
        private static StatusEffects ShippedHardControl() =>
            GameServiceProvider.TryGet<ICombatRulesProvider>()?.ControlResistance.HardControlMask
            ?? ControlResistanceRules.Disabled.HardControlMask;
    }
}
