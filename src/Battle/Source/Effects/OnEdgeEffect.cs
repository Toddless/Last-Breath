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
    public class OnEdgeEffect(int duration, int maxStacks, float bonusPerPercent = 0.03f)
        : Effect(id: "Effect_On_Edge", duration, maxStacks)
    {
        private string DecoratorId => $"{Id}_{InstanceId}";

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
                DecoratorId, 1 + (steps * bonusPerPercent), OperationType.Multiply, EntityParameter.PhysicalDamage, Priority.Weak));
        }
    }
}
