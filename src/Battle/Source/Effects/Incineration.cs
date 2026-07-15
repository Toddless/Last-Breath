namespace Battle.Source.Effects
{
    using Core.Enums;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Events;

    /// <summary>Righteous Wrath finisher: while incinerated, a BURNING target dies to any critical hit.</summary>
    public class Incineration(int duration)
        : Effect(id: "Effect_Incineration", duration, maxStacks: 1, statusEffect: StatusEffects.Cursed)
    {
        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied || Target == null) return;
            SubscribeUntilRemoved<DamageTakenEvent>(Target.CombatEvents, OnDamageTaken);
        }

        public override IEffect Copy() => new Incineration(Duration);

        private void OnDamageTaken(DamageTakenEvent obj)
        {
            if (Target is not { IsAlive: true } target || !target.IsSame(obj.Target.InstanceId)) return;
            if (!obj.Context.IsCrit) return;
            // The design kills the BURNING target: if the burning ran out before the crit, nothing happens.
            if ((target.StatusEffects & StatusEffects.Burning) == 0) return;
            target.Kill();
        }
    }
}
