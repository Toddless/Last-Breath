namespace Battle.Source.Abilities.HeadButt
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Components;
    using Core.Components.Decorator;
    using Core.Components.Module;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Effects;

    /// <summary>
    /// Single lunge that stuns the target on a successful hit. L3 turns it into two lunges;
    /// impact riders (L2 armor debuff) fire per attack like in every attack-series ability.
    /// </summary>
    public class HeadButt(
        string[] tags,
        int cooldown,
        int costValue,
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        int stunDuration,
        int attacks,
        Costs costType = Costs.Mana)
        : DamagingAbility(id: "Ability_Head_Butt", tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, costType)
    {
        private float this[Parameters parameter] => AbilityParameterDecorator.GetModule(parameter).GetValue();

        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParameterDecorator
        {
            get
            {
                if (field != null) return field;
                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(new()
                {
                    [Parameters.StunDuration] = new Module<Parameters>(() => stunDuration, Parameters.StunDuration),
                    [Parameters.Attacks] = new Module<Parameters>(() => attacks, Parameters.Attacks)
                });
                return field;
            }
        }

        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                AddModuleValues(values, AbilityParameterDecorator);
                return values;
            }
        }

        public int StunDuration => (int)this[Parameters.StunDuration];
        public int Attacks => (int)this[Parameters.Attacks];

        public enum Parameters : byte
        {
            StunDuration,
            Attacks
        }

        public override void AddParameterDecorator<T>(IModuleDecorator<T, IParameterModule<T>> decorator)
        {
            if (decorator is not AbilityParameterDecorator<Parameters> parameterDecorator)
            {
                base.AddParameterDecorator(decorator);
                return;
            }

            AbilityParameterDecorator.AddDecorator(parameterDecorator);
        }

        public override void RemoveParameterDecorator<T>(string id, T key)
        {
            if (key is not Parameters parameter)
            {
                base.RemoveParameterDecorator(id, key);
                return;
            }

            AbilityParameterDecorator.RemoveDecorator(id, parameter);
        }

        public override IAbility Copy()
        {
            var copy = new HeadButt(Tags, (int)Cooldown, CostValue, Damage, WeaponDamageScale, SpellDamageScale, StunDuration, Attacks, CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }


        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            var rnd = new Godot.RandomNumberGenerator();
            rnd.Randomize();
            var cts = new CancellationTokenSource();

            foreach (IFightable target in targets)
            {
                var scheduler = new AttackContextScheduler();
                for (int i = 0; i < Attacks; i++)
                {
                    if (!target.IsAlive) break;
                    float additionalDamage = Damage + (owner.Parameters.Damage * WeaponDamageScale) + (owner.Parameters.SpellDamage * SpellDamageScale);
                    var context = new AttackContext(owner, target, owner.Parameters.Damage, rnd, scheduler)
                    {
                        RawCriticalChance = owner.Parameters.CriticalChance,
                        RawCriticalDamage = owner.Parameters.CriticalDamage,
                        AdditionalDamage = additionalDamage,
                        Index = i,
                        TotalCount = Attacks,
                        SourceAbilityId = Id
                    };

                    if (!context.Schedule()) break;
                    await foreach (var processed in scheduler.RunQueue(cts.Token))
                    {
                        if (processed.Attacker.InstanceId != owner.InstanceId) continue;
                        if (processed.Result is AttackResults.Succeed) await ApplyStun(owner, processed.Target);
                        await ApplyImpactRiders(processed.ToImpact(field));
                    }
                }
            }
        }

        private async Task ApplyStun(IFightable owner, IFightable target) =>
            await new StunEffect(StunDuration).Apply(new EffectApplyingContext { Caster = owner, Target = target, Source = InstanceId });
    }
}
