namespace Battle.Source.Effects
{
    using Core.Enums;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Events.GameEvents;

    public class Incineration(int duration)
        : Effect(id: "Effect_Incineration", duration, maxStacks: 1, statusEffect: StatusEffects.Cursed)
    {
        public int Duration { get; } = duration;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied || Target == null) return;
            SubscribeUntilRemoved<DamageTakenEvent>(Target.CombatEvents, OnDamageTaken);
        }

        public override IEffect Copy() => new Incineration(Duration);

        private void OnDamageTaken(DamageTakenEvent obj)
        {
            if (!obj.Context.IsCrit || Target?.InstanceId.Equals(obj.Target.InstanceId) == false) return;
            Target?.Kill();
        }
    }
}
