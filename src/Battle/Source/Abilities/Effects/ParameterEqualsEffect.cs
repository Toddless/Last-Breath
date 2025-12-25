namespace Battle.Source.Abilities.Effects
{
    using Battle.Source.Decorators;
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class ParameterEqualsEffect(
        string id,
        int duration,
        EntityParameter parameter,
        float value,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id, duration, maxStacks: 1, statusEffect)
    {
        private string _id = string.Empty;
        public EntityParameter Parameter { get; } = parameter;
        public float Value { get; } = value;

        public override void Apply(EffectApplyingContext context)
        {
            base.Apply(context);
            var decorator = new ParameterValueEqualsDecorator(Value, Parameter, $"Equals_{Value}");
            _id = decorator.Id;
            context.Target.Parameters.AddModuleDecorator(decorator);
        }

        public override void Remove()
        {
            base.Remove();
            Owner?.Parameters.RemoveModuleDecorator(_id, Parameter);
        }

        public override IEffect Clone() => new ParameterEqualsEffect(Id, Duration, Parameter, Value, Status);

        public override bool IsStronger(IEffect otherEffect)
        {
            if (otherEffect is not ParameterEqualsEffect parameter) return false;
            return parameter.Parameter == Parameter && Value.Equals(parameter.Value);
        }
    }
}
