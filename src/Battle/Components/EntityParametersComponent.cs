namespace Battle.Components
{
    using Godot;
    using System;
    using Source;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Source.Module;
    using System.Collections.Generic;
    using Core.Interfaces.Components;
    using Core.Interfaces.Battle.Module;
    using Core.Interfaces.Battle.Decorator;

    public class EntityParametersComponent : IEntityParametersComponent
    {
        private readonly Dictionary<EntityParameter, float> _parameters = Enum.GetValues<EntityParameter>().ToDictionary(key => key, key => 0f);
        private readonly IModuleManager<EntityParameter, IParameterModule<EntityParameter>, EntityParameterModuleDecorator> _moduleManager;
        private float this[EntityParameter type] => _moduleManager.GetModule(type).GetValue();

        public float CurrentHealth
        {
            get => Mathf.Max(0, field);
            set
            {
                float clamped = Mathf.Max(0, Mathf.Min(value, MaxHealth));
                if (Mathf.Abs(clamped - field) < float.Epsilon) return;
                field = clamped;
                CurrentHealthChanged?.Invoke(field);
            }
        }

        public float CurrentBarrier
        {
            get => Mathf.Max(0, field);
            set
            {
                float clamped = Mathf.Max(0, Mathf.Min(value, MaxBarrier));
                if (Mathf.Abs(clamped - field) < float.Epsilon) return;
                field = clamped;
                CurrentBarrierChanged?.Invoke(field);
            }
        }

        public float MaxHealth => this[EntityParameter.Health];
        public float HealthRecovery => this[EntityParameter.HealthRecovery];
        public float Damage => this[EntityParameter.Damage];
        public float CriticalDamage => this[EntityParameter.CriticalDamage];
        public float CriticalChance => this[EntityParameter.CriticalChance];
        public float AdditionalHit => this[EntityParameter.AdditionalHitChance];
        public float SpellDamage => this[EntityParameter.SpellDamage];
        public float Armor => this[EntityParameter.Armor];
        public float Evade => this[EntityParameter.Evade];
        public float MaxBarrier => this[EntityParameter.Barrier];

        public event Action<float>? CurrentBarrierChanged, CurrentHealthChanged;
        public event Action<EntityParameter, float>? ParameterChanged;


        public EntityParametersComponent()
        {
            _moduleManager = new ModuleManager<EntityParameter, IParameterModule<EntityParameter>, EntityParameterModuleDecorator>(_parameters.ToDictionary(kv => kv.Key,
                IParameterModule<EntityParameter> (kv) => new Module<EntityParameter>(() => _parameters[kv.Key], kv.Key)));
        }

        public void Initialize()
        {
            CurrentHealth = MaxHealth;
            _moduleManager.ModuleChanges += RaiseParameterChanges;
            foreach (EntityParameter parameter in Enum.GetValues<EntityParameter>())
                RaiseParameterChanges(parameter);
        }

        public void AddModuleDecorator(EntityParameterModuleDecorator decorator) => _moduleManager.AddDecorator(decorator);
        public void RemoveModuleDecorator(string id, EntityParameter param) => _moduleManager.RemoveDecorator(id, param);
        public float CalculateForBase(EntityParameter parameter, float baseValue) => _moduleManager.GetModule(parameter).ApplyDecoratorsForValue(baseValue);

        public void OnModifiersChange(object? sender, IModifiersChangedEventArgs args)
        {
            var parameter = args.EntityParameter;
            if (!_parameters.ContainsKey(parameter)) return;
            float newCurrent = Mathf.Max(0, Calculations.CalculateFloatValue(args.Modifiers));
            _parameters[parameter] = newCurrent;
            RaiseParameterChanges(parameter);
        }

        private void RaiseParameterChanges(EntityParameter args) =>
            ParameterChanged?.Invoke(args, this[args]);
    }
}
