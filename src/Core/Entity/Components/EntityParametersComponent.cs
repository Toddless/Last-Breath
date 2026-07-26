namespace Core.Entity.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Enums;
    using Modifiers;
    using Decorator;
    using Godot;
    using Module;

    public class EntityParametersComponent : IEntityParametersComponent
    {
        /// <summary>Effective-value bounds — the single owner of every parameter cap. Applied in the
        /// indexer, so ALL read channels (named properties, GetValueForParameter, ParameterChanged,
        /// CalculateForBase previews) agree; decorators cannot push a value past its cap (tracker #42).</summary>
        private static readonly Dictionary<EntityParameter, (float Min, float Max)> s_bounds = new()
        {
            [EntityParameter.BlockChance] = (0f, 0.9f),
            [EntityParameter.CriticalChance] = (0f, 1f),
            [EntityParameter.AdditionalHitChance] = (0f, 0.75f),
            [EntityParameter.ArmorPenetration] = (0f, 1f),
            [EntityParameter.Suppress] = (0f, 0.75f),
            [EntityParameter.CriticalDamageMitigation] = (0f, 1f),
            [EntityParameter.LightningResistance] = (0f, 0.8f),
            [EntityParameter.FireResistance] = (0f, 0.8f),
            [EntityParameter.ColdResistance] = (0f, 0.8f),
            [EntityParameter.SuppressChance] = (0f, 1f),
            [EntityParameter.FireResistancePenetration] = (0f, 1f),
            [EntityParameter.ColdResistancePenetration] = (0f, 1f),
            [EntityParameter.LightningResistancePenetration] = (0f, 1f)
        };

        private readonly Dictionary<EntityParameter, (float Base, float Current)> _parameterValues = Enum.GetValues<EntityParameter>().ToDictionary(key => key, key => (0f, 0f));
        private readonly IModuleManager<EntityParameter, IParameterModule<EntityParameter>, EntityParameterModuleDecorator> _moduleManager;
        private float this[EntityParameter type] => ApplyBounds(type, _moduleManager.GetModule(type).GetValue());
        private Func<EntityParameter, IReadOnlyList<IModifier>>? _getModifiersForParameter;

        public float MaxHealth => this[EntityParameter.Health];
        public float HealthRecovery => this[EntityParameter.HealthRecovery];
        public float Damage => this[EntityParameter.PhysicalDamage];
        public float BlockChance => this[EntityParameter.BlockChance];
        public float CriticalDamage => this[EntityParameter.CriticalDamage];
        public float CriticalChance => this[EntityParameter.CriticalChance];
        public float AdditionalHit => this[EntityParameter.AdditionalHitChance];
        public float MulticastChance => this[EntityParameter.MulticastChance];
        public float SpellDamage => this[EntityParameter.SpellDamage];
        public float Accuracy => this[EntityParameter.Accuracy];
        public float Armor => this[EntityParameter.Armor];
        public float ArmorPenetration => this[EntityParameter.ArmorPenetration];
        public float Evade => this[EntityParameter.Evade];
        public float MaxBarrier => this[EntityParameter.Barrier];
        public float Suppress => this[EntityParameter.Suppress];
        public float MaxMana => this[EntityParameter.Mana];
        public float ManaRecovery => this[EntityParameter.ManaRecovery];
        public float MoveSpeed => this[EntityParameter.MoveSpeed];
        public float SuppressChance => this[EntityParameter.SuppressChance];
        public float CriticalDamageMitigation => this[EntityParameter.CriticalDamageMitigation];
        public float LightningResistance => this[EntityParameter.LightningResistance];
        public float FireResistance => this[EntityParameter.FireResistance];
        public float ColdResistance => this[EntityParameter.ColdResistance];

        public event Action<EntityParameter, float>? ParameterChanged;

        public EntityParametersComponent()
        {
            _moduleManager = new ModuleManager<EntityParameter, IParameterModule<EntityParameter>, EntityParameterModuleDecorator>(_parameterValues.ToDictionary(kv => kv.Key,
                IParameterModule<EntityParameter> (kv) => new Module<EntityParameter>(() => _parameterValues[kv.Key].Current, kv.Key)));
        }

        public float GetValueForParameter(EntityParameter parameter) => this[parameter];

        public void Initialize(Func<EntityParameter, IReadOnlyList<IModifier>> getModifiers)
        {
            _getModifiersForParameter = getModifiers;
            _moduleManager.ModuleChanges += RaiseParameterChanges;
        }

        public void AddModuleDecorator(EntityParameterModuleDecorator decorator) => _moduleManager.AddDecorator(decorator);
        public void RemoveModuleDecorator(string id, EntityParameter param) => _moduleManager.RemoveDecorator(id, param);

        public float CalculateForBase(EntityParameter parameter, float baseValue)
        {
            var modifiers = _getModifiersForParameter?.Invoke(parameter) ?? [];
            float value = Calculations.CalculateFloatValue(modifiers, baseValue);
            return ApplyBounds(parameter, _moduleManager.GetModule(parameter).ApplyDecoratorsForValue(value));
        }

        public void SetBaseValueForParameter(EntityParameter parameter, float baseValue)
        {
            if (!_parameterValues.TryGetValue(parameter, out var value)) return;
            value.Base = baseValue;
            value.Current = Calculations.CalculateFloatValue(_getModifiersForParameter?.Invoke(parameter) ?? [], baseValue);
            _parameterValues[parameter] = value;
            RaiseParameterChanges(parameter);
        }

        public void OnParameterModifiersChange(object? sender, IModifiersChangedEventArgs args)
        {
            var parameter = args.EntityParameter;
            if (!_parameterValues.TryGetValue(parameter, out (float Base, float Current) value)) return;
            float newCurrent = Mathf.Max(0, Calculations.CalculateFloatValue(args.Modifiers, value.Base));
            value.Current = newCurrent;
            _parameterValues[parameter] = value;
            RaiseParameterChanges(parameter);
        }

        private void RaiseParameterChanges(EntityParameter args) =>
            ParameterChanged?.Invoke(args, this[args]);

        private static float ApplyBounds(EntityParameter parameter, float value) =>
            s_bounds.TryGetValue(parameter, out (float Min, float Max) bounds) ? Mathf.Clamp(value, bounds.Min, bounds.Max) : value;
    }
}
