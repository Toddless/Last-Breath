namespace Battle.Source.Effects
{
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Entity;
    using Core.Enums;

    /// <summary>
    /// Disables the target's passive skills while present. Suppression lifts only when
    /// the last seal on the target is removed; skills stay learned and reattach automatically.
    /// </summary>
    public class SealOfOblivion(int duration, int maxStacks = 1)
        : Effect(id: "Effect_Seal_Of_Oblivion", duration, maxStacks, StatusEffects.Cursed)
    {
        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied) return; // a rejected stack must not suppress anything

            Target?.PassiveSkills.Suppress();
        }

        public override void Remove()
        {
            var target = Target;
            base.Remove();
            if (target == null) return;

            ResumeIfLastSeal(target);
        }

        public override IEffect Copy() => new SealOfOblivion(Duration, MaxStacks);

        private void ResumeIfLastSeal(IFightable target)
        {
            bool anotherSealExists = target.Effects.GetBy(effect => effect.Id == Id).Any();
            if (!anotherSealExists) target.PassiveSkills.Resume();
        }
    }
}
