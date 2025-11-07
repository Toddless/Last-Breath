namespace Battle.Components
{
    using Godot;
    using Source;
    using Core.Enums;
    using Source.Module.StatModules;
    using Core.Interfaces.Components;
    using System.Collections.Generic;
    using Core.Interfaces.Battle.Module;
    using Core.Interfaces.Battle.Decorator;

    internal class DamageComponent : Component, IDamageComponent
    {
        private readonly RandomNumberGenerator _rnd = new();

        public float Damage => this[Parameter.Damage].GetValue();
        public float CriticalChance => this[Parameter.CriticalChance].GetValue();
        public float CriticalDamage => this[Parameter.CriticalDamage].GetValue();
        public float AdditionalHit => this[Parameter.AdditionalHitChance].GetValue();
        public float SpellDamage => this[Parameter.SpellDamage].GetValue();

        public DamageComponent()
        {
            _rnd.Randomize();
            Parameters = new()
            {
                [Parameter.Damage] = (50f, 50f),
                [Parameter.CriticalDamage] = (0.05f, 0.05f),
                [Parameter.CriticalChance] = (1f, 1f),
                [Parameter.SpellDamage] = (25f, 25f),
                [Parameter.AdditionalHitChance] = (0.05f, 0.05f),
            };

            ModuleManager = new ModuleManager<Parameter, IParameterModule, StatModuleDecorator>(
                new Dictionary<Parameter, IParameterModule>
                {
                    [Parameter.Damage] = new DamageModule(() => Parameters[Parameter.Damage].Current),
                    [Parameter.CriticalChance] =
                        new CritChanceModule(() => Mathf.Min(1f, Parameters[Parameter.CriticalChance].Current)),
                    [Parameter.CriticalDamage] =
                        new CritDamageModule(() => Parameters[Parameter.CriticalDamage].Current),
                    [Parameter.AdditionalHitChance] =
                        new AdditionalHitChanceModule(() =>
                            Mathf.Min(1f, Parameters[Parameter.AdditionalHitChance].Current)),
                    [Parameter.SpellDamage] = new SpellDamageModule(() => Parameters[Parameter.SpellDamage].Current),
                });

            SetModule();
        }

        private void SetModule()
        {
            foreach (var param in Parameters.Keys)
                Stats[param] = ModuleManager.GetModule(param);
        }
    }
}
