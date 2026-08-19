namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Battle.Skills;

    /// <summary>
    /// Wraps a passive skill into a timed effect: the skill's mechanics run only while the effect
    /// lasts (e.g. the Porcupine augment attaches the Echo passive for the buff's duration).
    /// </summary>
    public class TemporarySkillEffect(string id, int duration, ISkill skill) : Effect(id, duration, maxStacks: 1)
    {
        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied || Target == null) return;
            skill.Attach(Target);
        }

        public override void Remove()
        {
            if (Target != null) skill.Detach(Target);
            base.Remove();
        }

        public override IEffect Copy() => new TemporarySkillEffect(Id, Duration, skill.Copy());
    }
}
