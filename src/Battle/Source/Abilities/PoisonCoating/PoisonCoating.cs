namespace Battle.Source.Abilities.PoisonCoating
{
    using Effects;
    using Module;
    using Decorators;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components;
    using Core.Interfaces.Components.Decorator;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Entity;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;

    /// <summary>
    /// Self-cast buff. For <see cref="CoatingDuration"/> turns, each of the caster's attacks
    /// applies a poison stack to the target.
    /// Cost: 100 mana. Cooldown: 7 turns.
    /// </summary>
    public class PoisonCoating(
        string[] tags,
        int cooldown,
        int costValue,
        int coatingDuration,
        int poisonDuration,
        float poisonDamagePercent,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        Costs costType = Costs.Mana)
        : Ability(
            id: "Ability_Poison_Coating",
            tags,
            cooldown,
            costValue,
            damage: 0,
            weaponDamageScale: 0,
            spellDamageScale: 0,
            upgrades,
            costType)
    {
        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParameterDecorator
        {
            get
            {
                if (field != null) return field;
                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(new()
                {
                    [Parameters.CoatingDuration] = new Module<Parameters>(() => coatingDuration, Parameters.CoatingDuration),
                    [Parameters.PoisonDuration] = new Module<Parameters>(() => poisonDuration, Parameters.PoisonDuration)
                });
                return field;
            }
        }

        public int CoatingDuration => (int)AbilityParameterDecorator.GetModule(Parameters.CoatingDuration).GetValue();
        public int PoisonDuration => (int)AbilityParameterDecorator.GetModule(Parameters.PoisonDuration).GetValue();
        public float PoisonDamagePercent { get; } = poisonDamagePercent;
        public IEntity? AbilityOwner => Owner;

        public enum Parameters : byte
        {
            CoatingDuration,
            PoisonDuration
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

        public override IAbility Copy() => throw new System.NotImplementedException();

        protected override Task ExecuteInternal(List<IEntity> targets, IEntity owner, IBattleField field)
        {
            // Apply the coating buff to the caster; PoisonCoatingEffect handles attack interception
            var coatingBuff = new PoisonCoatingEffect(
                duration: CoatingDuration,
                maxStacks: 1,
                poisonDuration: PoisonDuration,
                poisonDamagePercent: PoisonDamagePercent);

            coatingBuff.Apply(new EffectApplyingContext
            {
                Caster = owner,
                Target = owner,
                Source = InstanceId,
                Damage = 0
            });

            return Task.CompletedTask;
        }
    }
}
