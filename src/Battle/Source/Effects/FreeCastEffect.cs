namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Events;
    using Core.Modifiers.Context;

    /// <summary>
    /// "The next activated ability has no cooldown": puts a cast mutator on the owner and removes
    /// itself after the first non-source ability activation (the mutator ran within its pipeline).
    /// </summary>
    public class FreeCastEffect(string sourceAbilityId)
        : Effect(id: "Effect_Free_Cast", duration: 0, maxStacks: 1)
    {
        private NoCooldownActivationContextModifier? _modifier;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied || Target == null) return;

            _modifier = new NoCooldownActivationContextModifier(sourceAbilityId);
            Target.ModifierHandler.Add(_modifier);
            SubscribeUntilRemoved<AbilityActivatedEvent>(Target.CombatEvents, OnAbilityActivated);
        }

        /// <summary>Waits for its activation, does not decay with turns.</summary>
        public override void TurnEnd()
        {
        }

        public override void Remove()
        {
            if (_modifier != null) Target?.ModifierHandler.Remove(_modifier);
            _modifier = null;
            base.Remove();
        }

        public override IEffect Copy() => new FreeCastEffect(sourceAbilityId);

        private void OnAbilityActivated(AbilityActivatedEvent evt)
        {
            // Published after the cast pipeline ran — the mutator already zeroed this cast's cooldown.
            if (evt.Ability.Id == sourceAbilityId) return;
            Remove();
        }
    }
}
