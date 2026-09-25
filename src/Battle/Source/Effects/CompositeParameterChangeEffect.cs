namespace Battle.Source.Effects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Entity.Components.Decorator;
    using Core.Enums;
    using Core.Localization;

    /// <summary>One parameter change of a composite effect; the figure is authored, the shape says how
    /// it becomes the change.</summary>
    public record ParameterChange(
        EntityParameter Parameter,
        EffectValue Value,
        OperationType Type,
        Priority Priority,
        EffectValueShape Shape = EffectValueShape.Plain);

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

        /// <summary>What one change is actually worth on this instance — authored figure through the
        /// cast^s effectiveness, in the shape the change declares.</summary>
        public float ValueOf(ParameterChange change) => Effective(change.Value, change.Shape);

        /// <summary>{Changes} — the whole list as one display string: "+300 Health, +10% Health Recovery".</summary>
        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                values["Changes"] = string.Join(", ", Changes.Select(change =>
                    $"{Localization.FormatParameterChange(change.Parameter, ValueOf(change), change.Type, TextFormat.Rich)} {Localization.Localize(change.Parameter.ToString())}"));
                return values;
            }
        }

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

                float value = ValueOf(change);
                float stackedValue = change.Type is OperationType.Multiply or OperationType.Divide
                    ? MathF.Pow(value, stacks)
                    : value * stacks;
                Target.Parameters.AddModuleDecorator(new EntityParameterDecorator(decoratorId, stackedValue, change.Type, change.Parameter, change.Priority));
            }
        }
    }
}
