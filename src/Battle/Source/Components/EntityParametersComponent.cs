namespace Battle.Source.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle.Source;
    using Battle.Source.Module;
    using Core.Enums;
    using Core.Interfaces.Components;
    using Core.Interfaces.Components.Decorator;
    using Core.Interfaces.Components.Module;
    using Core.Modifiers;
    using Godot;
    using Utilities;

    public class EntityParametersComponent : IEntityParametersComponent
    {
        private readonly Dictionary<EntityParameter, (float Base, float Current)> _parameterValues = Enum.GetValues<EntityParameter>().ToDictionary(key => key, key => (0f, 0f));
        private readonly IModuleManager<EntityParameter, IParameterModule<EntityParameter>, EntityParameterModuleDecorator> _moduleManager;
        private float this[EntityParameter type] => _moduleManager.GetModule(type).GetValue();
        private Func<EntityParameter, IReadOnlyList<IModifier>>? _getModifiersForParameter;

        public float MaxHealth => this[EntityParameter.Health];
        public float HealthRecovery => this[EntityParameter.HealthRecovery];
        public float Damage => this[EntityParameter.Damage];
        public float BlockChance => Mathf.Clamp(this[EntityParameter.BlockChance], 0f, 0.9f);
        public float CriticalDamage => this[EntityParameter.CriticalDamage];
        public float CriticalChance => Mathf.Clamp(this[EntityParameter.CriticalChance], 0f, 1f);
        public float AdditionalHit => Mathf.Clamp(this[EntityParameter.AdditionalHitChance], 0f, 1f);
        public float MulticastChance => Mathf.Clamp(this[EntityParameter.MulticastChance], 0f, 1f);
        public float SpellDamage => this[EntityParameter.SpellDamage];
        public float Accuracy => this[EntityParameter.Accuracy];
        public float Armor => this[EntityParameter.Armor];
        public float ArmorPenetration => Mathf.Clamp(this[EntityParameter.ArmorPenetration], 0f, 1f);
        public float Evade => this[EntityParameter.Evade];
        public float MaxBarrier => this[EntityParameter.Barrier];
        public float Suppress => Mathf.Clamp(this[EntityParameter.Suppress], 0f, 1f);
        public float MaxMana => this[EntityParameter.Mana];
        public float ManaRecovery => this[EntityParameter.ManaRecovery];
        public float MoveSpeed => this[EntityParameter.MoveSpeed];


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
            return _moduleManager.GetModule(parameter).ApplyDecoratorsForValue(value);
        }

        public void SetBaseValueForParameter(EntityParameter parameter, float baseValue)
        {
            if (!_parameterValues.TryGetValue(parameter, out var value)) return;
            value.Base = baseValue;
            value.Current = Calculations.CalculateFloatValue(_getModifiersForParameter?.Invoke(parameter) ?? [], baseValue);
            _parameterValues[parameter] = value;
            RaiseParameterChanges(parameter);
        }

        public void OnModifiersChange(object? sender, IModifiersChangedEventArgs args)
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
    }
}
