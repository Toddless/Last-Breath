namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>Luck: while it stands, the bearer's critical rolls are made twice and the better draw counts.</summary>
    public class LuckyCritChanceEffect(
        int duration,
        int maxStacks,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Effect_Lucky_Crit_Chance", duration, maxStacks, statusEffect)
    {
        /// <summary>Whether THIS instance holds the mark. The registry counts sources, so an instance the
        /// stacking rules turned away must not leave one behind, and a second removal must not take another's.</summary>
        private bool _marked;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied || _marked) return;

            _marked = true;
            context.Target.Parameters.AddChanceLuck(EntityParameter.CriticalChance, ChanceLuck.Lucky);
        }

        public override void Remove()
        {
            if (_marked)
            {
                _marked = false;
                Target?.Parameters.RemoveChanceLuck(EntityParameter.CriticalChance, ChanceLuck.Lucky);
            }

            base.Remove();
        }

        public override IEffect Copy() => new LuckyCritChanceEffect(Duration, MaxStacks, Status);
    }
}
