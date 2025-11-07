namespace Battle.Components
{
    using Godot;
    using System;
    using Source;
    using Core.Enums;
    using Source.Module.StatModules;
    using Core.Interfaces.Components;
    using Core.Interfaces.Battle.Module;
    using Core.Interfaces.Battle.Decorator;

    internal class DefenseComponent : Component, IDefenceComponent
    {
        private float _currentBarrier;
        public event Action<float>? CurrentBarrierChanged;

        public float Armor => this[Parameter.Armor].GetValue();
        public float Evade => this[Parameter.Evade].GetValue();
        public float MaxBarrier => this[Parameter.Barrier].GetValue();
        public float MaxReduceDamage => this[Parameter.MaxReduceDamage].GetValue();
        public float MaxEvadeChance => this[Parameter.MaxEvadeChance].GetValue();

        public float CurrentBarrier
        {
            get => Mathf.Max(0, _currentBarrier);
            private set
            {
                float clamped = Mathf.Max(0, Mathf.Min(value, MaxBarrier));
                if (Mathf.Abs(clamped - _currentBarrier) < float.Epsilon) return;
                _currentBarrier = clamped;
                CurrentBarrierChanged?.Invoke(_currentBarrier);
            }
        }

        public DefenseComponent()
        {
            Parameters = new()
            {
                [Parameter.Armor] = (100f, 100f),
                [Parameter.Evade] = (100f, 100f),
                [Parameter.Barrier] = (10f, 10f),
                [Parameter.MaxEvadeChance] = (0f, 0f),
                [Parameter.MaxReduceDamage] = (0f, 0f)
            };

            ModuleManager = new ModuleManager<Parameter, IParameterModule, StatModuleDecorator>(new()
            {
                [Parameter.Armor] = new ArmorModule(() => Parameters[Parameter.Armor].Current),
                [Parameter.Evade] = new EvadeChanceModule(() => Parameters[Parameter.Evade].Current),
                [Parameter.Barrier] = new BarrierModule(() => Parameters[Parameter.Barrier].Current),
                [Parameter.MaxReduceDamage] =
                    new MaxReduceDamageModule(() => Mathf.Min(0.9f, Parameters[Parameter.MaxReduceDamage].Current)),
                [Parameter.MaxEvadeChance] =
                    new MaxEvadeChanceModule(() => Parameters[Parameter.MaxEvadeChance].Current),
            });
            SetModule();
        }

        public override void OnParameterChanges(object? sender, IModifiersChangedEventArgs args)
        {
            base.OnParameterChanges(sender, args);
            if (args.Parameter != Parameter.Barrier) return;
            float newValue = Stats[args.Parameter].GetValue();
            if (CurrentBarrier > newValue) CurrentBarrier = newValue;
        }

        public float BarrierAbsorbDamage(float amount)
        {
            return 0f;
        }

        private void SetModule()
        {
            foreach (var parameter in Parameters.Keys)
                Stats[parameter] = ModuleManager.GetModule(parameter);
        }
    }
}
