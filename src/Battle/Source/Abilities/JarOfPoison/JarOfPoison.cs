namespace Battle.Source.Abilities.JarOfPoison
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Components;
    using Core.Components.Decorator;
    using Core.Components.Module;
    using Core.Data;
    using Core.Entity;
    using Core.Enums;
    using Effects;
    using HitDelivery;

    public class JarOfPoison(
        string[] tags,
        int cooldown,
        int costValue,
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        int poisonDuration,
        Costs costType = Costs.Mana)
        : DamagingAbility(id: "Ability_Jar_Of_Poison", tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, costType)
    {
        private float this[Parameters parameter] => AbilityParametersModuleManager.GetModule(parameter).GetValue();

        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParametersModuleManager
        {
            get
            {
                if (field != null) return field;
                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(new()
                {
                    [Parameters.PoisonDuration] = new Module<Parameters>(() => poisonDuration, Parameters.PoisonDuration)
                });
                return field;
            }
        }

        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                AddModuleValues(values, AbilityParametersModuleManager);
                return values;
            }
        }


        public int PoisonDuration => (int)this[Parameters.PoisonDuration];

        /// <summary>How the jar reaches its victims: the selected target, N bounces or every enemy (L3 upgrades swap it).</summary>
        public IHitSequenceStrategy HitSequence { get; set; } = new SelectedTargetsHits();

        public enum Parameters : byte
        {
            PoisonDuration
        }

        public override void AddParameterDecorator<T>(IModuleDecorator<T, IParameterModule<T>> decorator)
        {
            if (decorator is not AbilityParameterDecorator<Parameters> parameterDecorator)
            {
                base.AddParameterDecorator(decorator);
                return;
            }

            AbilityParametersModuleManager.AddDecorator(parameterDecorator);
        }

        public override void RemoveParameterDecorator<T>(string id, T key)
        {
            if (key is not Parameters parameter)
            {
                base.RemoveParameterDecorator(id, key);
                return;
            }

            AbilityParametersModuleManager.RemoveDecorator(id, parameter);
        }

        public override IAbility Copy()
        {
            var copy = new JarOfPoison(Tags, (int)Cooldown, CostValue, Damage, WeaponDamageScale, SpellDamageScale, PoisonDuration, CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        /// <summary>Each landing applies a poison stack AND fires the impact riders — every touched target gets the L2 debuffs.</summary>
        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            foreach (IFightable target in HitSequence.GetHitSequence(owner, targets, field))
            {
                if (!target.IsAlive) continue;
                await ApplyPoison(owner, target);
                await ApplyImpactRiders(new AbilityImpact(owner, target, field));
            }
        }

        private async Task ApplyPoison(IFightable owner, IFightable target)
        {
            float damage = Damage + (owner.Parameters.Damage * WeaponDamageScale) + (owner.Parameters.SpellDamage * SpellDamageScale);

            var context = new EffectApplyingContext { Caster = owner, Target = target, Source = InstanceId, Damage = damage };
            var poison = new DamageOverTurnEffect(PoisonDuration, StatusEffects.Poison);
            await poison.Apply(context);
        }
    }
}
