namespace Battle.Source.Effects
{
    using System;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Entity.Components.Decorator;
    using Core.Enums;

    /// <summary>
    /// "На грани": the bearer deals <c>bonusPerPercent</c> more damage for every 1% of missing health.
    /// The bonus follows the health bar live while the effect lasts.
    /// </summary>
    public class OnEdgeEffect(int duration, int maxStacks, EffectValue bonusPerPercent = default)
        : Effect(id: "Effect_On_Edge", duration, maxStacks)
    {
        private string DecoratorId => $"{Id}_{InstanceId}";

        /// <summary>Damage gained per 1% of missing health. The bonus is that share TIMES the steps, so the
        /// figure passes through plainly and the one is added once, around the whole product.
        /// The default stands in for the parameterless struct default.</summary>
        private float BonusPerPercent => Effective(bonusPerPercent.Authored == 0f ? 0.03f : bonusPerPercent);

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied || Target == null) return; // a rejected stack must not boost anything

            Target.CurrentHealthChanged += OnHealthChanged;
            RebuildDecorator(Target.CurrentHealth);
        }

        public override void Remove()
        {
            if (Target != null)
            {
                Target.CurrentHealthChanged -= OnHealthChanged;
                Target.Parameters.RemoveModuleDecorator(DecoratorId, EntityParameter.PhysicalDamage);
            }

            base.Remove();
        }

        public override IEffect Copy() => new OnEdgeEffect(Duration, MaxStacks, bonusPerPercent);

        private void OnHealthChanged(float currentHealth) => RebuildDecorator(currentHealth);

        private void RebuildDecorator(float currentHealth)
        {
            if (Target == null) return;
            Target.Parameters.RemoveModuleDecorator(DecoratorId, EntityParameter.PhysicalDamage);

            float percentLost = 1 - (currentHealth / Target.Parameters.MaxHealth);
            int steps = (int)MathF.Floor(percentLost * 100);
            if (steps <= 0) return;

            Target.Parameters.AddModuleDecorator(new EntityParameterDecorator(
                DecoratorId, 1 + (steps * BonusPerPercent), OperationType.Multiply, EntityParameter.PhysicalDamage, Priority.Weak));
        }
    }
}
