namespace Battle.Components
{
    using Godot;
    using System;
    using Source;
    using Utilities;
    using Core.Enums;
    using System.Collections.Generic;
    using Core.Interfaces.Components;
    using Core.Interfaces.Battle.Module;
    using Core.Interfaces.Battle.Decorator;

    public abstract class Component
    {
        protected Dictionary<Parameter, (float Base, float Current)> Parameters = [];
        protected readonly Dictionary<Parameter, IParameterModule> Stats = [];

        protected IModuleManager<Parameter, IParameterModule, StatModuleDecorator> ModuleManager =
            new ModuleManager<Parameter, IParameterModule, StatModuleDecorator>([]);

        public event Action<Parameter, float>? ParameterChanged;

        protected IParameterModule this[Parameter type] => Stats[type];

        protected Component()
        {
            ModuleManager.ModuleDecoratorChanges += OnModuleDecoratorChanges;
        }

        public virtual void OnParameterChanges(object? sender, IModifiersChangedEventArgs args)
        {
            if (!Parameters.TryGetValue(args.Parameter, out (float Base, float Current) values)) return;
            var parameter = args.Parameter;
            float newCurrent = Mathf.Max(0, Calculations.CalculateFloatValue(values.Base, args.Modifiers));
            values.Current = newCurrent;
            Parameters[parameter] = values;
            RaiseParameterChanges(parameter);
        }

        public virtual void AddModuleDecorator(StatModuleDecorator decorator) => ModuleManager.AddDecorator(decorator);

        public virtual void RemoveModuleDecorator(string id, Parameter param) =>
            ModuleManager.RemoveDecorator(id, param);

        protected virtual void RaiseParameterChanges(Parameter args) =>
            ParameterChanged?.Invoke(args, this[args].GetValue());

        protected virtual void OnModuleDecoratorChanges(Parameter type, IParameterModule module)
        {
            if (!Parameters.ContainsKey(type)) return;
            Stats[type] = module;
        }
    }
}
