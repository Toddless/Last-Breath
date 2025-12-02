namespace Battle.TestData.Abilities.Effects
{
    using Core.Enums;
    using Source.Decorators;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;

    public class ParameterEqualsEffect(
        string id,
        int duration,
        EntityParameter parameter,
        float value,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id, duration, stacks: 1, statusEffect)
    {
        private string _id = string.Empty;
        public EntityParameter Parameter { get; } = parameter;
        public float Value { get; } = value;

        public override void OnApply(EffectApplyingContext context)
        {
            base.OnApply(context);
            var decorator = new ParameterValueEqualsDecorator(Value, Parameter, $"Equals_{Value}");
            _id = decorator.Id;
            context.Target.Parameters.AddModuleDecorator(decorator);
        }

        public override void Remove(IEntity target)
        {
            base.Remove(target);
            target.Parameters.RemoveModuleDecorator(_id, Parameter);
        }

        public override IEffect Clone() => new ParameterEqualsEffect(Id, Duration, Parameter, Value, Status);

        public override bool IsStronger(IEffect otherEffect)
        {
            if (otherEffect is not ParameterEqualsEffect parameter) return false;
            return parameter.Parameter == Parameter && Value.Equals(parameter.Value);
        }
    }
}
