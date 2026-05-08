namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using System.Linq;
    using Source.Decorators;
    using System.Threading.Tasks;
    using Core.Interfaces.Abilities;

    public class ParameterBuffEffect(
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
        public float BuffValue { get; } = value;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            int stacks = Target?.Effects.GetBy(x => x.Id == Id).Count() ?? 1;
            var decorator = new EntityParameterDecorator($"{Id}_{type}", BuffValue * stacks, type, Parameter, priority);
            Target?.Parameters.AddModuleDecorator(decorator);
            _decoratorId = decorator.Id;
        }

        public override void Remove()
        {
            Target?.Parameters.RemoveModuleDecorator(_decoratorId, Parameter);
            int stacks = Target?.Effects.GetBy(x => x.Id == Id && x != this).Count() ?? 0;
            if (stacks > 0) Target?.Parameters.AddModuleDecorator(new EntityParameterDecorator(_decoratorId, BuffValue * stacks, type, Parameter, priority));
            base.Remove();
        }

        public override IEffect Copy() => new ParameterBuffEffect(Id, Duration, MaxStacks, BuffValue, Parameter, type, priority, Status);
    }
}
