namespace Battle.Source.Abilities.PoisonExplosion
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Conditions;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;
    using Core.Interfaces.Components.Decorator;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Entity;
    using Decorators;
    using Effects;
    using Module;

    /// <summary>
    /// Removes all poison stacks from the target and instantly deals their accumulated damage.
    /// Executes the target if it had more than <see cref="ExecutionThreshold"/> stacks.
    /// </summary>
    public class PoisonExplosion : Ability
    {
        private readonly int _executionThreshold;
        private readonly float _damageMultiplier;

        public PoisonExplosion(string[] tags,
            int cooldown,
            int costValue,
            int executionThreshold,
            float damageMultiplier,
            Costs costType = Costs.Mana) : base(id: "Ability_Poison_Explosion", tags, cooldown, costValue, costType)
        {
            _executionThreshold = executionThreshold;
            _damageMultiplier = damageMultiplier;
            ExecuteCondition = new PoisonStackExecuteCondition(() => ExecutionThreshold);
        }

        private float this[Parameters parameters] => AbilityParameterDecorator.GetModule(parameters).GetValue();

        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParameterDecorator
        {
            get
            {
                if (field != null) return field;
                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(new()
                {
                    [Parameters.DamageMultiplier] = new Module<Parameters>(() => _damageMultiplier, Parameters.DamageMultiplier),
                    [Parameters.ExecutionThreshold] = new Module<Parameters>(() => _executionThreshold, Parameters.ExecutionThreshold),
                });
                return field;
            }
        }

        public int ExecutionThreshold => (int)this[Parameters.ExecutionThreshold];
        public float DamageMultiplier => this[Parameters.DamageMultiplier];

        public bool PreserveStacks { get; set; }
        public IPoisonSpreadMode? SpreadMode { get; set; }
        public IExecuteCondition? ExecuteCondition { get; set; }

        public enum Parameters : byte
        {
            ExecutionThreshold,
            DamageMultiplier
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
            var copy = new PoisonExplosion(Tags, (int)Cooldown, CostValue, ExecutionThreshold, DamageMultiplier, CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            foreach (IFightable target in targets)
                await ExplodePoison(target, owner, field);
        }

        private async Task ExplodePoison(IFightable target, IFightable owner, IBattleField field)
        {
            var poisonStacks = target.Effects
                .GetBy(e => e.Status == StatusEffects.Poison)
                .OfType<DamageOverTurnEffect>()
                .ToList();

            if (poisonStacks.Count == 0) return;

            // Sum all remaining damage (DamagePerTick × remaining Duration) for each stack
            float totalDamage = poisonStacks.Sum(dot => dot.DamagePerTick * dot.Duration) * (1 + DamageMultiplier);

            var context = new DamageContext { Source = owner, Cause = DamageCause.Ability };
            context.Add(DamageType.Poison, totalDamage);
            await target.TakeDamage(context);

            // Execute target if stack count exceeds threshold
            if (ExecuteCondition?.ShouldExecute(target) == true)
                target.Kill();

            SpreadMode?.SpreadPoison(poisonStacks, target, owner, field, InstanceId);

            if (!PreserveStacks)
            {
                foreach (var stack in poisonStacks)
                    stack.Remove();
            }
        }
    }
}
