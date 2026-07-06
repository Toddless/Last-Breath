namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Events.GameEvents;
    using Godot;

    /// <summary>
    /// The Porcupine buff: every real enemy hit is answered with pure damage — a share of the damage
    /// taken plus a share of the bearer's armor. Optional hooks (upgrades): heal on being hit and a
    /// chance to shave a turn off the source ability's cooldown.
    /// </summary>
    public class PorcupineBuffEffect(
        IAbility sourceAbility,
        int duration,
        float damageReturn,
        float armorReturn,
        float healOnHitPercent,
        float cooldownReduceChance)
        : Effect(id: "Effect_Porcupine", duration, maxStacks: 1)
    {
        private readonly RandomNumberGenerator _rnd = CreateRandom();

        public float DamageReturn => damageReturn;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied || Target == null) return;
            SubscribeUntilRemoved<DamageTakenEvent>(Target.CombatEvents, OnDamageTaken);
        }

        public override bool IsStronger(IEffect otherEffect) =>
            otherEffect is PorcupineBuffEffect other && damageReturn > other.DamageReturn;

        public override IEffect Copy() =>
            new PorcupineBuffEffect(sourceAbility, Duration, damageReturn, armorReturn, healOnHitPercent, cooldownReduceChance);

        private void OnDamageTaken(DamageTakenEvent evt)
        {
            if (Target == null) return;
            var context = evt.Context;
            // Answer only real hits: reflected damage, DoTs and passives must not ping-pong.
            if (context.Cause is not (DamageCause.Attack or DamageCause.Ability)) return;
            var attacker = context.Source;
            if (attacker.IsSame(Target.InstanceId) || !attacker.IsAlive) return;

            if (healOnHitPercent > 0)
                Target.Heal(new HealContext(Target, Target) { Amount = Target.Parameters.MaxHealth * healOnHitPercent });
            if (cooldownReduceChance > 0 && sourceAbility.CooldownLeft > 0 && _rnd.Randf() <= cooldownReduceChance)
                sourceAbility.CooldownLeft--;

            float returned = (context.TotalDamage * damageReturn) + (Target.Parameters.Armor * armorReturn);
            if (returned <= 0) return;
            var retaliation = new DamageContext { Source = Target, Cause = DamageCause.Effect };
            retaliation.Add(DamageType.Pure, returned);
            attacker.TakeDamage(retaliation);
        }

        private static RandomNumberGenerator CreateRandom()
        {
            var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            return rnd;
        }
    }
}
