namespace Battle.Components
{
    using Godot;
    using System;
    using Source;
    using Utilities;
    using Core.Enums;
    using Source.Module;
    using System.Collections.Generic;
    using Core.Interfaces.Components;
    using Core.Interfaces.Battle.Module;
    using Core.Interfaces.Battle.Decorator;

    public class EntityParametersComponent : IEntityParametersComponent
    {
        private readonly Dictionary<EntityParameter, (float Base, float Current)> _parameters;
        private readonly IModuleManager<EntityParameter, IParameterModule<EntityParameter>, StatModuleDecorator> _moduleManager;
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
        public float MaxReduceDamage => this[EntityParameter.MaxReduceDamage];
        public float MaxEvadeChance => this[EntityParameter.MaxEvadeChance];

        public event Action<float>? CurrentBarrierChanged;
        public event Action<float>? CurrentHealthChanged;
        public event Action<EntityParameter, float>? ParameterChanged;


        public EntityParametersComponent()
        {
            _parameters = new()
            {
                [EntityParameter.Armor] = (100f, 100f),
                [EntityParameter.Evade] = (100f, 100f),
                [EntityParameter.Barrier] = (10f, 10f),
                [EntityParameter.Suppress] =  (0f, 0f),
                [EntityParameter.MaxEvadeChance] = (0f, 0f),
                [EntityParameter.MaxReduceDamage] = (0f, 0f),
                [EntityParameter.Damage] = (50f, 50f),
                [EntityParameter.CriticalDamage] = (0.05f, 0.05f),
                [EntityParameter.CriticalChance] = (1f, 1f),
                [EntityParameter.SpellDamage] = (0f, 0f),
                [EntityParameter.AdditionalHitChance] = (0.05f, 0.05f),
                [EntityParameter.Health] = (100f, 100f),
                [EntityParameter.HealthRecovery] = (1f, 1f),
                [EntityParameter.Resource] = (50f, 50f),
                [EntityParameter.ResourceRecovery] = (5f, 5f),

            };

            _moduleManager = new ModuleManager<EntityParameter, IParameterModule<EntityParameter>, StatModuleDecorator>(new()
            {
                [EntityParameter.Armor] = new Module<EntityParameter>(() => _parameters[EntityParameter.Armor].Current, EntityParameter.Armor),
                [EntityParameter.Evade] = new Module<EntityParameter>(() => _parameters[EntityParameter.Evade].Current, EntityParameter.Evade),
                [EntityParameter.Barrier] = new Module<EntityParameter>(() => _parameters[EntityParameter.Barrier].Current, EntityParameter.Barrier),
                [EntityParameter.Suppress]= new Module<EntityParameter>(() => _parameters[EntityParameter.Suppress].Current, EntityParameter.Suppress),
                [EntityParameter.MaxReduceDamage] =
                    new Module<EntityParameter>(() => Mathf.Min(0.9f, _parameters[EntityParameter.MaxReduceDamage].Current), EntityParameter.MaxReduceDamage),
                [EntityParameter.MaxEvadeChance] = new Module<EntityParameter>(() => _parameters[EntityParameter.MaxEvadeChance].Current, EntityParameter.MaxEvadeChance),
                [EntityParameter.Damage] = new Module<EntityParameter>(() => _parameters[EntityParameter.Damage].Current, EntityParameter.Damage),
                [EntityParameter.CriticalChance] =
                    new Module<EntityParameter>(() => Mathf.Min(1f, _parameters[EntityParameter.CriticalChance].Current), EntityParameter.CriticalChance),
                [EntityParameter.CriticalDamage] = new Module<EntityParameter>(() => _parameters[EntityParameter.CriticalDamage].Current, EntityParameter.CriticalDamage),
                [EntityParameter.AdditionalHitChance] =
                    new Module<EntityParameter>(() => Mathf.Min(1f, _parameters[EntityParameter.AdditionalHitChance].Current), EntityParameter.AdditionalHitChance),
                [EntityParameter.SpellDamage] = new Module<EntityParameter>(() => _parameters[EntityParameter.SpellDamage].Current, EntityParameter.SpellDamage),
                [EntityParameter.Health] = new Module<EntityParameter>(() => _parameters[EntityParameter.Health].Current, EntityParameter.Health),
                [EntityParameter.HealthRecovery] = new Module<EntityParameter>(() => _parameters[EntityParameter.HealthRecovery].Current, EntityParameter.HealthRecovery),
                [EntityParameter.Resource] = new Module<EntityParameter>(() => _parameters[EntityParameter.Resource].Current, EntityParameter.Resource),
                [EntityParameter.ResourceRecovery] = new Module<EntityParameter>(() => _parameters[EntityParameter.ResourceRecovery].Current, EntityParameter.ResourceRecovery),
            });

            CurrentHealth = MaxHealth;
        }

        public void AddModuleDecorator(StatModuleDecorator decorator) => _moduleManager.AddDecorator(decorator);
        public void RemoveModuleDecorator(string id, EntityParameter param) => _moduleManager.RemoveDecorator(id, param);
        public float CalculateForBase(EntityParameter parameter, float baseValue) => _moduleManager.GetModule(parameter).ApplyDecorators(baseValue);

        public void OnParameterChanges(object? sender, IModifiersChangedEventArgs args)
        {
            var parameter = args.EntityParameter;
            if (!_parameters.TryGetValue(parameter, out (float Base, float Current) values)) return;
            float newCurrent = Mathf.Max(0, Calculations.CalculateFloatValue(values.Base, args.Modifiers));
            values.Current = newCurrent;
            _parameters[parameter] = values;
            RaiseParameterChanges(parameter);
        }

        private void RaiseParameterChanges(EntityParameter args) =>
            ParameterChanged?.Invoke(args, this[args]);
    }
}
