namespace Battle.Source.Effects
{
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Events;

    /// <summary>
    /// While it stands, every critical hit its bearer scores gives ONE more turn to every buff the bearer
    /// carries. The generalisation of what the Crit Calculation buff already did for itself: there a crit
    /// extended one effect, here it extends the whole standing arrangement.
    /// <para>The loop is held by the same budget every extension travels through — each instance carries
    /// its own, so a fight of crits runs the buffs into their budgets instead of holding them up forever.
    /// This effect is a buff and extends itself along with the rest, exactly as its predecessor did.</para>
    /// </summary>
    public class MythicCalculationEffect(int duration, int maxStacks)
        : Effect(id: "Effect_Mythic_Calculation", duration, maxStacks)
    {
        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (Target == null) return;

            SubscribeUntilRemoved<AfterAttackEvent>(Target.CombatEvents, OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evt)
        {
            if (!evt.Context.IsCritical || Target == null) return;

            // Read to a list first: an extension may remove an effect that ran out of budget, and the
            // bearer's list is the collection being walked.
            foreach (IEffect standing in Target.Effects.GetBy(effect => !effect.IsHarmful).ToList())
                standing.Extend(1);
        }

        public override IEffect Copy() => new MythicCalculationEffect(Duration, MaxStacks);
    }
}
