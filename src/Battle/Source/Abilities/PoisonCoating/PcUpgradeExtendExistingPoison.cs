namespace Battle.Source.Abilities.PoisonCoating
{
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;

    /// <summary>
    /// L3 upgrade: when the coating is applied, extends the duration of all existing poison stacks
    /// on the target by 1 turn.
    /// Implemented as an event listener on the caster's AfterAttack event while the coating is active.
    /// </summary>
    public class PcUpgradeExtendExistingPoison(string id, string[] tags, int tier, int extension = 1)
        : AbilityUpgrade<PoisonCoating>(id, tags, tier)
    {
        private IEntity? _owner;

        public override void ApplyUpgrade(PoisonCoating ability)
        {
            // Hook into the owner's AfterAttack to extend poison on hit
            if (ability.AbilityOwner is not { } owner) return;
            _owner = owner;
            owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        public override void RemoveUpgrade(PoisonCoating ability)
        {
            if (_owner == null) return;
            _owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            _owner = null;
        }

        private void OnAfterAttack(AfterAttackEvent evt)
        {
            if (evt.Context.Result != AttackResults.Succeed) return;
            var poisonOnTarget = evt.Context.Target.Effects
                .GetBy(e => e.Status == StatusEffects.Poison)
                .ToList();
            foreach (var stack in poisonOnTarget)
                stack.Duration += extension;
        }

        public override IAbilityUpgradeWrap<PoisonCoating> Copy() =>
            new PcUpgradeExtendExistingPoison(Id, Tags, Tier, extension);
    }
}
