namespace Core.Modifiers.Context
{
    using System;
    using Core.Context;
    using Entity;
    using Enums;

    /// <summary>Outgoing-damage switch: the owner's hits skip the target's elemental resistances, optionally
    /// only from one <see cref="DamageCause"/> ("attacks ignore elemental resistances"). Mitigation reads the
    /// flag, so nothing here touches the numbers.
    /// <para><paramref name="isOn"/> is the switch itself, read at every hit rather than at attach: a source
    /// whose switch can go out from under it (a line that holds only while its condition does) is answered
    /// where every other knob is — when the pipeline runs. Absent, the switch is on for as long as the
    /// modifier is attached, which is what a line that simply exists means.</para></summary>
    public class IgnoreResistancesContextModifier(IFightable owner, DamageCause? cause = null, Func<bool>? isOn = null)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Ignore_Resistances"), IDamageModifier
    {
        public void Apply(IDamageContext context)
        {
            if (!context.Source.IsSame(owner.InstanceId)) return;
            if (cause != null && context.Cause != cause) return;
            if (isOn != null && !isOn()) return;

            context.IgnoreResistances = true;
        }
    }
}
