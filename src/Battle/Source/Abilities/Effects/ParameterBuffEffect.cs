namespace Battle.Source.Abilities.Effects
{
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Modifiers;
    using Core.Interfaces.Abilities;

    public class ParameterBuffEffect : Effect
    {
        private IModifierInstance _modifier;
        private readonly ModifierValueType _type;

        public ParameterBuffEffect(string id, int duration, int maxStacks, float amount, EntityParameter parameter, ModifierValueType type,
            StatusEffects statusEffect = StatusEffects.None) : base(id, duration, maxStacks, statusEffect)
        {
            Parameter = parameter;
            BuffAmount = amount;
            _type = type;
            _modifier = new SimpleModifier(parameter, type, amount, InstanceId);
        }

        public EntityParameter Parameter { get; }

        public float BuffAmount { get; }

        public override async Task Apply(EffectApplyingContext context)
        {
            if(AppliedTo == null) return;
            await base.Apply(context);
            _modifier.Apply(AppliedTo);
        }

        public override IEffect Clone() => new ParameterBuffEffect(Id, Duration, MaxStacks, BuffAmount, Parameter, _type, Status);
    }
}
