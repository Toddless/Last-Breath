namespace Battle.Source.Effects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Decorators;

    /// <summary>One parameter change of a composite effect.</summary>
    public record ParameterChange(EntityParameter Parameter, float Value, OperationType Type, Priority Priority);

    /// <summary>
    /// Effect that changes SEVERAL entity parameters at once (e.g. Ares' Blessing: health + health
    /// recovery). Stacking semantics match <see cref="ParameterChangeEffect"/>, applied per change:
    /// Add/Subtract scale linearly with stacks, Multiply/Divide exponentially.
    /// </summary>
    public abstract class CompositeParameterChangeEffect(
        string id,
        int duration,
        int maxStacks,
        IReadOnlyList<ParameterChange> changes,
        StatusEffects statusEffect = StatusEffects.None) : Effect(id, duration, maxStacks, statusEffect)
    {
        public IReadOnlyList<ParameterChange> Changes { get; } = changes;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (Target == null) return;
            int stacks = Target.Effects.GetBy(effect => effect.Id == Id).Count();
            RebuildDecorators(stacks);
        }

        public override void Remove()
        {
            // Called before base.Remove(), so the current stack is still counted — hence the -1.
            int stacks = (Target?.Effects.GetBy(effect => effect.Id == Id).Count() ?? 0) - 1;
            RebuildDecorators(stacks);
            base.Remove();
        }

        /// <summary>One decorator per changed parameter, recalculated for the given stack count.</summary>
        private void RebuildDecorators(int stacks)
        {
            if (Target == null) return;
            foreach (var change in Changes)
            {
                string decoratorId = $"{Id}_{change.Parameter}_{change.Type}";
                Target.Parameters.RemoveModuleDecorator(decoratorId, change.Parameter);
                if (stacks <= 0) continue;

                float stackedValue = change.Type is OperationType.Multiply or OperationType.Divide
                    ? MathF.Pow(change.Value, stacks)
                    : change.Value * stacks;
                Target.Parameters.AddModuleDecorator(new EntityParameterDecorator(decoratorId, stackedValue, change.Type, change.Parameter, change.Priority));
            }
        }
    }
}
