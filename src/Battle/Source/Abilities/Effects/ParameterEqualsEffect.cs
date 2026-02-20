namespace Battle.Source.Abilities.Effects
{
    using Battle.Source.Decorators;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components.Decorator;

    public class ParameterEqualsEffect(
        string id,
        int duration,
        int maxStacks,
        EntityParameter parameter,
        float value,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id, duration, maxStacks, statusEffect)
    {
        private ParameterValueEqualsDecorator? _decorator;
        public EntityParameter Parameter { get; } = parameter;
        public float Value { get; } = value;

        public override void Apply(EffectApplyingContext context)
        {
            base.Apply(context);
            _decorator = new ParameterValueEqualsDecorator(Value, Parameter, $"Effect_Parameter_{Parameter}_Equals_{Value}");
            context.Target.Parameters.AddModuleDecorator(_decorator);
        }

        public override void Remove()
        {
            base.Remove();
            if (_decorator != null)
                Owner?.Parameters.RemoveModuleDecorator(_decorator.Id, Parameter);
        }

        public override IEffect Clone() => new ParameterEqualsEffect(Id, Duration, MaxMaxStacks, Parameter, Value, Status);

        public override bool IsStronger(IEffect otherEffect)
        {
            if (otherEffect is not ParameterEqualsEffect parameter) return false;
            return parameter.Parameter == Parameter && Value.Equals(parameter.Value);
        }
    }
}
