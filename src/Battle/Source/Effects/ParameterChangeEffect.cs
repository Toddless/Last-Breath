namespace Battle.Source.Effects
{
    using System.Linq;
    using System.Threading.Tasks;
    using Decorators;
    using Core.Enums;
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
            int stacks = Target.Effects.GetBy(effect => effect.Id == Id).Count();
            var decorator = new EntityParameterDecorator($"{Id}_{type}", Value * stacks, type, Parameter, priority);
            Target.Parameters.AddModuleDecorator(decorator);
            _decoratorId = decorator.Id;
        }

        public override void Remove()
        {
            Target?.Parameters.RemoveModuleDecorator(_decoratorId, Parameter);
            int stacks = (Target?.Effects.GetBy(effect => effect.Id == Id).Count() ?? 0) - 1;
            if (stacks > 0) Target?.Parameters.AddModuleDecorator(new EntityParameterDecorator(_decoratorId, Value * stacks, type, Parameter, priority));
            base.Remove();
        }
    }
}
