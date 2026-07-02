namespace Battle.Source.Effects
{
    using Decorators;
    using Core.Enums;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Interfaces.Abilities;

    public abstract class ParameterChangeEffect(
        string id,
        int duration,
        int maxStacks,
        float value,
        EntityParameter parameter,
        OperationType type,
        Priority priority,
        StatusEffects statusEffect = StatusEffects.None) : Effect(id, duration, maxStacks, statusEffect)
    {
        private string _decoratorId = string.Empty;
        public EntityParameter Parameter { get; } = parameter;
        public float Value { get; } = value;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (Target == null) return;
            _decoratorId = $"{Id}_{type}";
            int stacks = Target.Effects.GetBy(effect => effect.Id == Id).Count();
            RebuildDecorator(stacks);
        }

        public override void Remove()
        {
            // Called before base.Remove(), so the current stack is still counted — hence the -1.
            int stacks = (Target?.Effects.GetBy(effect => effect.Id == Id).Count() ?? 0) - 1;
            RebuildDecorator(stacks);
            base.Remove();
        }

        /// <summary>
        /// Keeps a single decorator per effect type, recalculated for the given stack count:
        /// Add/Subtract scale linearly (Value * stacks), Multiply/Divide exponentially (Value ^ stacks).
        /// </summary>
        private void RebuildDecorator(int stacks)
        {
            if (Target == null) return;
            Target.Parameters.RemoveModuleDecorator(_decoratorId, Parameter);
            if (stacks <= 0) return;

            float stackedValue = type is OperationType.Multiply or OperationType.Divide
                ? MathF.Pow(Value, stacks)
                : Value * stacks;
            Target.Parameters.AddModuleDecorator(new EntityParameterDecorator(_decoratorId, stackedValue, type, Parameter, priority));
        }
    }
}
